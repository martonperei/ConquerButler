using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Canary.Core;

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
                        c = NativeMethods.IsForegroundWindow(x.Task.Process) ? 1 : -1;
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

    public class ConquerInputScheduler
    {
        private const int TICK_INTERVAL = 100;

        private readonly List<ConquerTask> Tasks;

        private PriorityQueue<ConquerActionFocus> ActionFocusQueue;
        private Random Random;

        public long Tick { get; protected set; }

        public ConquerInputScheduler()
        {
            Tasks = new List<ConquerTask>();
            ActionFocusQueue = new PriorityQueue<ConquerActionFocus>(new ActionFocusComparer());
            Random = new Random();

            Tick = 0;
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

        public void Wait(int wait)
        {
            Thread.Sleep(wait + Random.Next(-50, 50));
        }

        public void Start()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    while (ActionFocusQueue.Count > 0)
                    {
                        ConquerActionFocus actionFocus = ActionFocusQueue.Take();

                        if (actionFocus.Task.Enabled)
                        {
                            if (actionFocus.BringToForeground)
                            {
                                NativeMethods.SetForegroundWindow(actionFocus.Task.Process);

                                Wait(500);

                                actionFocus.Action();

                                actionFocus.TaskCompletion.SetResult(true);

                            }
                            else if (NativeMethods.IsForegroundWindow(actionFocus.Task.Process) &&
                              NativeMethods.IsCursorInsideWindow(NativeMethods.GetCursorPosition(actionFocus.Task.Process),
                              actionFocus.Task.Process))
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

                        Thread.Sleep(TICK_INTERVAL);
                    }

                    Thread.Sleep(TICK_INTERVAL);
                }
            });

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    foreach (ConquerTask task in Tasks)
                    {
                        if (task.Enabled && !task.IsRunning)
                        {
                            if (task.NextRun <= 0)
                            {
                                // TODO: start task
                                task.Tick();
                            }
                            else
                            {
                                task.NextRun -= TICK_INTERVAL;
                            }
                        }
                    }

                    Thread.Sleep(TICK_INTERVAL);
                }
            });
        }

        public void Clear()
        {
        }
    }
}
