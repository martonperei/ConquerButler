using Accord.Extensions.Imaging.Algorithms.LINE2D;
using log4net;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TemplatePyramid = Accord.Extensions.Imaging.Algorithms.LINE2D.ImageTemplatePyramid<Accord.Extensions.Imaging.Algorithms.LINE2D.ImageTemplate>;

namespace ConquerButler.Tasks
{
    public class ItemFindPauseTask : ConquerTask
    {
        private static ILog log = LogManager.GetLogger(typeof(HuntingTask));

        public static string TASK_TYPE_NAME = "ItemFind";

        private readonly TemplatePyramid _dropTemplate;

        public ItemFindPauseTask(ConquerProcess process)
            : base(TASK_TYPE_NAME, process)
        {
            Interval = 10;

            _dropTemplate = LoadTemplate("images/drop.png");
        }

        public override Task DoTick()
        {
            List<Match> isDrop = FindMatches(0.95f, ConquerControls.CHAT_AREA, _dropTemplate);

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
        }
    }
}
