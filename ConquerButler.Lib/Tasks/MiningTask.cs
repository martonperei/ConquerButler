using Accord.Extensions.Imaging.Algorithms.LINE2D;
using log4net;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Drawing;
using TemplatePyramid = Accord.Extensions.Imaging.Algorithms.LINE2D.ImageTemplatePyramid<Accord.Extensions.Imaging.Algorithms.LINE2D.ImageTemplate>;
using WindowsInput.Native;

namespace ConquerButler.Tasks
{
    public class MiningTask : ConquerTask
    {
        private static ILog log = LogManager.GetLogger(typeof(MiningTask));

        public static string TASK_TYPE_NAME = "Mining";

        private static Point DROP_POINT = new Point(700, 100);
        private const int MAX_DROP_DROP = 9;

        private readonly TemplatePyramid _copperoreTemplate;
        private readonly TemplatePyramid _ironoreTemplate;

        public int HealthThreshold { get; set; } = 50;

        public bool BeingDisconnected { get; protected set; } = false;

        public int OreCount { get; protected set; }
        public int TotalOresDropped { get; protected set; }

        public override string ResultDisplayInfo { get { return $"{OreCount} > {TotalOresDropped}"; } }

        public MiningTask(ConquerProcess process)
            : base(TASK_TYPE_NAME, process)
        {
            _copperoreTemplate = LoadTemplate("images/copperore.png");
            _ironoreTemplate = LoadTemplate("images/ironore.png");

            Interval = 120000;
        }

        public override async Task OnHealthChanged(int previous, int current)
        {
            if (current < HealthThreshold && !BeingDisconnected)
            {
                BeingDisconnected = true;

                await EnqueueInputAction(() =>
                {
                    Process.Close();

                    return Task.CompletedTask;
                });
            }
        }

        protected internal override async Task Tick()
        {
            List<Match> matches = FindMatches(0.93f, ConquerControlConstants.INVENTORY, _ironoreTemplate, _copperoreTemplate);

            OreCount = matches.Count;

            log.Info($"Found {OreCount} ores...");

            for (int i = matches.Count - 1; i >= 0 && i >= matches.Count - MAX_DROP_DROP; i--)
            {
                Match m = matches[i];

                await EnqueueInputAction(async () =>
                {
                    Process.LeftClick(m.Center());
                    await Delay(250);
                    Process.LeftClick(DROP_POINT, 20);
                    await Delay(250);

                    OreCount--;
                    TotalOresDropped++;
                }, i);
            }

            await EnqueueInputAction(() =>
            {
                Process.RightClick(DROP_POINT, 20);

                return Task.CompletedTask;
            }, 0);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
