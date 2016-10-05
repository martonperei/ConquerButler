using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using AForge.Imaging;
using PropertyChanged;
using System;
using System.Threading;
using System.Drawing.Imaging;
using System.ComponentModel;
using log4net;

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
    public abstract class ConquerTask : IDisposable, INotifyPropertyChanged
    {
        private static ILog log = LogManager.GetLogger(typeof(ConquerTask));

        public const int DEFAULT_PRIORITY = 100;

        protected readonly ConquerInputScheduler Scheduler;

        public ConquerProcess Process { get; }
        public int Interval { get; set; } = 10000;
        public long StartTick { get; set; }
        public long NextRun { get; set; }
        public bool Enabled { get; set; } = true;
        public int Priority { get; protected set; } = DEFAULT_PRIORITY;
        public bool IsRunning { get; protected set; }
        public bool IsPaused { get; protected set; }
        public bool RunsInForeground { get; protected set; } = false;
        public string TaskType { get; protected set; }

        protected CancellationTokenSource CancellationToken;
        protected Task CurrentTask;

        public virtual string DisplayInfo { get { return $"{TaskType} Running: {IsRunning} Next run: {NextRun} ms"; } }

        public event PropertyChangedEventHandler PropertyChanged;

        public ConquerTask(string taskType, ConquerInputScheduler scheduler, ConquerProcess process)
        {
            TaskType = taskType;
            Scheduler = scheduler;
            Process = process;

            Process.Tasks.Add(this);
        }

        public Task Tick(int interval)
        {
            if (Enabled && !IsRunning)
            {
                if (NextRun <= 0)
                {
                    CancellationToken = new CancellationTokenSource();

                    CurrentTask = Task.Run(async () =>
                    {
                        IsRunning = true;

                        StartTick = Scheduler.CurrentTick;
                        NextRun = -1;

                        try
                        {
                            await DoTick();
                        }
                        finally
                        {
                            NextRun = Interval;

                            IsRunning = false;
                        }
                    }, CancellationToken.Token);

                    return CurrentTask;
                }
                else
                {
                    NextRun -= interval;
                }
            }

            return null;
        }

        public abstract Task DoTick();

        public void Pause()
        {
            Enabled = false;
        }

        public void Resume()
        {
            Enabled = true;
        }

        public void Cancel()
        {
            IsRunning = false;

            Enabled = false;

            CancellationToken.Cancel();
        }

        protected Bitmap LoadImage(string fileName)
        {
            return new Bitmap(fileName).ConvertToFormat(PixelFormat.Format24bppRgb);
        }

        public async Task<bool> RequestInputFocus(Action action, int priority)
        {
            CancellationToken.Token.ThrowIfCancellationRequested();

            var focusAction = Scheduler.RequestInputFocus(this, action, priority, !RunsInForeground);

            await focusAction.TaskCompletion.Task;

            return focusAction.TaskCompletion.Task.Result;
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ConquerTask()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Cancel();
            }
        }
    }
}
