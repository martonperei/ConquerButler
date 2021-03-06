using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;

namespace ConquerButler.Native
{
    public class Helpers
    {
        public static Point GetCursorPosition(Process process)
        {
            POINT lpPoint;
            NativeMethods.GetCursorPos(out lpPoint);

            Point p = new Point(lpPoint.X, lpPoint.Y);

            NativeMethods.ScreenToClient(process.MainWindowHandle, ref p);

            return p;
        }

        public static Point SetCursorPosition(Process process, Point p)
        {
            NativeMethods.ClientToScreen(process.MainWindowHandle, ref p);

            NativeMethods.SetCursorPos(p.X, p.Y);

            return p;
        }

        public static bool IsCursorInsideWindow(Point p, Process process)
        {
            if (p.X < 0 || p.Y < 0)
            {
                return false;
            }

            RECT rc;
            NativeMethods.GetWindowRect(process.MainWindowHandle, out rc);

            if (p.X < rc.Width && p.Y < rc.Height)
            {
                return true;
            }
            else
            {
                return false;
            }
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

            NativeMethods.ClientToScreen(process.MainWindowHandle, ref p);

            p.X = p.X * 65535 / screenBounds.Width;
            p.Y = p.Y * 65535 / screenBounds.Height;
        }

        public static bool IsForegroundWindow(Process process)
        {
            return NativeMethods.GetForegroundWindow() == process.MainWindowHandle;
        }

        public static bool SetForegroundWindow(Process process)
        {
            NativeMethods.ShowWindow(process.MainWindowHandle, NativeMethods.SW_RESTORE);

            NativeMethods.SetForegroundWindow(process.MainWindowHandle);

            Stopwatch clock = new Stopwatch();
            clock.Start();

            while (NativeMethods.GetForegroundWindow() != process.MainWindowHandle)
            {
                if (clock.ElapsedMilliseconds > 2000)
                {
                    return false;
                }

                Thread.SpinWait(1);
            }

            return true;
        }

        public static string GetWindowTitle(IntPtr handle)
        {
            const int nChars = 256;
            StringBuilder buffer = new StringBuilder(nChars);

            if (NativeMethods.GetWindowText(handle, buffer, nChars) > 0)
            {
                return buffer.ToString();
            }
            return null;
        }

        public static string GetDialogText(IntPtr handle, int item)
        {
            const int nChars = 256;
            StringBuilder buffer = new StringBuilder(nChars);

            if (NativeMethods.GetDlgItemText(handle, item, buffer, nChars) > 0)
            {
                return buffer.ToString();
            }
            return null;
        }

        public static string GetClassName(IntPtr handle)
        {
            const int nChars = 256;
            StringBuilder buffer = new StringBuilder(nChars);

            if (NativeMethods.GetClassName(handle, buffer, nChars) > 0)
            {
                return buffer.ToString();
            }
            return null;
        }

        public static string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = NativeMethods.GetForegroundWindow();

            if (NativeMethods.GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }



        public static IEnumerable<Process> GetProcessesByTitle(string title)
        {
            return Process.GetProcesses().Where(process => process.MainWindowTitle.Equals(title));
        }

        public static IEnumerable<Process> GetProcesses(string processName)
        {
            return Process.GetProcesses().Where(process => process.ProcessName.StartsWith(processName));
        }

        public static List<IntPtr> GetAllChildHandles(IntPtr handle)
        {
            List<IntPtr> childHandles = new List<IntPtr>();

            EnumWindowProc childProc = new EnumWindowProc((hWnd, lParam) =>
            {
                childHandles.Add(hWnd);

                return true;
            });

            NativeMethods.EnumChildWindows(handle, childProc, IntPtr.Zero);

            return childHandles;
        }

        public static List<IntPtr> GetAllChildHandles(Process process)
        {
            var handles = new List<IntPtr>();

            foreach (ProcessThread thread in process.Threads)
            {
                NativeMethods.EnumThreadWindows(thread.Id, (hWnd, lParam) =>
                {
                    handles.Add(hWnd);
                    return true;
                },
                IntPtr.Zero);
            }

            return handles;
        }

        public static Bitmap CropBitmap(Bitmap bitmap, Rectangle tr)
        {
            Bitmap target = new Bitmap(tr.Width, tr.Height, PixelFormat.Format24bppRgb);

            using (Graphics g = Graphics.FromImage(target))
            {
                g.DrawImage(bitmap, new Rectangle(0, 0, target.Width, target.Height),
                                    tr,
                                    GraphicsUnit.Pixel);
            }

            return target;
        }

        public static Predicate<Color> YELLOW_FILTER = (c) =>
        {
            return c.GetHue() == 60 && c.GetBrightness() == 0.5f;
        };

        public static Bitmap PrintWindow(Process process)
        {
            RECT rc;
            NativeMethods.GetWindowRect(process.MainWindowHandle, out rc);

            if (rc.Width == 0 || rc.Height == 0)
            {
                return null;
            }

            Bitmap bmp = new Bitmap(rc.Width, rc.Height, PixelFormat.Format24bppRgb);

            using (Graphics gfxBmp = Graphics.FromImage(bmp))
            {
                IntPtr hdcBitmap = gfxBmp.GetHdc();
                try
                {
                    NativeMethods.PrintWindow(process.MainWindowHandle, hdcBitmap, 0);
                }
                finally
                {
                    gfxBmp.ReleaseHdc(hdcBitmap);
                }
            }

            NativeMethods.InvalidateRect(process.MainWindowHandle, IntPtr.Zero, true);
            //NativeMethods.UpdateWindow(process.MainWindowHandle);

            return bmp;
        }

        public static float GetScalingFactor(Process process)
        {
            using (Graphics g = Graphics.FromHwnd(process.MainWindowHandle))
            {
                IntPtr desktop = g.GetHdc();
                try
                {
                    int LogicalScreenHeight = NativeMethods.GetDeviceCaps(desktop, (int)DeviceCap.VERTRES);
                    int PhysicalScreenHeight = NativeMethods.GetDeviceCaps(desktop, (int)DeviceCap.DESKTOPVERTRES);

                    float ScreenScalingFactor = (float)PhysicalScreenHeight / (float)LogicalScreenHeight;

                    return ScreenScalingFactor; // 1.25 = 125%
                }
                finally
                {
                    NativeMethods.DeleteObject(desktop);
                }
            }
        }
    }
}
