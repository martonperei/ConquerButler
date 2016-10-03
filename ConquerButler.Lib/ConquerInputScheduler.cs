﻿using System;
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
                        c = ProcessHelper.IsForegroundWindow(x.Task.Process) ? 1 : -1;
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

        public ConquerActionFocus RequestInputFocus(ConquerTask task, Action action, int priority)
        {
            ConquerActionFocus focus = new ConquerActionFocus()
            {
                Priority = priority,
                Task = task,
                Action = action
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
            Thread.Sleep(wait + Random.Next(-100, 100));
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
                            ProcessHelper.SetForegroundWindow(actionFocus.Task.Process);

                            Wait(500);

                            actionFocus.Action();

                            actionFocus.TaskCompletion.SetResult(true);
                        } else
                        {
                            actionFocus.TaskCompletion.SetCanceled();
                        }
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
