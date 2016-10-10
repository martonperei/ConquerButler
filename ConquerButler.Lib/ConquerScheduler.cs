using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConquerButler
{
    public class ActionFocusComparer : IComparer<ConquerAction>
    {
        public int Compare(ConquerAction x, ConquerAction y)
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

    public class ConquerAction
    {
        public ConquerTask Task { get; set; }
        public Action Action { get; set; }
        public long Priority { get; set; }
        public TaskCompletionSource<bool> TaskCompletion { get; } = new TaskCompletionSource<bool>();
    }

    public class ConquerScheduler : IDisposable
    {
        private static ILog log = LogManager.GetLogger(typeof(ConquerScheduler));

        public const string CONQUER_PROCESS_NAME = "Zonquer";

        public ObservableCollection<ConquerProcess> Processes { get; set; }

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

        private readonly ConcurrentQueue<ConquerProcess> _endedProcesses;
        private readonly ConcurrentQueue<ConquerProcess> _startedProcesses;

        private readonly ConcurrentQueue<ConquerAction> _deferredActions;
        private readonly BlockingCollection<ConquerAction> _actions;

        private readonly ProcessWatcher _processWatcher;

        private readonly Random _random;

        private CancellationTokenSource _cancellationTokenSource;
        private Task _tickTask;
        private Task _actionTask;

        public ConquerScheduler()
        {
            Processes = new ObservableCollection<ConquerProcess>();
            TargetElapsedTime = 1 / 60f;
            Clock = new Clock();

            _endedProcesses = new ConcurrentQueue<ConquerProcess>();
            _startedProcesses = new ConcurrentQueue<ConquerProcess>();
            _deferredActions = new ConcurrentQueue<ConquerAction>();

            _actions = new BlockingCollection<ConquerAction>(new ConcurrentPriorityQueue<ConquerAction>(new ActionFocusComparer()));

            _random = new Random();

            _processWatcher = new ProcessWatcher(CONQUER_PROCESS_NAME);
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
            while (_endedProcesses.Count > 0)
            {
                ConquerProcess process;

                _endedProcesses.TryDequeue(out process);

                if (process != null)
                {
                    Processes.Remove(process);
                }
            }

            while (_startedProcesses.Count > 0)
            {
                ConquerProcess process;

                _startedProcesses.TryDequeue(out process);

                if (process != null)
                {
                    Processes.Add(process);
                }
            }

            while (_deferredActions.Count > 0)
            {
                ConquerAction action;

                _deferredActions.TryDequeue(out action);

                if (action != null)
                {
                    _actions.Add(action);
                }
            }

            foreach (ConquerProcess process in Processes)
            {
                process.Tick(_frameTime);
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

        private void ActionLoop()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                ConquerAction action = _actions.Take(_cancellationTokenSource.Token);

                if (action.Task.Enabled)
                {
                    if (!action.Task.NeedsUserFocus &&
                        !Helpers.IsForegroundWindow(action.Task.Process.InternalProcess))
                    {
                        log.Info($"Bringing process {action.Task.Process.Id} to foreground");

                        Helpers.SetForegroundWindow(action.Task.Process.InternalProcess);

                        Wait(500);
                    }

                    if (Helpers.IsForegroundWindow(action.Task.Process.InternalProcess))
                    {
                        action.Action();

                        action.TaskCompletion.SetResult(true);
                    }
                    else
                    {
                        _deferredActions.Enqueue(action);
                    }
                }
                else
                {
                    action.TaskCompletion.SetCanceled();
                }
            }
        }

        public ConquerAction RequestInputFocus(ConquerTask task, Action action, int priority)
        {
            ConquerAction focus = new ConquerAction()
            {
                Task = task,
                Action = action,
                Priority = priority,
            };

            _actions.Add(focus);

            return focus;
        }

        public void CancelRunning()
        {
            log.Info("Scheduler cancelling running taks...");

            foreach (ConquerProcess process in Processes)
            {
                process.Cancel();
            }

            while (_actions.Count > 0)
            {
                ConquerAction action = _actions.Take();

                action.TaskCompletion.SetCanceled();
            }
        }

        public void Wait(int wait, int variance = 50)
        {
            Thread.Sleep(wait + _random.Next(-variance, variance));
        }

        private void _processWatcher_ProcessStateChanged(Process process, bool isDisconnected)
        {
            ConquerProcess conquerProcess = Processes.FirstOrDefault(c => c.InternalProcess.Id == process.Id);

            log.Info($"Process {process.Id} disconnect state changed from {conquerProcess.Disconnected} to {isDisconnected}");

            conquerProcess.Disconnected = isDisconnected;
        }

        private void _processWatcher_ProcessEnded(Process process)
        {
            log.Info($"Process {process.Id} ended");

            ConquerProcess conquerProcess = Processes.FirstOrDefault(c => c.InternalProcess.Id == process.Id);

            _endedProcesses.Enqueue(conquerProcess);
        }

        private void _processWatcher_ProcessStarted(Process process, bool isDisconnected)
        {
            log.Info($"Process {process.Id} started");

            _startedProcesses.Enqueue(new ConquerProcess(process, this)
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

                foreach (ConquerProcess process in Processes)
                {
                    process.Dispose();
                }
            }
        }
    }
}
