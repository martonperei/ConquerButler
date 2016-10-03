using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;

namespace ConquerButler.Gui
{
    class HwndHostEx : HwndHost
    {
        public const int GWL_STYLE = (-16);
        public const int WS_CHILD = 0x40000000;

        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        private IntPtr ChildHandle = IntPtr.Zero;

        public HwndHostEx(IntPtr handle)
        {
            this.ChildHandle = handle;
        }

        protected override System.Runtime.InteropServices.HandleRef BuildWindowCore(System.Runtime.InteropServices.HandleRef hwndParent)
        {
            HandleRef href = new HandleRef();

            if (ChildHandle != IntPtr.Zero)
            {
                SetWindowLong(this.ChildHandle, GWL_STYLE, WS_CHILD);

                SetParent(this.ChildHandle, hwndParent.Handle);
                href = new HandleRef(this, this.ChildHandle);
            }

            return href;
        }

        protected override void DestroyWindowCore(System.Runtime.InteropServices.HandleRef hwnd)
        {

        }
    }
}
