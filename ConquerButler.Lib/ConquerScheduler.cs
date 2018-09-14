using ConquerButler.Collections;
using ConquerButler.Native;
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
    public class ConquerScheduler : IDisposable
    {
        private static ILog log = LogManager.GetLogger(typeof(ConquerScheduler));

        public ObservableCollection<ConquerProcess> Processes { get; set; }
        public ObservableCollection<ConquerTask> Tasks { get; set; }

        public bool IsRunning
        {
            get
            {
                return _actionInputCancellation != null && !_actionInputCancellation.IsCancellationRequested;
            }
        }

        public Stopwatch Clock { get; protected set; }

        private readonly ConcurrentQueue<ConquerInputAction> _deferredInputActions;
        private readonly BlockingCollection<ConquerInputAction> _inputActions;

        private readonly ProcessWatcher _processWatcher;
        private CancellationTokenSource _actionInputCancellation;

        public ConquerScheduler()
        {
            Processes = new ObservableCollection<ConquerProcess>();
            Tasks = new ObservableCollection<ConquerTask>();

            Clock = new Stopwatch();

            _deferredInputActions = new ConcurrentQueue<ConquerInputAction>();

            _inputActions = new BlockingCollection<ConquerInputAction>(new ConcurrentPriorityQueue<ConquerInputAction>(new ActionFocusComparer()));

            _processWatcher = new ProcessWatcher();
            _processWatcher.ProcessStarted += _processWatcher_ProcessStarted;
            _processWatcher.ProcessEnded += _processWatcher_ProcessEnded;
            _processWatcher.ProcessStateChanged += _processWatcher_ProcessStateChanged;
        }

        public void Start()
        {
            Clock.Start();

            _actionInputCancellation = new CancellationTokenSource();

            _processWatcher.Start();

            Task.Factory.StartNew(InputActionLoop, _actionInputCancellation.Token);
        }

        public void Stop()
        {
            _actionInputCancellation.Cancel();

            _processWatcher.Stop();

            Clock.Stop();

            log.Info("Scheduler stopped");
        }

        private async void InputActionLoop()
        {
            while (!_actionInputCancellation.IsCancellationRequested)
            {
                while (_deferredInputActions.Count > 0)
                {
                    ConquerInputAction inputAction;

                    _deferredInputActions.TryDequeue(out inputAction);

                    if (inputAction != null)
                    {
                        _inputActions.Add(inputAction);
                    }
                }

                try
                {
                    ConquerInputAction inputAction = _inputActions.Take(_actionInputCancellation.Token);

                    if (inputAction.Task.Enabled)
                    {
                        if (inputAction.Task.NeedsUserFocus)
                        {
                            // task needs and has user focus
                            if (inputAction.Task.Process.HasUserFocus())
                            {
                                await inputAction.Execute();
                            }
                            else
                            {
                                // schedule it again for later
                                _deferredInputActions.Enqueue(inputAction);
                            }
                        }
                        else
                        {
                            if (!Helpers.IsForegroundWindow(inputAction.Task.Process.InternalProcess))
                            {
                                // Task doesn't need user focus, set window to foreground
                                log.Info($"Bringing process {inputAction.Task.Process.Id} to foreground");

                                Helpers.SetForegroundWindow(inputAction.Task.Process.InternalProcess);

                                await Task.Delay(100);
                            }

                            if (Helpers.IsForegroundWindow(inputAction.Task.Process.InternalProcess))
                            {
                                await inputAction.Execute();
                            }
                        }
                    }
                    else
                    {
                        inputAction.Cancel();
                    }

                    await Task.Delay(100);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        public void AddTask(ConquerTask conquerTask)
        {
            Tasks.Add(conquerTask);

            log.Info($"Task {conquerTask} added");
        }

        public void RemoveTask(ConquerTask task)
        {
            task.Stop();

            Tasks.Remove(task);

            log.Info($"Task {task} removed");
        }

        public void AddInputAction(ConquerInputAction inputAction)
        {
            _inputActions.Add(inputAction);
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

            foreach (ConquerTask task in tasks.ToList())
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
            }
        }
    }

    public class ActionFocusComparer : IComparer<ConquerInputAction>
    {
        public int Compare(ConquerInputAction x, ConquerInputAction y)
        {
            int c = x.Task.Priority.CompareTo(y.Task.Priority);

            if (c == 0)
            {
                c = y.Task.StartTime.CompareTo(x.Task.StartTime);

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
}
