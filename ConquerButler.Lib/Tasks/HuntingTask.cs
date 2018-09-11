using log4net;
using System.Threading.Tasks;

namespace ConquerButler.Tasks
{
    public class HuntingTask : ConquerTask
    {
        private static ILog log = LogManager.GetLogger(typeof(HuntingTask));

        public static string TASK_TYPE_NAME = "Hunting";

        public HuntingTask(ConquerProcess process)
            : base(TASK_TYPE_NAME, process)
        {
            Interval = 0;

            NeedsUserFocus = true;
        }

        public async override Task DoTick()
        {
            await EnqueueInputAction(async () =>
            {
                Process.Simulator.Mouse.RightButtonClick();
                await Scheduler.Delay(125);
            }, 1);

            await EnqueueInputAction(async () =>
            {
                Process.Simulator.Mouse.LeftButtonClick();
                await Scheduler.Delay(125);
            }, 1);

            await EnqueueInputAction(async () =>
            {
                Process.Simulator.Mouse.RightButtonClick();
                await Scheduler.Delay(125);
            }, 1);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
