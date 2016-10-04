using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Canary.Core;
using System.Diagnostics;
using System.Collections.ObjectModel;
using log4net;

namespace ConquerButler
{
    public class ActionFocusComparer : IComparer<ConquerActionFocus>
    {
        public int Compare(ConquerActionFocus x, ConquerActionFocus y)
        {
            int c = x.Task.Priority.CompareTo(y.Task.Priority);

            if (c == 0)
            {
                c = x.Task.StartTick.CompareTo(y.Task.StartTick);

                if (c == 0)
                {
                    c = x.Priority.CompareTo(y.Priority);

                    if (c == 0)
                    {
                        c = Helpers.IsForegroundWindow(x.Task.Process.InternalProcess) ? 1 : -1;
                    }
                }
            }

            return c;
        }
    }

    public class ConquerActionFocus
    {
        public ConquerTask Task { get; set; }
        public Action Action { get; set; }
        public long Priority { get; set; }
        public bool BringToForeground { get; set; }
        public TaskCompletionSource<bool> TaskCompletion { get; } = new TaskCompletionSource<bool>();
    }

    public class ConquerInputScheduler : IDisposable
    {
        private static ILog log = LogManager.GetLogger(typeof(ConquerInputScheduler));

        public const string CONQUER_PROCESS_NAME = "Zonquer";

        public ObservableCollection<ConquerProcess> Processes { get; set; }
        protected ConcurrentQueue<ConquerProcess> EndedProcesses { get; }
        protected ConcurrentQueue<ConquerProcess> StartedProcesses { get; }

        public ObservableCollection<ConquerTask> Tasks { get; set; }
        private PriorityQueue<ConquerActionFocus> ActionFocusQueue;

        private ProcessWatcher ProcessWatcher;

        public bool IsRunning { get; protected set; }
        public long CurrentTick { get; protected set; }
        private Task ActionTask;
        private CancellationTokenSource ActionTaskCancellation;

        private Random Random;

        public ConquerInputScheduler()
        {
            Processes = new ObservableCollection<ConquerProcess>();
            EndedProcesses = new ConcurrentQueue<ConquerProcess>();
            StartedProcesses = new ConcurrentQueue<ConquerProcess>();
            Tasks = new ObservableCollection<ConquerTask>();
            ActionFocusQueue = new PriorityQueue<ConquerActionFocus>(new ActionFocusComparer());
            Random = new Random();

            CurrentTick = 0;

            ProcessWatcher = new ProcessWatcher(CONQUER_PROCESS_NAME);
            ProcessWatcher.ProcessStarted += ProcessWatcher_ProcessStarted;
            ProcessWatcher.ProcessEnded += ProcessWatcher_ProcessEnded;
        }

        public void Start()
        {
            ProcessWatcher.Start();

            ActionTaskCancellation = new CancellationTokenSource();
            ActionTask = Task.Factory.StartNew(DoActions, ActionTaskCancellation.Token);
        }

        public void Stop()
        {
            ProcessWatcher.Stop();

            ActionTaskCancellation.Cancel();
        }

        private void DoActions()
        {
            IsRunning = true;

            while (!ActionTaskCancellation.IsCancellationRequested)
            {
                while (EndedProcesses.Count > 0)
                {
                    ConquerProcess process;

                    EndedProcesses.TryDequeue(out process);

                    if (process != null)
                    {
                        Processes.Remove(process);
                    }
                }

                while (StartedProcesses.Count > 0)
                {
                    ConquerProcess process;

                    StartedProcesses.TryDequeue(out process);

                    if (process != null)
                    {
                        Processes.Add(process);
                    }
                }

                foreach (ConquerProcess process in Processes)
                {
                    process.Refresh();
                }

                foreach (ConquerTask task in Tasks)
                {
                    task.Tick(1);
                }

                while (ActionFocusQueue.Count > 0)
                {
                    ConquerActionFocus actionFocus = ActionFocusQueue.Take();

                    if (actionFocus.Task.Enabled)
                    {
                        if (actionFocus.BringToForeground)
                        {
                            Helpers.SetForegroundWindow(actionFocus.Task.Process.InternalProcess);

                            Wait(500);

                            actionFocus.Action();

                            actionFocus.TaskCompletion.SetResult(true);

                        }
                        else if (Helpers.IsForegroundWindow(actionFocus.Task.Process.InternalProcess) &&
                          Helpers.IsCursorInsideWindow(Helpers.GetCursorPosition(actionFocus.Task.Process.InternalProcess),
                          actionFocus.Task.Process.InternalProcess))
                        {
                            actionFocus.Action();

                            actionFocus.TaskCompletion.SetResult(true);
                        }
                        else
                        {
                            // requeue for future
                            ActionFocusQueue.Add(actionFocus);
                        }
                    }
                    else
                    {
                        actionFocus.TaskCompletion.SetCanceled();
                        ActionFocusQueue.Remove(actionFocus);
                    }
                }

                Thread.Sleep(1);
            }

            IsRunning = false;
        }

        public ConquerActionFocus RequestInputFocus(ConquerTask task, Action action, int priority, bool bringToForeground)
        {
            ConquerActionFocus focus = new ConquerActionFocus()
            {
                Task = task,
                Action = action,
                Priority = priority,
                BringToForeground = bringToForeground
            };

            ActionFocusQueue.Add(focus);

            return focus;
        }

        public void Add(ConquerTask task)
        {
            Tasks.Add(task);
        }

        public void Remove(ConquerTask task)
        {
            Tasks.Remove(task);
        }

        public void CancelRunning()
        {
            ActionFocusQueue.Clear();

            foreach (ConquerProcess process in Processes)
            {
                process.Cancel();
            }
        }

        public void Wait(int wait)
        {
            Thread.Sleep(wait + Random.Next(-50, 50));
        }

        private void ProcessWatcher_ProcessEnded(Process process)
        {
            ConquerProcess conquerProcess = Processes.FirstOrDefault(c => c.InternalProcess.Id == process.Id);

            EndedProcesses.Enqueue(conquerProcess);
        }

        private void ProcessWatcher_ProcessStarted(Process process)
        {
            StartedProcesses.Enqueue(new ConquerProcess(process));
        }

        public void Clear()
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ConquerInputScheduler()
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
