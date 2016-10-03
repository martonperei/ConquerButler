using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ConquerButler
{
    [StructLayout(LayoutKind.Sequential)]
    struct POINT
    {
        public int X;
        public int Y;

        public static implicit operator Point(POINT point)
        {
            return new Point(point.X, point.Y);
        }
    }

    public class CursorHelper
    {
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("User32.Dll")]
        private static extern long SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref Point lpPoint);

        public static Point GetCursorPosition(Process process)
        {
            POINT lpPoint;
            GetCursorPos(out lpPoint);

            Point p = new Point(lpPoint.X, lpPoint.Y);

            ScreenToClient(process.MainWindowHandle, ref p);

            return p;
        }

        public static Point SetCursorPosition(Process process, Point p)
        {
            ClientToScreen(process.MainWindowHandle, ref p);

            SetCursorPos(p.X, p.Y);

            return p;
        }

        public static Point MatchRectangleToPoint(Rectangle offset, Rectangle matchRectangle)
        {
            Point p = new Point(offset.X + (matchRectangle.X + matchRectangle.Width / 2),
                offset.Top + (matchRectangle.Y + matchRectangle.Height / 2));

            return p;
        }

        public static Point ClientToVirtualScreen(Process process, Rectangle rect)
        {
            Point p = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);

            ClientToVirtualScreen(process, ref p);

            return p;
        }

        public static void ClientToVirtualScreen(Process process, ref Point p)
        {
            Rectangle screenBounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;

            ClientToScreen(process.MainWindowHandle, ref p);

            p.X = p.X * 65535 / screenBounds.Width;
            p.Y = p.Y * 65535 / screenBounds.Height;
        }
    }
}
