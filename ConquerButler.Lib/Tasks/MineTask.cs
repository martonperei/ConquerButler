using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using AForge.Imaging;

namespace ConquerButler.Tasks
{
    public class MineTask : ConquerTask
    {
        private readonly Bitmap copperoreTemplate;
        private readonly Bitmap ironoreTemplate;

        private static object synchronization = new object();

        public int OreCount { get; set; }

        public override string DisplayInfo { get { return $"{TaskType} Ores: {OreCount} | Running: {IsRunning} Next run: {NextRun} ms"; } }

        public MineTask(ConquerInputScheduler scheduler, ConquerProcess process)
            : base("Mine", scheduler, process)
        {
            copperoreTemplate = LoadImage("images/copperore.png");
            ironoreTemplate = LoadImage("images/ironore.png");

            Interval = 10000;
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
                    Process.LeftClickOnPoint(Process.GetWindowPointFromArea(m, ConquerControls.INVENTORY));
                    Scheduler.Wait(250);
                    Process.LeftClickOnPoint(new Point(700, 100));
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
