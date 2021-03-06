using Accord.Extensions.Imaging.Algorithms.LINE2D;
using ConquerButler.Native;
using DotImaging;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using TemplatePyramid = Accord.Extensions.Imaging.Algorithms.LINE2D.ImageTemplatePyramid<Accord.Extensions.Imaging.Algorithms.LINE2D.ImageTemplate>;

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

    public abstract class ConquerTask : IDisposable
    {
        private static ILog log = LogManager.GetLogger(typeof(ConquerTask));

        public const int DEFAULT_PRIORITY = 100;

        private static readonly MatchComparer _matchComparer = new MatchComparer();

        public ConquerScheduler Scheduler { get; }

        public ConquerProcess Process { get; }

        public bool Running { get; internal protected set; }
        public bool Started { get; internal protected set; }
        public bool Enabled { get; internal protected set; }

        public long StartTime { get; internal protected set; }

        public long NextRun
        {
            get
            {
                return !Enabled || Running ? 0 : Math.Max((StartTime + Interval) - Scheduler.Clock.ElapsedMilliseconds, 0);
            }
        }

        public virtual string ResultDisplayInfo { get { return ""; } }

        public string TaskType { get; set; }
        public int Interval { get; set; } = 10000;
        public int Priority { get; set; } = DEFAULT_PRIORITY;
        public bool NeedsUserFocus { get; set; } = false;
        public bool NeedsToBeConnected { get; set; } = true;

        protected readonly Random Random;

        public ConquerTask(string taskType, ConquerProcess process)
        {
            TaskType = taskType;
            Scheduler = process.Scheduler;
            Process = process;

            Random = new Random();
        }

        internal protected abstract Task Tick();

        public void Start()
        {
            if (!Enabled)
            {
                Enabled = true;

                log.Info($"Process {Process.Id} - task {TaskType} enabled");
            }
        }

        public void Stop()
        {
            if (Enabled)
            {
                Enabled = false;

                log.Info($"Process {Process.Id} - task {TaskType} disabled");
            }
        }

        public virtual Task OnHealthChanged(int previous, int current)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnManaChanged(int previous, int current)
        {
            return Task.CompletedTask;
        }

        public async Task EnqueueInputAction(Func<Task> action, int priority = 1)
        {
            await Scheduler.AddInputAction(this, action, priority);
        }

        public Task Delay(int delay, int variance = 50)
        {
            return Scheduler.Delay(this, delay + Random.Next(-variance, variance));
        }

        protected TemplatePyramid LoadTemplate(string fileName, string class_ = null)
        {
            using (var image = new Bitmap(fileName))
            {

                return TemplatePyramid.CreatePyramid(image.ToBgr(), class_ ?? fileName);
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
            return Process.Equals(other.Process) && string.Equals(TaskType, other.TaskType);
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
            return Process.GetHashCode() ^ TaskType.GetHashCode();
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
                Stop();
            }
        }
    }
}
