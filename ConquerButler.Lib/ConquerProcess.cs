using AForge.Imaging;
using log4net;
using PropertyChanged;
using System;
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

        public bool Invalid { get; protected set; }
        public Process InternalProcess { get; set; }
 
        public Point MousePosition { get; set; }
        public ObservableCollection<ConquerTask> Tasks { get; }

        public readonly InputSimulator Simulator = new InputSimulator();
        protected readonly TemplateMatchComparer Comparer = new TemplateMatchComparer();

        public event PropertyChangedEventHandler PropertyChanged;

        public ConquerProcess(Process process)
        {
            InternalProcess = process;
            Tasks = new ObservableCollection<ConquerTask>();

            Refresh();
        }

        public void Refresh()
        {
            Point p = Helpers.GetCursorPosition(InternalProcess);

            if (Helpers.IsForegroundWindow(InternalProcess) && Helpers.IsCursorInsideWindow(p, InternalProcess))
            {
                MousePosition = p;
            }
            else
            {
                MousePosition = Point.Empty;
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

        protected List<TemplateMatch> Detect(float similiarityThreshold, Bitmap sourceImage, params Bitmap[] templates)
        {
            ExhaustiveTemplateMatching tm = new ExhaustiveTemplateMatching(similiarityThreshold);

            List<TemplateMatch> matches = new List<TemplateMatch>();

            foreach (Bitmap template in templates)
            {
                TemplateMatch[] match = tm.ProcessImage(sourceImage, template);

                matches.AddRange(match);
            }

            return matches;
        }

        public List<TemplateMatch> FindMatches(float similiarity, Rectangle sourceRect, params Bitmap[] templates)
        {
            using (Bitmap source = Helpers.PrintWindow(InternalProcess, sourceRect))
            {
                List<TemplateMatch> matches = Detect(0.95f, source, templates);

                matches.Sort(Comparer);

                return matches;
            }
        }

        public Point GetWindowPointFromArea(TemplateMatch m, Rectangle area)
        {
            return Helpers.MatchRectangleToPoint(area, m.Rectangle);
        }

        public void LeftClickOnPoint(Point p)
        {
            Helpers.ClientToVirtualScreen(InternalProcess, ref p);

            Simulator.Mouse.MoveMouseTo(p.X, p.Y);
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
