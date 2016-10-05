﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

    public class ConquerScheduler : IDisposable
    {
        private static ILog log = LogManager.GetLogger(typeof(ConquerScheduler));

        public const string CONQUER_PROCESS_NAME = "Zonquer";

        public ObservableCollection<ConquerProcess> Processes { get; set; }

        public bool IsRunning { get; protected set; }

        public double TargetElapsedTime { get; protected set; }
        public Clock Clock { get; protected set; }
        private double _frameTime;
        private double _accumulator;

        private readonly ConcurrentQueue<ConquerProcess> _endedProcesses;
        private readonly ConcurrentQueue<ConquerProcess> _startedProcesses;
        private readonly PriorityQueue<ConquerActionFocus> _actionFocusQueue;

        private readonly ProcessWatcher _processWatcher;

        private readonly Random _random;

        private Task _tickTask;
        private Task _actionTask;

        public ConquerScheduler()
        {
            Processes = new ObservableCollection<ConquerProcess>();
            TargetElapsedTime = 1f / 60.0f;
            Clock = new Clock();

            _endedProcesses = new ConcurrentQueue<ConquerProcess>();
            _startedProcesses = new ConcurrentQueue<ConquerProcess>();

            _actionFocusQueue = new ConcurrentPriorityQueue<ConquerActionFocus>(new ActionFocusComparer());

            _random = new Random();

            _processWatcher = new ProcessWatcher(CONQUER_PROCESS_NAME);
            _processWatcher.ProcessStarted += ProcessWatcher_ProcessStarted;
            _processWatcher.ProcessEnded += ProcessWatcher_ProcessEnded;
        }

        public void Start()
        {
            IsRunning = true;

            Clock.Start();

            Clock.Frame();

            _processWatcher.Start();

            _tickTask = Task.Factory.StartNew(TickLoop);

            _actionTask = Task.Factory.StartNew(ActionLoop);
        }

        public void Stop()
        {
            IsRunning = false;

            _processWatcher.Stop();
        }

        protected void FixedTick(double dt)
        {

        }

        private void TickLoop()
        {
            while (IsRunning)
            {
                _frameTime = Clock.Frame();

                if (_frameTime > 0.25f) _frameTime = 0.25f;

                _accumulator += _frameTime;

                while (_accumulator >= TargetElapsedTime)
                {
                    FixedTick(TargetElapsedTime);

                    _accumulator -= TargetElapsedTime;

                    Clock.FixedUpdate();
                }

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

                foreach (ConquerProcess process in Processes)
                {
                    process.Tick(_frameTime);
                }

                Thread.Sleep(1);
            }
        }

        private void ActionLoop()
        {
            while (IsRunning)
            {
                while (_actionFocusQueue.Count > 0 && IsRunning)
                {
                    ConquerActionFocus actionFocus = _actionFocusQueue.Take();

                    if (actionFocus.Task.Enabled)
                    {
                        if (actionFocus.BringToForeground)
                        {
                            Helpers.SetForegroundWindow(actionFocus.Task.Process.InternalProcess);

                            Wait(500);

                            actionFocus.Action();

                            actionFocus.TaskCompletion.SetResult(true);

                        }
                        else if (Helpers.IsInFocus(actionFocus.Task.Process.InternalProcess))
                        {
                            actionFocus.Action();

                            actionFocus.TaskCompletion.SetResult(true);
                        }
                        else
                        {
                            // requeue for future
                            _actionFocusQueue.Add(actionFocus);
                        }
                    }
                    else
                    {
                        actionFocus.TaskCompletion.SetCanceled();
                    }

                    Thread.Sleep(1);
                }

                Thread.Sleep(1);
            }
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

            _actionFocusQueue.Add(focus);

            return focus;
        }

        public void CancelRunning()
        {
            while (_actionFocusQueue.Count > 0)
            {
                ConquerActionFocus actionFocus = _actionFocusQueue.Take();

                actionFocus.TaskCompletion.SetCanceled();
            }

            foreach (ConquerProcess process in Processes)
            {
                process.Cancel();
            }
        }

        public void Wait(int wait)
        {
            Thread.Sleep(wait + _random.Next(-50, 50));
        }

        private void ProcessWatcher_ProcessEnded(Process process)
        {
            ConquerProcess conquerProcess = Processes.FirstOrDefault(c => c.InternalProcess.Id == process.Id);

            _endedProcesses.Enqueue(conquerProcess);
        }

        private void ProcessWatcher_ProcessStarted(Process process)
        {
            _startedProcesses.Enqueue(new ConquerProcess(process, this));
        }

        public void Clear()
        {
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