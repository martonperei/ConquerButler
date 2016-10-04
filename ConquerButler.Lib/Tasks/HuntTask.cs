using AForge.Imaging;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace ConquerButler.Tasks
{
    public class HuntTask : ConquerTask
    {
        private readonly Bitmap xpFlyTemplate;
        private readonly Bitmap descendTemplate;
        private readonly Bitmap dropTemplate;

        public HuntTask(ConquerInputScheduler scheduler, ConquerProcess process)
            : base("Hunt", scheduler, process)
        {
            Interval = 1;

            RunsInForeground = true;

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
                    Point p = Helpers.GetCursorPosition(Process.InternalProcess);

                    Process.LeftClickOnPoint(Process.GetWindowPointFromArea(isXpFly[0], ConquerControls.XP_SKILLS));

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
                    Process.LeftClickOnPoint(Process.GetWindowPointFromArea(isDrop[0], ConquerControls.CHAT_AREA));
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
