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

        private readonly Bitmap _copperoreTemplate;
        private readonly Bitmap _ironoreTemplate;

        public int OreCount { get; protected set; }

        public override string ResultDisplayInfo { get { return $"{OreCount}"; } }

        public MiningTask(ConquerProcess process)
            : base(TASK_TYPE_NAME, process)
        {
            _copperoreTemplate = LoadImage("images/copperore.png");
            _ironoreTemplate = LoadImage("images/ironore.png");

            Interval = 240;
            IntervalVariance = 60;
        }

        public override async Task DoTick()
        {
            List<TemplateMatch> matches = FindMatches(0.95f, ConquerControls.INVENTORY, _ironoreTemplate, _copperoreTemplate);

            OreCount = matches.Count;

            List<Task> taskList = new List<Task>();

            for (int i = matches.Count - 1; i >= 0 && i >= matches.Count - 19; i--)
            {
                TemplateMatch m = matches[i];

                taskList.Add(RequestInputFocus(() =>
                {
                    Process.LeftClickOnPoint(MatchToPoint(m));
                    Scheduler.Wait(250);
                    Process.LeftClickOnPoint(new Point(700, 100), 40);
                    Scheduler.Wait(250);

                    OreCount--;
                }, i));
            }

            await Task.WhenAll(taskList);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _copperoreTemplate.Dispose();
            _ironoreTemplate.Dispose();
        }
    }
}
