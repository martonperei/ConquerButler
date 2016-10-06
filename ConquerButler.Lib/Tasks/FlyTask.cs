using AForge.Imaging;
using log4net;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace ConquerButler.Tasks
{
    public class FlyTask : ConquerTask
    {
        private static ILog log = LogManager.GetLogger(typeof(FlyTask));

        public static string TASK_TYPE_NAME = "Fly";

        private readonly Bitmap xpFlyTemplate;
        private readonly Bitmap descendTemplate;

        public FlyTask(ConquerProcess process)
            : base(TASK_TYPE_NAME, process)
        {
            Interval = 10;

            NeedsUserFocus = true;

            xpFlyTemplate = LoadImage("images/xpfly.png");
            descendTemplate = LoadImage("images/descend.png");
        }

        public bool IsFlying()
        {
            return Process.FindMatches(0.95f, ConquerControls.XP_SKILLS, descendTemplate).Count > 0;
        }

        public async override Task DoTick()
        {
            List<TemplateMatch> isXpFly = Process.FindMatches(0.95f, ConquerControls.XP_SKILLS, xpFlyTemplate);

            if (isXpFly.Count > 0)
            {
                await RequestInputFocus(() =>
                {
                    Point p = Process.GetCursorPosition();

                    Process.LeftClickOnPoint(Process.MatchToPoint(isXpFly[0]));

                    Scheduler.Wait(250);

                    Helpers.ClientToVirtualScreen(Process.InternalProcess, ref p);

                    Process.Simulator.Mouse.MoveMouseTo(p.X, p.Y);
                }, 1);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            xpFlyTemplate.Dispose();
            descendTemplate.Dispose();
        }
    }
}
