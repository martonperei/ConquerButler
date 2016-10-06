using AForge.Imaging;
using log4net;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace ConquerButler.Tasks
{
    public class ItemFindPauseTask : ConquerTask
    {
        private static ILog log = LogManager.GetLogger(typeof(HuntingTask));

        public static string TASK_TYPE_NAME = "ItemFind";

        private readonly Bitmap _dropTemplate;

        public ItemFindPauseTask(ConquerProcess process)
            : base(TASK_TYPE_NAME, process)
        {
            Interval = 10;

            _dropTemplate = LoadImage("images/drop.png");
        }

        public override Task DoTick()
        {
            List<TemplateMatch> isDrop = FindMatches(0.95f, ConquerControls.CHAT_AREA, _dropTemplate);

            if (isDrop.Count > 0)
            {
                // TODO: what to pause?
                foreach (ConquerTask task in Process.Tasks.Where(t => t.NeedsUserFocus))
                {
                    task.Pause();
                }
            }

            return Task.FromResult(true);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _dropTemplate.Dispose();
        }
    }
}
