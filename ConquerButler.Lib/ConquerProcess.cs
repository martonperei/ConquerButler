using AForge.Imaging;
using log4net;
using PropertyChanged;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using WindowsInput;

namespace ConquerButler
{
    [ImplementPropertyChanged]
    public class ConquerProcess : IDisposable, INotifyPropertyChanged
    {
        private static ILog log = LogManager.GetLogger(typeof(ConquerProcess));

        public ConquerScheduler Scheduler { get; protected set; }
        public Process InternalProcess { get; protected set; }
        public bool Invalid { get; protected set; }
        public Point MousePosition { get; set; }
        public InputSimulator Simulator { get; protected set; }

        public ObservableCollection<ConquerTask> Tasks { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private readonly ConcurrentQueue<ConquerTask> _addedTasks;
        private readonly ConcurrentQueue<ConquerTask> _removedTasks;
        private readonly TemplateMatchComparer _templateComparer;
        private readonly Random _random;

        public ConquerProcess(Process process, ConquerScheduler scheduler)
        {
            InternalProcess = process;
            Scheduler = scheduler;
            Tasks = new ObservableCollection<ConquerTask>();
            Simulator = new InputSimulator();

            _random = new Random();

            _templateComparer = new TemplateMatchComparer();

            _addedTasks = new ConcurrentQueue<ConquerTask>();
            _removedTasks = new ConcurrentQueue<ConquerTask>();
        }

        public void AddTask(ConquerTask task)
        {
            _addedTasks.Enqueue(task);
        }

        public void RemoveTask(ConquerTask task)
        {
            _removedTasks.Enqueue(task);
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
                task.Tick(dt);
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

        public Point GetCursorPosition()
        {
            return Helpers.GetCursorPosition(InternalProcess);
        }

        public bool HasUserFocus()
        {
            return Helpers.IsForegroundWindow(InternalProcess) &&
                Helpers.IsCursorInsideWindow(Helpers.GetCursorPosition(InternalProcess), InternalProcess);
        }

        public List<TemplateMatch> FindMatches(float similiarity, Rectangle sourceRect, params Bitmap[] templates)
        {
            using (Bitmap source = Helpers.PrintWindow(InternalProcess))
            {
                if (source.Width < source.Width || source.Height < sourceRect.Height)
                {
                    return new List<TemplateMatch>();
                }

                ExhaustiveTemplateMatching tm = new ExhaustiveTemplateMatching(similiarity);

                List<TemplateMatch> matches = new List<TemplateMatch>();

                foreach (Bitmap template in templates)
                {
                    TemplateMatch[] match = tm.ProcessImage(source, template, sourceRect);

                    matches.AddRange(match);
                }

                matches.Sort(_templateComparer);

                return matches;
            }
        }

        public Point MatchToPoint(TemplateMatch m)
        {
            return new Point(m.Rectangle.X + m.Rectangle.Width / 2, m.Rectangle.Y + m.Rectangle.Height / 2);
        }

        public void LeftClickOnPoint(Point p, int variation = 5)
        {
            Helpers.ClientToVirtualScreen(InternalProcess, ref p);

            float x = p.X + _random.Next(-variation, variation);
            float y = p.Y + _random.Next(-variation, variation);

            Simulator.Mouse.MoveMouseTo(x, y);
            Simulator.Mouse.LeftButtonClick();
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
