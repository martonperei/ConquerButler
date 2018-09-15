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
                return _schedulerCancellation != null && !_schedulerCancellation.IsCancellationRequested;
            }
        }

        public Stopwatch Clock { get; protected set; }

        private readonly ProcessWatcher _processWatcher;

        private readonly BlockingCollection<ConquerInputAction> _inputActions;

        private readonly Dictionary<ConquerTask, CancellationTokenSource> _taskCancellations;

        private CancellationTokenSource _schedulerCancellation;

        public ConquerScheduler()
        {
            Processes = new ObservableCollection<ConquerProcess>();
            Tasks = new ObservableCollection<ConquerTask>();

            Clock = new Stopwatch();

            _inputActions = new BlockingCollection<ConquerInputAction>(new ConcurrentPriorityQueue<ConquerInputAction>(new ActionFocusComparer()));
            _taskCancellations = new Dictionary<ConquerTask, CancellationTokenSource>();

            _processWatcher = new ProcessWatcher();
            _processWatcher.ProcessStarted += _processWatcher_ProcessStarted;
            _processWatcher.ProcessEnded += _processWatcher_ProcessEnded;
            _processWatcher.ProcessStateChanged += _processWatcher_ProcessStateChanged;
        }

        public void Start()
        {
            _processWatcher.Start();

            Clock.Start();

            _schedulerCancellation = new CancellationTokenSource();

            Task.Run(MainLoop, _schedulerCancellation.Token).ContinueWith(t =>
            {
                if (t.IsCanceled || t.IsFaulted)
                {
                    log.Info($"MainLoop failed ${t.Exception}");
                }
                else
                {
                    log.Info("MainLoop finished");
                }
            });
        }

        public void Stop()
        {
            foreach (ConquerTask task in Tasks)
            {
                _taskCancellations.TryGetValue(task, out CancellationTokenSource taskCancellation);
                taskCancellation.Cancel();
            }

            _schedulerCancellation.Cancel();

            _processWatcher.Stop();

            Clock.Stop();

            log.Info("Scheduler stopped");
        }

        public async Task MainLoop()
        {
            InputActionLoop();

            while (!_schedulerCancellation.IsCancellationRequested)
            {
                foreach (ConquerTask task in Tasks)
                {
                    if (task.Enabled && !task.Started)
                    {
                        TaskLoop(task);
                    }
                    else if (!task.Enabled && task.Started)
                    {
                        _taskCancellations.TryGetValue(task, out CancellationTokenSource taskCancellation);

                        taskCancellation.Cancel();
                    }
                }

                await Task.Delay(1, _schedulerCancellation.Token);
            }
        }

        private async void TaskLoop(ConquerTask task)
        {
            CancellationTokenSource taskCancellation = new CancellationTokenSource();

            _taskCancellations.Add(task, taskCancellation);

            task.Started = true;

            await Task.Yield();

            log.Info($"Process {task.Process.Id} - task {task.TaskType} started");

            while (!taskCancellation.IsCancellationRequested)
            {
                try
                {
                    if ((!task.NeedsToBeConnected || (!task.Process.Disconnected && task.NeedsToBeConnected)) && task.Enabled && !task.Running)
                    {
                        try
                        {
                            task.StartTime = Clock.ElapsedMilliseconds;

                            task.Running = true;

                            await task.Tick();
                        }
                        finally
                        {
                            task.Running = false;
                        }
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(task.Interval), taskCancellation.Token);
                }
                catch (TaskCanceledException)
                {
                    log.Info($"Process {task.Process.Id} - task {task.TaskType} forcibly cancelled");
                    break;
                }
            }

            task.Started = false;

            _taskCancellations.Remove(task);

            log.Info($"Process {task.Process.Id} - task {task.TaskType} stopped");
        }

        private async void InputActionLoop()
        {
            await Task.Yield();

            while (!_schedulerCancellation.IsCancellationRequested)
            {
                try
                {
                    _inputActions.TryTake(out ConquerInputAction inputAction);

                    if (inputAction != null && inputAction.Task.Enabled)
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
                                _inputActions.Add(inputAction);
                            }
                        }
                        else
                        {
                            if (!Helpers.IsForegroundWindow(inputAction.Task.Process.InternalProcess))
                            {
                                // Task doesn't need user focus, set window to foreground
                                log.Info($"Bringing process {inputAction.Task.Process.Id} to foreground");

                                Helpers.SetForegroundWindow(inputAction.Task.Process.InternalProcess);

                                await Task.Delay(100, _schedulerCancellation.Token);
                            }

                            if (Helpers.IsForegroundWindow(inputAction.Task.Process.InternalProcess))
                            {
                                await inputAction.Execute();
                            }
                        }
                    }
                    else if (inputAction != null)
                    {
                        inputAction.Cancel();
                    }

                    await Task.Delay(1, _schedulerCancellation.Token);
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

        public Task Delay(ConquerTask task, int delay)
        {
            _taskCancellations.TryGetValue(task, out CancellationTokenSource taskCancellation);

            return Task.Delay(delay, taskCancellation.Token);
        }

        public Task AddInputAction(ConquerTask task, Func<Task> action, int priority)
        {
            ConquerInputAction inputAction = new ConquerInputAction()
            {
                Task = task,
                Action = action,
                Priority = priority,
            };

            _inputActions.Add(inputAction);

            return inputAction.ActionCompletion.Task;
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
