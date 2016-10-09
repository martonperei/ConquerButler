using Accord.Extensions.Imaging.Algorithms.LINE2D;
using log4net;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using TemplatePyramid = Accord.Extensions.Imaging.Algorithms.LINE2D.ImageTemplatePyramid<Accord.Extensions.Imaging.Algorithms.LINE2D.ImageTemplate>;

namespace ConquerButler.Tasks
{
    public class FlyTask : ConquerTask
    {
        private static ILog log = LogManager.GetLogger(typeof(FlyTask));

        public static string TASK_TYPE_NAME = "Fly";

        private readonly TemplatePyramid _xpFlyTemplate;
        private readonly TemplatePyramid _descendTemplate;

        public FlyTask(ConquerProcess process)
            : base(TASK_TYPE_NAME, process)
        {
            Interval = 10;

            NeedsUserFocus = true;

            _xpFlyTemplate = LoadTemplate("images/xpfly.png");
            _descendTemplate = LoadTemplate("images/descend.png");
        }

        public bool IsFlying()
        {
            return FindMatches(0.95f, ConquerControls.XP_SKILLS, _descendTemplate).Count > 0;
        }

        public async override Task DoTick()
        {
            List<Match> isXpFly = FindMatches(0.95f, ConquerControls.XP_SKILLS, _xpFlyTemplate);

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
        }
    }
}
