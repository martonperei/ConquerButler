using Accord.Extensions.Imaging.Algorithms.LINE2D;
using log4net;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
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
            Interval = 100;

            _dropTemplate = LoadTemplate("images/drop.png");
        }

        protected override Task Tick()
        {
            List<Match> isDrop = FindMatches(0.95f, ConquerControlConstants.CHAT_AREA, _dropTemplate);

            if (isDrop.Count > 0)
            {
                foreach (ConquerTask task in Scheduler.Tasks.Where(t => t.Process.Equals(Process) && t.NeedsUserFocus))
                {
                    task.Stop();
                }

                MessageBox.Show($"Item dropped");

                Stop();
            }

            return Task.FromResult(true);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
