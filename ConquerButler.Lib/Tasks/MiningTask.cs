using AForge.Imaging;
using log4net;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace ConquerButler.Tasks
{
    public class MiningTask : ConquerTask
    {
        private static ILog log = LogManager.GetLogger(typeof(MiningTask));

        public static string TASK_TYPE_NAME = "Mining";

        private readonly Bitmap copperoreTemplate;
        private readonly Bitmap ironoreTemplate;

        public int OreCount { get; protected set; }

        public override string DisplayInfo { get { return $"{TaskType} Ores: {OreCount} | Running: {IsRunning} Next run: {NextRun:F2}s"; } }

        public MiningTask(ConquerProcess process)
            : base(TASK_TYPE_NAME, process)
        {
            copperoreTemplate = LoadImage("images/copperore.png");
            ironoreTemplate = LoadImage("images/ironore.png");

            Interval = 60;
        }

        public override async Task DoTick()
        {
            List<TemplateMatch> matches = Process.FindMatches(0.95f, ConquerControls.INVENTORY, ironoreTemplate, copperoreTemplate);

            OreCount = matches.Count;

            for (int i = 0; i < matches.Count && i < 19; i++)
            {
                TemplateMatch m = matches[i];

                await RequestInputFocus(() =>
                {
                    Process.LeftClickOnPoint(Process.MatchToPoint(m));
                    Scheduler.Wait(250);
                    Process.LeftClickOnPoint(new Point(700, 100), 40);
                    Scheduler.Wait(250);
                }, i);

                OreCount--;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            copperoreTemplate.Dispose();
            ironoreTemplate.Dispose();
        }
    }
}
