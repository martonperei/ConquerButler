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
            Interval = 100;

            NeedsUserFocus = true;
        }

        protected async override Task DoTick()
        {
            await EnqueueInputAction(() =>
            {
                Process.Simulator.Mouse.RightButtonClick();

                return Task.CompletedTask;
            });

            await Delay(125);

            await EnqueueInputAction(() =>
            {
                Process.Simulator.Mouse.LeftButtonClick();

                return Task.CompletedTask;
            });

            await Delay(125);

            await EnqueueInputAction(() =>
            {
                Process.Simulator.Mouse.RightButtonClick();

                return Task.CompletedTask;
            });

            await Delay(125);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
