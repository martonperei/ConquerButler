using Accord.Extensions.Imaging.Algorithms.LINE2D;
using DotImaging;
using log4net;
using PropertyChanged;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using WindowsInput;

namespace ConquerButler
{
    [ImplementPropertyChanged]
    public class ConquerProcess : IDisposable, INotifyPropertyChanged
    {
        private static ILog log = LogManager.GetLogger(typeof(ConquerProcess));

        public int Id { get; protected set; }
        public ConquerScheduler Scheduler { get; protected set; }
        public Process InternalProcess { get; protected set; }

        private bool _isDisconnected;
        public bool Disconnected
        {
            get { return _isDisconnected; }
            set
            {
                if (_isDisconnected != value)
                {
                    ProcessStateChange?.Invoke(value);
                }

                _isDisconnected = value;
            }
        }

        public bool Invalid { get; protected set; }
        public Point MousePosition { get; set; }
        public InputSimulator Simulator { get; protected set; }

        public ObservableCollection<ConquerTask> Tasks { get; set; }

        public event Action<bool> ProcessStateChange;
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly ConcurrentQueue<ConquerTask> _addedTasks;
        private readonly ConcurrentQueue<ConquerTask> _removedTasks;

        private readonly Random _random;

        public ConquerProcess(Process process, ConquerScheduler scheduler)
        {
            Id = process.Id;
            InternalProcess = process;
            Scheduler = scheduler;
            Tasks = new ObservableCollection<ConquerTask>();
            Simulator = new InputSimulator();

            _random = new Random();

            _addedTasks = new ConcurrentQueue<ConquerTask>();
            _removedTasks = new ConcurrentQueue<ConquerTask>();
        }

        public void AddTask(ConquerTask task)
        {
            if (!Tasks.Any(t => t.TaskType.Equals(task.TaskType)))
            {
                _addedTasks.Enqueue(task);
            }
        }

        public void RemoveTask(ConquerTask task)
        {
            _removedTasks.Enqueue(task);
        }

        public void PauseTask<T>()
        {
            foreach (ConquerTask task in Tasks)
            {
                if (typeof(T) == task.GetType())
                {
                    task.Pause();
                }
            }
        }

        public void Tick(double dt)
        {
            if (HasUserFocus())
            {
                MousePosition = GetCursorPosition();
            }
            else
            {
                MousePosition = Point.Empty;
            }

            while (_removedTasks.Count > 0)
            {
                ConquerTask task;

                _removedTasks.TryDequeue(out task);

                if (task != null)
                {
                    Tasks.Remove(task);

                    task.Cancel();
                }
            }

            while (_addedTasks.Count > 0)
            {
                ConquerTask task;

                _addedTasks.TryDequeue(out task);

                if (task != null)
                {
                    Tasks.Add(task);
                }
            }

            foreach (ConquerTask task in Tasks)
            {
                if (!task.NeedsToBeConnected || (!Disconnected && task.NeedsToBeConnected))
                {
                    task.Tick(dt);
                }
            }
        }

        public void Invalidate()
        {
            Invalid = true;

            foreach (ConquerTask task in Tasks)
            {
                task.Cancel();
            }
        }

        public void Pause()
        {
            foreach (ConquerTask task in Tasks)
            {
                task.Pause();
            }
        }

        public void Resume()
        {
            foreach (ConquerTask task in Tasks)
            {
                task.Resume();
            }
        }

        public void Cancel()
        {
            foreach (ConquerTask task in Tasks)
            {
                task.Cancel();
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public Bitmap Screenshot()
        {
            return Helpers.PrintWindow(InternalProcess);
        }

        public LinearizedMapPyramid ScreenshotTemplate()
        {
            using (var screenshot = Screenshot())
            {
                Bgr<byte>[,] bgr = screenshot.ToArray() as Bgr<byte>[,];

                return LinearizedMapPyramid.CreatePyramid(bgr);
            }
        }

        public Point GetCursorPosition()
        {
            return Helpers.GetCursorPosition(InternalProcess);
        }

        public bool HasUserFocus()
        {
            return Helpers.IsForegroundWindow(InternalProcess) &&
                Helpers.IsCursorInsideWindow(Helpers.GetCursorPosition(InternalProcess), InternalProcess);
        }

        public void LeftClickOnPoint(Point p, int variation = 5)
        {
            p.X = p.X + _random.Next(-variation, variation);
            p.Y = p.Y + _random.Next(-variation, variation);

            Helpers.ClientToVirtualScreen(InternalProcess, ref p);

            Simulator.Mouse.MoveMouseTo(p.X, p.Y);
            Simulator.Mouse.LeftButtonClick();
        }

        public void MoveToPoint(Point p, int variation = 5)
        {
            p.X = p.X + _random.Next(-variation, variation);
            p.Y = p.Y + _random.Next(-variation, variation);

            Helpers.ClientToVirtualScreen(InternalProcess, ref p);

            Simulator.Mouse.MoveMouseTo(p.X, p.Y);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ConquerProcess()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Invalid = true;

                foreach (ConquerTask task in Tasks)
                {
                    task.Dispose();
                }

                Tasks.Clear();
            }
        }
    }
}
