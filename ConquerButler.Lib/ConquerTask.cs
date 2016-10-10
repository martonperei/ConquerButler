using Accord.Extensions.Imaging.Algorithms.LINE2D;
using DotImaging;
using log4net;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TemplatePyramid = Accord.Extensions.Imaging.Algorithms.LINE2D.ImageTemplatePyramid<Accord.Extensions.Imaging.Algorithms.LINE2D.ImageTemplate>;
using System.Linq;
using System.Drawing;

namespace ConquerButler
{
    public class MatchComparer : IComparer<Match>
    {
        public const int EPSILON = 10;

        public int Compare(Match x, Match y)
        {
            int diff = Math.Abs(x.BoundingRect.Y - y.BoundingRect.Y);

            int c = diff < EPSILON ? 0 : x.BoundingRect.Y.CompareTo(y.BoundingRect.Y);

            if (c == 0)
            {
                return x.BoundingRect.X.CompareTo(y.BoundingRect.X);
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

        private static readonly MatchComparer _matchComparer = new MatchComparer();

        public event PropertyChangedEventHandler PropertyChanged;

        protected readonly ConquerScheduler Scheduler;

        public ConquerProcess Process { get; }

        public string TaskType { get; protected set; }
        public bool Enabled { get; protected set; } = true;
        public bool IsRunning { get; protected set; } = false;
        public bool IsPaused { get; protected set; } = true;

        public long StartTick { get; protected set; }
        public double NextRun { get; protected set; }

        public double Interval { get; set; } = 10;
        public int IntervalVariance { get; set; } = 1;
        public int Priority { get; set; } = DEFAULT_PRIORITY;
        public bool NeedsUserFocus { get; set; } = false;
        public bool NeedsToBeConnected { get; set; } = true;

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
            if (!NeedsToBeConnected || (!Process.Disconnected && NeedsToBeConnected) &&
                Enabled && !IsRunning && !IsPaused)
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

                            log.Info($"Process {Process.Id} - task {TaskType} finished - {ResultDisplayInfo}");
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

        public void ForceRun()
        {
            if (!IsRunning)
            {
                NextRun = 0;
            }
        }

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

        public Task RequestInputFocus(Action action, int priority)
        {
            CancellationToken.Token.ThrowIfCancellationRequested();

            var focusAction = Scheduler.RequestInputFocus(this, action, priority);

            return focusAction.TaskCompletion.Task;
        }

        protected TemplatePyramid LoadTemplate(string fileName)
        {
            using (var image = new Bitmap(fileName))
            {
                return TemplatePyramid.CreatePyramid(image.ToBgr(), fileName);
            }
        }

        public List<Match> FindMatches(float similiarity, Rectangle sourceRect, params TemplatePyramid[] templates)
        {
            return FindMatches(similiarity, sourceRect, templates.ToList());
        }

        public List<Match> FindMatches(float similiarity, Rectangle sourceRect, List<TemplatePyramid> templates)
        {
            using (var screenshot = Process.Screenshot())
            using (var source = Helpers.CropBitmap(screenshot, sourceRect))
            {
                Stopwatch watch = new Stopwatch();

                watch.Start();

                LinearizedMapPyramid sourceTemplate = LinearizedMapPyramid.CreatePyramid(source.ToBgr());

                log.Debug($"Process {Process.Id} - task {TaskType} - preparing took {watch.ElapsedMilliseconds}ms");

                watch.Restart();

                List<Match> matches = sourceTemplate.MatchTemplates(templates, (int)(similiarity * 100), true);

                List<Match> bestMatches = new List<Match>();

                var matchGroups = new MatchClustering(0).Group(matches.ToArray());
                foreach (var group in matchGroups)
                {
                    var match = new Match
                    {
                        X = group.Representative.X + sourceRect.Left,
                        Y = group.Representative.Y + sourceRect.Top,
                        Score = group.Representative.Score,
                        Template = group.Representative.Template
                    };

                    bestMatches.Add(match);
                }

                log.Debug($"Process {Process.Id} - task {TaskType} - detection took {watch.ElapsedMilliseconds}ms");

                bestMatches.Sort(_matchComparer);

                return bestMatches;
            }
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
