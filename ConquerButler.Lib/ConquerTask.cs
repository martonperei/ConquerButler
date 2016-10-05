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

        public event PropertyChangedEventHandler PropertyChanged;

        protected readonly ConquerScheduler Scheduler;

        public ConquerProcess Process { get; }
        public double Interval { get; set; } = 10;
        public long StartTick { get; set; }
        public double NextRun { get; set; }
        public bool Enabled { get; set; } = true;
        public int Priority { get; protected set; } = DEFAULT_PRIORITY;
        public bool IsRunning { get; protected set; }
        public bool IsPaused { get; protected set; }
        public bool NeedsUserFocus { get; protected set; } = false;
        public string TaskType { get; protected set; }

        protected CancellationTokenSource CancellationToken;
        protected Task CurrentTask;

        public virtual string DisplayInfo { get { return $"{TaskType} Running: {IsRunning} Next run: {NextRun} ms"; } }

        public ConquerTask(string taskType, ConquerProcess process)
        {
            TaskType = taskType;
            Scheduler = process.Scheduler;
            Process = process;
        }

        public void Start()
        {
            Process.AddTask(this);
        }

        public void Stop()
        {
            Cancel();

            Process.RemoveTask(this);
        }

        public void Tick(double dt)
        {
            if (Enabled && !IsRunning)
            {
                if (NextRun <= 0)
                {
                    log.Info($"{TaskType} Running");

                    IsRunning = true;

                    CancellationToken = new CancellationTokenSource();

                    CurrentTask = Task.Run(async () =>
                    {
                        StartTick = Scheduler.Clock.Tick;
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
                }
                else
                {
                    NextRun -= dt;
                }
            }
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
            using (var bmp = new Bitmap(fileName))
            {
                return bmp.ConvertToFormat(PixelFormat.Format24bppRgb);
            }
        }

        public async Task<bool> RequestInputFocus(Action action, int priority)
        {
            CancellationToken.Token.ThrowIfCancellationRequested();

            var focusAction = Scheduler.RequestInputFocus(this, action, priority, !NeedsUserFocus);

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
