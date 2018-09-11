using Accord.Extensions.Imaging.Algorithms.LINE2D;
using log4net;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Drawing;
using TemplatePyramid = Accord.Extensions.Imaging.Algorithms.LINE2D.ImageTemplatePyramid<Accord.Extensions.Imaging.Algorithms.LINE2D.ImageTemplate>;
using ConquerButler.Native;

namespace ConquerButler.Tasks
{
    public class MiningTask : ConquerTask
    {
        private static ILog log = LogManager.GetLogger(typeof(MiningTask));

        public static string TASK_TYPE_NAME = "Mining";

        private static Point DROP_POINT = new Point(700, 100);

        private readonly TemplatePyramid _copperoreTemplate;
        private readonly TemplatePyramid _ironoreTemplate;

        public int OreCount { get; protected set; }
        public int TotalOresDropped { get; protected set; }

        public override string ResultDisplayInfo { get { return $"{OreCount} > {TotalOresDropped}"; } }

        public MiningTask(ConquerProcess process)
            : base(TASK_TYPE_NAME, process)
        {
            _copperoreTemplate = LoadTemplate("images/copperore.png");
            _ironoreTemplate = LoadTemplate("images/ironore.png");

            Interval = 120;
            IntervalVariance = 20;
        }

        public override async Task DoTick()
        {
            List<Match> matches = FindMatches(0.93f, ConquerControlConstants.INVENTORY, _ironoreTemplate, _copperoreTemplate);

            OreCount = matches.Count;

            log.Info($"Found {OreCount} ores...");

            List<Task> taskList = new List<Task>();

            for (int i = matches.Count - 1; i >= 0 && i >= matches.Count - 19; i--)
            {
                Match m = matches[i];

                taskList.Add(EnqueueInputAction(async () =>
                {
                    Process.LeftClick(m.Center());
                    await Scheduler.Delay(250);
                    Process.LeftClick(DROP_POINT, 20);
                    await Scheduler.Delay(250);

                    OreCount--;
                    TotalOresDropped++;
                }, i));
            }

            taskList.Add(EnqueueInputAction(() =>
            {
                Process.RightClick(DROP_POINT, 20);

                return Task.CompletedTask;
            }, 0));

            await Task.WhenAll(taskList);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
