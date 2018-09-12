using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConquerButler.Util;
using ConquerButler.Collections;
using ConquerButler.Native;

namespace ConquerButler
{
    public class ActionFocusComparer : IComparer<ConquerInputAction>
    {
        public int Compare(ConquerInputAction x, ConquerInputAction y)
        {
            int c = x.Task.Priority.CompareTo(y.Task.Priority);

            if (c == 0)
            {
                c = y.Task.StartTick.CompareTo(x.Task.StartTick);

                if (c == 0)
                {
                    c = x.Task.Process.Id.CompareTo(y.Task.Process.Id);

                    if (c == 0)
                    {
                        c = x.Priority.CompareTo(y.Priority);
                    }
                }
            }

            return c;
        }
    }

    public class ConquerInputAction
    {
        public ConquerTask Task { get; set; }
        public Func<Task> Action { get; set; }
        public long Priority { get; set; }
        public TaskCompletionSource<bool> ActionCompletion { get; } = new TaskCompletionSource<bool>();

        public async Task Call()
        {
            try
            {
                await Action();

                ActionCompletion.SetResult(true);
            }
            catch (Exception e)
            {
                ActionCompletion.SetException(e);
            }
        }

        public void Cancel()
        {
            ActionCompletion.SetCanceled();
        }
    }

    public class ConquerScheduler : IDisposable
    {
        private static ILog log = LogManager.GetLogger(typeof(ConquerScheduler));

        public ObservableCollection<ConquerProcess> Processes { get; set; }
        public ObservableCollection<ConquerTask> Tasks { get; set; }

        public bool IsRunning
        {
            get
            {
                return _cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested;
            }
        }

        public double TargetElapsedTime { get; protected set; }
        public Clock Clock { get; protected set; }

        private double _frameTime;
        private double _accumulator;

        private readonly ConcurrentQueue<ConquerInputAction> _deferredActions;
        private readonly BlockingCollection<ConquerInputAction> _actions;

        private readonly ProcessWatcher _processWatcher;

        private readonly Random _random;

        private CancellationTokenSource _cancellationTokenSource;
        private Task _tickTask;
        private Task _actionTask;

        public ConquerScheduler()
        {
            Processes = new ObservableCollection<ConquerProcess>();
            Tasks = new ObservableCollection<ConquerTask>();

            TargetElapsedTime = 1 / 60f;
            Clock = new Clock();

            _deferredActions = new ConcurrentQueue<ConquerInputAction>();

            _actions = new BlockingCollection<ConquerInputAction>(new ConcurrentPriorityQueue<ConquerInputAction>(new ActionFocusComparer()));

            _random = new Random();

            _processWatcher = new ProcessWatcher();
            _processWatcher.ProcessStarted += _processWatcher_ProcessStarted;
            _processWatcher.ProcessEnded += _processWatcher_ProcessEnded;
            _processWatcher.ProcessStateChanged += _processWatcher_ProcessStateChanged;
        }

        public void Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            Clock.Start();

            Clock.Frame();

            _processWatcher.Start();

            _tickTask = Task.Factory.StartNew(TickLoop, _cancellationTokenSource.Token).ContinueWith(task =>
            {
                if (!task.IsCanceled && task.IsFaulted)
                {
                    log.Error("Tick task exception", task.Exception);
                }
                else
                {
                    log.Info("Tick task ended");
                }
            });

            _actionTask = Task.Factory.StartNew(ActionLoop, _cancellationTokenSource.Token).ContinueWith(task =>
            {
                if (!task.IsCanceled && task.IsFaulted)
                {
                    log.Error("Action task exception", task.Exception);
                } else
                {
                    log.Info("Action task ended");
                }
            });
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();

            _processWatcher.Stop();

            log.Info("Scheduler stopped");
        }

        protected void FixedTick(double dt)
        {
            foreach (ConquerTask task in Tasks)
            {
                task.Tick(_frameTime);
            }
        }

        private void TickLoop()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                _frameTime = Clock.Frame();

                if (_frameTime > 0.25f) _frameTime = 0.25f;

                _accumulator += _frameTime;

                while (_accumulator >= TargetElapsedTime)
                {
                    _accumulator -= TargetElapsedTime;

                    Clock.FixedUpdate();

                    FixedTick(TargetElapsedTime);
                }

                var remainingTimeMs = Math.Max(0, (int)((TargetElapsedTime - _frameTime) * 1000));

                Thread.Sleep(remainingTimeMs);
            }
        }

        private async void ActionLoop()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    while (_deferredActions.Count > 0)
                    {
                        ConquerInputAction deferredAction;

                        _deferredActions.TryDequeue(out deferredAction);

                        if (deferredAction != null)
                        {
                            _actions.Add(deferredAction);
                        }
                    }

                    ConquerInputAction action = _actions.Take(_cancellationTokenSource.Token);

                    if (action.Task.Enabled)
                    {
                        if (action.Task.NeedsUserFocus)
                        {
                            // task needs and has user focus
                            if (action.Task.Process.HasUserFocus())
                            {
                                await action.Call();
                            }
                            else
                            {
                                // schedule it again for later
                                _deferredActions.Enqueue(action);
                            }
                        }
                        else
                        { 
                            if (!Helpers.IsForegroundWindow(action.Task.Process.InternalProcess))
                            {
                                // Task doesn't need user focus, set window to foreground
                                log.Info($"Bringing process {action.Task.Process.Id} to foreground");

                                Helpers.SetForegroundWindow(action.Task.Process.InternalProcess);

                                await Task.Delay(100);
                            }

                            if (Helpers.IsForegroundWindow(action.Task.Process.InternalProcess))
                            {
                                await action.Call();
                            }
                        }
                    }
                    else
                    {
                        action.Cancel();
                    }
                } catch (OperationCanceledException)
                {
                    log.Info("Scheduler task cancelled");
                }

                await Task.Delay(100);
            }
        }

        public void AddTask(ConquerTask task)
        {
            Tasks.Add(task);

            log.Info($"Task {task} added");
        }

        public void RemoveTask(ConquerTask task)
        {
            task.Cancel();

            Tasks.Remove(task);

            log.Info($"Task {task} removed");
        }

        public void AddInputAction(ConquerInputAction inputAction)
        {
            _actions.Add(inputAction);
        }

        public void CancelRunning()
        {
            log.Info("Scheduler cancelling running taks...");

            foreach (ConquerTask task in Tasks)
            {
                task.Cancel();
            }

            while (_actions.Count > 0)
            {
                ConquerInputAction action = _actions.Take();

                action.Cancel();
            }
        }

        public Task Delay(int delay, int variance = 50)
        {
            return Task.Delay(delay + _random.Next(-variance, variance));
        }

        private void _processWatcher_ProcessStateChanged(Process process, bool isDisconnected)
        {
            ConquerProcess conquerProcess = Processes.Single(c => c.InternalProcess.Id == process.Id);

            log.Info($"Process {process.Id} disconnect state changed from {conquerProcess.Disconnected} to {isDisconnected}");

            conquerProcess.Disconnected = isDisconnected;
        }

        private void _processWatcher_ProcessEnded(Process process)
        {
            log.Info($"Process {process.Id} ended");

            ConquerProcess conquerProcess = Processes.Single(c => c.InternalProcess.Id == process.Id);

            List<ConquerTask> tasks = Tasks.Where(t => t.Process.Equals(conquerProcess)).ToList();

            foreach (ConquerTask task in tasks)
            {
                RemoveTask(task);
            }

            Processes.Remove(conquerProcess);
        }

        private void _processWatcher_ProcessStarted(Process process, bool isDisconnected)
        {
            log.Info($"Process {process.Id} started");

            Processes.Add(new ConquerProcess(process, this)
            {
                Disconnected = isDisconnected
            });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ConquerScheduler()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stop();

                foreach (ConquerTask task in Tasks)
                {
                    task.Dispose();
                }
            }
        }
    }
}
