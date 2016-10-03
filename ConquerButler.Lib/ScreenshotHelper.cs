using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;

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

    public class ScreenshotHelper
    {
        [DllImport("user32.dll")]
        internal static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")]
        internal static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

        public static Bitmap PrintWindow(Process process)
        {
            RECT rc;
            GetWindowRect(process.MainWindowHandle, out rc);

            if (rc.Width == 0 || rc.Height == 0)
            {
                return null;
            }

            Bitmap bmp = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb);
            Graphics gfxBmp = Graphics.FromImage(bmp);
            IntPtr hdcBitmap = gfxBmp.GetHdc();

            PrintWindow(process.MainWindowHandle, hdcBitmap, 0);

            gfxBmp.ReleaseHdc(hdcBitmap);
            gfxBmp.Dispose();

            return bmp.ConvertToFormat(PixelFormat.Format24bppRgb);
        }

        public static Bitmap CropBitmap(Bitmap bitmap, Rectangle tr)
        {
            Bitmap target = new Bitmap(tr.Width, tr.Height);

            using (Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(bitmap, new Rectangle(0, 0, target.Width, target.Height),
                                 tr,
                                 GraphicsUnit.Pixel);
            }

            return target.ConvertToFormat(PixelFormat.Format24bppRgb);
        }

        public static Bitmap PrintWindow(Process process, Rectangle rectangle)
        {
            RECT rc;
            GetWindowRect(process.MainWindowHandle, out rc);

            if (rc.Width == 0 || rc.Height == 0)
            {
                return null;
            }

            Rectangle tr = new Rectangle(rectangle.X + rc.X,
                rectangle.Y + rc.Y, rectangle.Width, rectangle.Height);

            Bitmap bmp = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb);
            Graphics gfxBmp = Graphics.FromImage(bmp);
            IntPtr hdcBitmap = gfxBmp.GetHdc();

            PrintWindow(process.MainWindowHandle, hdcBitmap, 0);

            gfxBmp.ReleaseHdc(hdcBitmap);
            gfxBmp.Dispose();

            Bitmap target = new Bitmap(tr.Width, tr.Height);

            using (Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(bmp, new Rectangle(0, 0, target.Width, target.Height),
                                 rectangle,
                                 GraphicsUnit.Pixel);
            }

            return target.ConvertToFormat(PixelFormat.Format24bppRgb);
        }

        public static Bitmap CaptureWindow(Process process)
        {
            RECT rc;
            GetWindowRect(process.MainWindowHandle, out rc);

            if (rc.Width == 0 || rc.Height == 0)
            {
                return null;
            }

            Bitmap bitmap = new Bitmap(rc.Width, rc.Height);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(new Point(rc.Left, rc.Top), Point.Empty, rc.Size);
            }
            return bitmap.ConvertToFormat(PixelFormat.Format24bppRgb);
        }

        public static Bitmap CaptureWindow(Process process, Rectangle rectangle)
        {
            RECT windowRectangle;
            GetWindowRect(process.MainWindowHandle, out windowRectangle);

            if (windowRectangle.Width == 0 || windowRectangle.Height == 0)
            {
                return null;
            }

            Rectangle tr = new Rectangle(rectangle.X + windowRectangle.X,
                rectangle.Y + windowRectangle.Y, rectangle.Width, rectangle.Height);

            Bitmap bitmap = new Bitmap(tr.Width, tr.Height);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(new Point(tr.Left, tr.Top), Point.Empty, tr.Size);
            }
            return bitmap.ConvertToFormat(PixelFormat.Format24bppRgb);
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
            Graphics g = Graphics.FromHwnd(process.MainWindowHandle);
            IntPtr desktop = g.GetHdc();
            int LogicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.VERTRES);
            int PhysicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.DESKTOPVERTRES);

            float ScreenScalingFactor = (float)PhysicalScreenHeight / (float)LogicalScreenHeight;

            return ScreenScalingFactor; // 1.25 = 125%
        }
    }
}
