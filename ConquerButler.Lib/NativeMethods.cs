using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Linq;

namespace ConquerButler
{
    [StructLayout(LayoutKind.Sequential)]
    struct RECT
    {
        private int _Left;
        private int _Top;
        private int _Right;
        private int _Bottom;

        public RECT(RECT Rectangle) : this(Rectangle.Left, Rectangle.Top, Rectangle.Right, Rectangle.Bottom)
        {
        }
        public RECT(int Left, int Top, int Right, int Bottom)
        {
            _Left = Left;
            _Top = Top;
            _Right = Right;
            _Bottom = Bottom;
        }

        public int X
        {
            get { return _Left; }
            set { _Left = value; }
        }
        public int Y
        {
            get { return _Top; }
            set { _Top = value; }
        }
        public int Left
        {
            get { return _Left; }
            set { _Left = value; }
        }
        public int Top
        {
            get { return _Top; }
            set { _Top = value; }
        }
        public int Right
        {
            get { return _Right; }
            set { _Right = value; }
        }
        public int Bottom
        {
            get { return _Bottom; }
            set { _Bottom = value; }
        }
        public int Height
        {
            get { return _Bottom - _Top; }
            set { _Bottom = value + _Top; }
        }
        public int Width
        {
            get { return _Right - _Left; }
            set { _Right = value + _Left; }
        }
        public Point Location
        {
            get { return new Point(Left, Top); }
            set
            {
                _Left = value.X;
                _Top = value.Y;
            }
        }
        public Size Size
        {
            get { return new Size(Width, Height); }
            set
            {
                _Right = value.Width + _Left;
                _Bottom = value.Height + _Top;
            }
        }

        public static implicit operator Rectangle(RECT Rectangle)
        {
            return new Rectangle(Rectangle.Left, Rectangle.Top, Rectangle.Width, Rectangle.Height);
        }
        public static implicit operator RECT(Rectangle Rectangle)
        {
            return new RECT(Rectangle.Left, Rectangle.Top, Rectangle.Right, Rectangle.Bottom);
        }
        public static bool operator ==(RECT Rectangle1, RECT Rectangle2)
        {
            return Rectangle1.Equals(Rectangle2);
        }
        public static bool operator !=(RECT Rectangle1, RECT Rectangle2)
        {
            return !Rectangle1.Equals(Rectangle2);
        }

        public override string ToString()
        {
            return "{Left: " + _Left + "; " + "Top: " + _Top + "; Right: " + _Right + "; Bottom: " + _Bottom + "}";
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public bool Equals(RECT Rectangle)
        {
            return Rectangle.Left == _Left && Rectangle.Top == _Top && Rectangle.Right == _Right && Rectangle.Bottom == _Bottom;
        }

        public override bool Equals(object Object)
        {
            if (Object is RECT)
            {
                return Equals((RECT)Object);
            }
            else if (Object is Rectangle)
            {
                return Equals(new RECT((Rectangle)Object));
            }

            return false;
        }
    }

    public enum PROCESS_DPI_AWARENESS
    {
        Process_DPI_Unaware = 0,
        Process_System_DPI_Aware = 1,
        Process_Per_Monitor_DPI_Aware = 2
    }

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

    public class NativeMethods
    {
        [DllImport("user32.dll")]
        internal static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")]
        internal static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("SHCore.dll", SetLastError = true)]
        public static extern bool SetProcessDpiAwareness(PROCESS_DPI_AWARENESS awareness);
        [DllImport("SHCore.dll", SetLastError = true)]
        public static extern void GetProcessDpiAwareness(IntPtr hprocess, out PROCESS_DPI_AWARENESS awareness);

        [DllImport("user32.dll")]
        private static extern int ShowWindow(IntPtr hWnd, uint Msg);

        private const uint SW_RESTORE = 0x09;

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("User32.Dll")]
        private static extern long SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref Point lpPoint);

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

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

        public static bool IsCursorInsideWindow(Point p, Process process)
        {
            if (p.X < 0 || p.Y < 0)
            {
                return false;
            }

            RECT rc;
            GetWindowRect(process.MainWindowHandle, out rc);

            if (p.X < rc.Width && p.Y < rc.Height)
            {
                return true;
            }
            else
            {
                return false;
            }
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

        public static bool IsForegroundWindow(Process process)
        {
            return GetForegroundWindow() == process.MainWindowHandle;
        }

        public static bool SetForegroundWindow(Process process)
        {
            if (IsForegroundWindow(process))
            {
                return true;
            }

            SetForegroundWindow(process.MainWindowHandle);

            Stopwatch clock = new Stopwatch();
            clock.Start();

            while (GetForegroundWindow() != process.MainWindowHandle)
            {
                if (clock.ElapsedMilliseconds > 2000)
                {
                    return false;
                }
            }

            return true;
        }

        public static string GetWindowTitle(IntPtr handle)
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }

        public static string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }

        public static IEnumerable<Process> GetProcesses(string processName)
        {
            return Process.GetProcesses().Where(process => process.ProcessName.StartsWith(processName));
        }

        public static Bitmap CropBitmap(Bitmap bitmap, Rectangle tr)
        {
            using (Bitmap target = new Bitmap(tr.Width, tr.Height))
            {
                using (Graphics g = Graphics.FromImage(target))
                {
                    g.DrawImage(bitmap, new Rectangle(0, 0, target.Width, target.Height),
                                     tr,
                                     GraphicsUnit.Pixel);
                }

                return target.ConvertToFormat(PixelFormat.Format24bppRgb);
            }
        }

        public static Bitmap PrintWindow(Process process)
        {
            RECT rc;
            GetWindowRect(process.MainWindowHandle, out rc);

            if (rc.Width == 0 || rc.Height == 0)
            {
                return null;
            }

            using (Bitmap bmp = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb))
            {
                using (Graphics gfxBmp = Graphics.FromImage(bmp))
                {
                    IntPtr hdcBitmap = gfxBmp.GetHdc();
                    try
                    {
                        PrintWindow(process.MainWindowHandle, hdcBitmap, 0);
                    }
                    finally
                    {
                        gfxBmp.ReleaseHdc(hdcBitmap);
                    }
                }

                return bmp.ConvertToFormat(PixelFormat.Format24bppRgb);
            }
        }

        public static Bitmap PrintWindow(Process process, Rectangle rectangle)
        {
            using (Bitmap bmp = PrintWindow(process))
            {
                return CropBitmap(bmp, rectangle);
            }
        }

        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        public enum DeviceCap
        {
            VERTRES = 10,
            DESKTOPVERTRES = 117,
        }

        public static float GetScalingFactor(Process process)
        {
            using (Graphics g = Graphics.FromHwnd(process.MainWindowHandle))
            {
                IntPtr desktop = g.GetHdc();
                try
                {
                    int LogicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.VERTRES);
                    int PhysicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.DESKTOPVERTRES);

                    float ScreenScalingFactor = (float)PhysicalScreenHeight / (float)LogicalScreenHeight;

                    return ScreenScalingFactor; // 1.25 = 125%
                } finally
                {
                    DeleteObject(desktop);
                }
            }
        }
    }
}
