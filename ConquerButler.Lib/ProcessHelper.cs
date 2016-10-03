using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace ConquerButler
{
    public enum PROCESS_DPI_AWARENESS
    {
        Process_DPI_Unaware = 0,
        Process_System_DPI_Aware = 1,
        Process_Per_Monitor_DPI_Aware = 2
    }

    public class ProcessHelper
    {
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
    }
}
