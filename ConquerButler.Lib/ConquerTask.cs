using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using WindowsInput;
using AForge.Imaging;
using PropertyChanged;
using System;
using System.Threading;
using System.Drawing.Imaging;

namespace ConquerButler
{
    public class TemplateMatchComparer : IComparer<TemplateMatch>
    {
        public const int EPSILON = 10;

        public int Compare(TemplateMatch x, TemplateMatch y)
        {
            int diff = Math.Abs(y.Rectangle.Y - x.Rectangle.Y);

            int c = diff < EPSILON ? 0 : y.Rectangle.Y.CompareTo(x.Rectangle.Y);

            if (c == 0)
            {
                return y.Rectangle.X.CompareTo(x.Rectangle.X);
            }
            else
            {
                return c;
            }
        }
    }

    [ImplementPropertyChanged]
    public abstract class ConquerTask
    {
        public const int DEFAULT_PRIORITY = 100;

        protected readonly InputSimulator Simulator = new InputSimulator();
        protected readonly TemplateMatchComparer Comparer = new TemplateMatchComparer();

        protected readonly ConquerInputScheduler Scheduler;

        public Process Process { get; }
        public int Interval { get; set; } = 10000;
        public long StartTick { get; set; }
        public long NextRun { get; set; }
        public bool Enabled { get; set; } = true;
        public int Priority { get; protected set; } = DEFAULT_PRIORITY;
        public bool IsRunning { get; protected set; }
        public bool IsPaused { get; protected set; }
        public bool RunsInForeground { get; protected set; } = false;
        public string TaskType { get; protected set; }
        protected CancellationTokenSource CancellationToken = new CancellationTokenSource();

        public virtual string DisplayInfo { get { return $"{TaskType} Running: {IsRunning} Next run: {NextRun} ms"; } }

        public ConquerTask(string taskType, ConquerInputScheduler scheduler, Process process)
        {
            TaskType = taskType;
            Scheduler = scheduler;
            Process = process;
        }

        public async Task Tick()
        {
            IsRunning = true;

            StartTick = Scheduler.Tick;
            NextRun = -1;

            await DoTick();

            NextRun = Interval;

            IsRunning = false;
        }

        public abstract Task DoTick();

        public void Cancel()
        {
            Enabled = false;

            CancellationToken.Cancel();
        }

        protected Bitmap LoadImage(string fileName)
        {
            return new Bitmap(fileName).ConvertToFormat(PixelFormat.Format24bppRgb);
        }

        public static List<TemplateMatch> Detect(float similiarityThreshold, Bitmap sourceImage, params Bitmap[] templates)
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

        protected Point GetWindowPointFromArea(TemplateMatch m, Rectangle area)
        {
            return NativeMethods.MatchRectangleToPoint(area, m.Rectangle);
        }

        public async Task<bool> RequestInputFocus(Action action, int priority)
        {
            CancellationToken.Token.ThrowIfCancellationRequested();

            var focusAction = Scheduler.RequestInputFocus(this, action, priority, !RunsInForeground);

            await focusAction.TaskCompletion.Task;

            return focusAction.TaskCompletion.Task.Result;
        }

        protected void LeftClickOnPoint(Point p)
        {
            NativeMethods.ClientToVirtualScreen(Process, ref p);

            Simulator.Mouse.MoveMouseTo(p.X, p.Y);
            Simulator.Mouse.LeftButtonClick();
        }

        protected Bitmap Snapshot(Rectangle rect)
        {
            return NativeMethods.PrintWindow(Process, rect);
        }

        protected List<TemplateMatch> FindMatches(float similiarity, Bitmap source, params Bitmap[] templates)
        {
            List<TemplateMatch> matches = Detect(0.95f, source, templates);

            matches.Sort(Comparer);

            return matches;
        }

        protected bool Equals(ConquerTask other)
        {
            return string.Equals(TaskType, other.TaskType);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ConquerTask)obj);
        }

        public override int GetHashCode()
        {
            return TaskType.GetHashCode();
        }
    }
}
