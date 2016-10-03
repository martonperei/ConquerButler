using AForge.Imaging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace ConquerButler.Tasks
{
    public class HuntTask : ConquerTask
    {
        private readonly Bitmap xpFlyTemplate;
        private readonly Bitmap descendTemplate;
        private readonly Bitmap dropTemplate;

        public HuntTask(ConquerInputScheduler scheduler, Process process) 
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
            return FindMatches(0.95f, Snapshot(ConquerControls.XP_SKILLS), descendTemplate).Count > 0;
        }

        public async override Task DoTick()
        {
            List<TemplateMatch> isXpFly = FindMatches(0.95f, Snapshot(ConquerControls.XP_SKILLS), xpFlyTemplate);

            if (isXpFly.Count > 0)
            {
                await RequestInputFocus(() =>
                {
                    Point p = CursorHelper.GetCursorPosition(Process);

                    LeftClickOnPoint(GetWindowPointFromArea(isXpFly[0], ConquerControls.XP_SKILLS));

                    Scheduler.Wait(250);

                    CursorHelper.ClientToVirtualScreen(Process, ref p);

                    Simulator.Mouse.MoveMouseTo(p.X, p.Y);
                }, 1);
            }

            List<TemplateMatch> isDrop = FindMatches(0.95f, Snapshot(ConquerControls.CHAT_AREA), dropTemplate);

            if (isDrop.Count > 0)
            {
                await RequestInputFocus(() =>
                {
                    LeftClickOnPoint(GetWindowPointFromArea(isDrop[0], ConquerControls.CHAT_AREA));
                }, 1);
            }

            await RequestInputFocus(() =>
            {
                Simulator.Mouse.RightButtonClick();
                Scheduler.Wait(125);
            }, 1);

            await RequestInputFocus(() =>
            {
                Simulator.Mouse.LeftButtonClick();
                Scheduler.Wait(125);
            }, 1);

            await RequestInputFocus(() =>
            {
                Simulator.Mouse.RightButtonClick();
                Scheduler.Wait(125);
            }, 1);
        }
    }
}
