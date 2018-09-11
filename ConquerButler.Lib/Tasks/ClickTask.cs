﻿using log4net;
using System.Threading.Tasks;

namespace ConquerButler.Tasks
{
    public enum MouseButton
    {
        Left,
        Right
    }

    public class ClickTask : ConquerTask
    {
        private static ILog log = LogManager.GetLogger(typeof(ClickTask));

        public static string TASK_TYPE_NAME = "Click";

        public MouseButton MouseButton { get; set; } = MouseButton.Left;
        public bool HoldCtrl { get; set; } = false;
        public int Wait { get; set; } = 500;

        public ClickTask(ConquerProcess process)
            : base(TASK_TYPE_NAME, process)
        {
            Interval = 0;

            NeedsUserFocus = true;
        }

        public async override Task DoTick()
        {
            await EnqueueInputAction(async () =>
            {
                if (HoldCtrl)
                {
                    Process.Simulator.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.CONTROL);
                }

                switch (MouseButton)
                {
                    case MouseButton.Left:
                        Process.Simulator.Mouse.LeftButtonClick();
                        break;
                    case MouseButton.Right:
                        Process.Simulator.Mouse.RightButtonClick();
                        break;
                }

                if (HoldCtrl)
                {
                    Process.Simulator.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.CONTROL);
                }

                await Scheduler.Delay(Wait);
            }, 1);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
