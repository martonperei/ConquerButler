using log4net;
using System.Drawing;
using System.Threading.Tasks;

namespace ConquerButler.Tasks
{
    public class HuntingTask : ConquerTask
    {
        private static ILog log = LogManager.GetLogger(typeof(HuntingTask));

        public static string TASK_TYPE_NAME = "Hunting";

        private readonly Bitmap xpFlyTemplate;
        private readonly Bitmap descendTemplate;
        private readonly Bitmap dropTemplate;

        public HuntingTask(ConquerProcess process)
            : base(TASK_TYPE_NAME, process)
        {
            Interval = 1;

            NeedsUserFocus = true;
        }

        public async override Task DoTick()
        {
            await RequestInputFocus(() =>
            {
                Process.Simulator.Mouse.RightButtonClick();
                Scheduler.Wait(125);
            }, 1);

            await RequestInputFocus(() =>
            {
                Process.Simulator.Mouse.LeftButtonClick();
                Scheduler.Wait(125);
            }, 1);

            await RequestInputFocus(() =>
            {
                Process.Simulator.Mouse.RightButtonClick();
                Scheduler.Wait(125);
            }, 1);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
