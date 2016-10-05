using AForge.Imaging;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace ConquerButler.Tasks
{
    public class HuntingTask : ConquerTask
    {
        private readonly Bitmap xpFlyTemplate;
        private readonly Bitmap descendTemplate;
        private readonly Bitmap dropTemplate;

        public HuntingTask(ConquerProcess process)
            : base("Hunting", process)
        {
            Interval = 1;

            NeedsUserFocus = true;

            xpFlyTemplate = LoadImage("images/xpfly.png");
            descendTemplate = LoadImage("images/descend.png");
            dropTemplate = LoadImage("images/drop.png");
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

            List<TemplateMatch> isDrop = Process.FindMatches(0.95f, ConquerControls.CHAT_AREA, dropTemplate);

            if (isDrop.Count > 0)
            {
                await RequestInputFocus(() =>
                {
                    Process.LeftClickOnPoint(Process.MatchToPoint(isDrop[0]));
                }, 1);
            }

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

            xpFlyTemplate.Dispose();
            descendTemplate.Dispose();
            dropTemplate.Dispose();
        }
    }
}
