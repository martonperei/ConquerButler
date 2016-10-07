using AForge.Imaging;
using log4net;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;

namespace ConquerButler
{
    public class TemplateMatchComparer : IComparer<TemplateMatch>
    {
        public const int EPSILON = 10;

        public int Compare(TemplateMatch x, TemplateMatch y)
        {
            int diff = Math.Abs(x.Rectangle.Y - y.Rectangle.Y);

            int c = diff < EPSILON ? 0 : x.Rectangle.Y.CompareTo(y.Rectangle.Y);

            if (c == 0)
            {
                return x.Rectangle.X.CompareTo(y.Rectangle.X);
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
        private static readonly TemplateMatchComparer _templateComparer = new TemplateMatchComparer();

        public const int DEFAULT_PRIORITY = 100;

        public event PropertyChangedEventHandler PropertyChanged;

        protected readonly ConquerScheduler Scheduler;

        public ConquerProcess Process { get; }
        public double Interval { get; set; } = 10;
        public int IntervalVariance { get; set; } = 1;
        public long StartTick { get; protected set; }
        public double NextRun { get; protected set; }

        public int Priority { get; protected set; } = DEFAULT_PRIORITY;

        public bool Enabled { get; protected set; } = true;
        public bool IsRunning { get; protected set; } = false;
        public bool IsPaused { get; protected set; } = true;
        public bool NeedsUserFocus { get; protected set; } = false;

        public string TaskType { get; protected set; }

        protected CancellationTokenSource CancellationToken;
        protected Task CurrentTask;

        protected readonly Random Random;

        public virtual string ResultDisplayInfo { get { return ""; } }

        public ConquerTask(string taskType, ConquerProcess process)
        {
            TaskType = taskType;
            Scheduler = process.Scheduler;
            Process = process;

            Random = new Random();
        }

        public void Add()
        {
            Process.AddTask(this);
        }

        public void Remove()
        {
            Cancel();

            Process.RemoveTask(this);
        }

        public void Tick(double dt)
        {
            if (Enabled && !IsRunning && !IsPaused)
            {
                if (NextRun <= 0)
                {
                    log.Info($"Process {Process.Id} - task {TaskType} running");

                    IsRunning = true;

                    CancellationToken = new CancellationTokenSource();

                    CurrentTask = Task.Run(async () =>
                    {
                        StartTick = Scheduler.Clock.Tick;
                        NextRun = 0;

                        try
                        {
                            await DoTick();
                        }
                        catch (Exception e)
                        {
                            log.Error($"Process {Process.Id} - task {TaskType} exception", e);

                            throw e;
                        }
                        finally
                        {
                            NextRun = Interval + Random.Next(-IntervalVariance, IntervalVariance);

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
            IsPaused = true;
        }

        public void Resume()
        {
            IsPaused = false;
        }

        public void Cancel()
        {
            IsPaused = false;

            IsRunning = false;

            Enabled = false;

            CancellationToken?.Cancel();
        }

        public async Task<bool> RequestInputFocus(Action action, int priority)
        {
            CancellationToken.Token.ThrowIfCancellationRequested();

            var focusAction = Scheduler.RequestInputFocus(this, action, priority, !NeedsUserFocus);

            await focusAction.TaskCompletion.Task;

            return focusAction.TaskCompletion.Task.Result;
        }

        protected Bitmap LoadImage(string fileName)
        {
            using (var bmp = new Bitmap(fileName))
            {
                Bitmap result = bmp.ConvertToFormat(PixelFormat.Format24bppRgb);
                result.Tag = fileName;
                return result;
            }
        }

        public List<TemplateMatch> FindMatches(float similiarity, Rectangle sourceRect, params Bitmap[] templates)
        {
            using (Bitmap source = Process.Screenshot())
            {
                if (source.Width < source.Width || source.Height < sourceRect.Height)
                {
                    return new List<TemplateMatch>();
                }

                ExhaustiveTemplateMatching tm = new ExhaustiveTemplateMatching(similiarity);

                List<TemplateMatch> matches = new List<TemplateMatch>();

                Stopwatch watch = new Stopwatch();

                foreach (Bitmap template in templates)
                {
                    watch.Restart();

                    TemplateMatch[] match = tm.ProcessImage(source, template, sourceRect);

                    log.Info($"Process {Process.Id} - task {TaskType} - {template.Tag} detection took {watch.ElapsedMilliseconds}ms");

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
