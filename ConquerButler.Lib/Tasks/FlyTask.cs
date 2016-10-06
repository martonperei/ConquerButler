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

        private readonly Bitmap _xpFlyTemplate;
        private readonly Bitmap _descendTemplate;

        public FlyTask(ConquerProcess process)
            : base(TASK_TYPE_NAME, process)
        {
            Interval = 10;

            NeedsUserFocus = true;

            _xpFlyTemplate = LoadImage("images/xpfly.png");
            _descendTemplate = LoadImage("images/descend.png");
        }

        public bool IsFlying()
        {
            return FindMatches(0.95f, ConquerControls.XP_SKILLS, _descendTemplate).Count > 0;
        }

        public async override Task DoTick()
        {
            List<TemplateMatch> isXpFly = FindMatches(0.95f, ConquerControls.XP_SKILLS, _xpFlyTemplate);

            if (isXpFly.Count > 0)
            {
                await RequestInputFocus(() =>
                {
                    Scheduler.Wait(1000);

                    Point p = Process.GetCursorPosition();

                    Process.LeftClickOnPoint(MatchToPoint(isXpFly[0]));

                    Scheduler.Wait(500);

                    Helpers.ClientToVirtualScreen(Process.InternalProcess, ref p);

                    Process.Simulator.Mouse.MoveMouseTo(p.X, p.Y);
                }, 1);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _xpFlyTemplate.Dispose();
            _descendTemplate.Dispose();
        }
    }
}
