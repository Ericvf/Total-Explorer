using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BitSquared.Explorer.Tools
{
    public class MouseHook
    {
        private const int WH_MOUSE_LL = 14;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public  enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205,
            WM_BUTTON4UP = 524,
            WM_BUTTON4DOWN = 523,
            WM_BUTTON5UP = 109,
            WM_BUTTON5DOWN = 108
        }

        [StructLayout(LayoutKind.Sequential)]
        public  struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        public static event EventHandler<MouseHookEventArgs> OnMouseHookEvent;
        private static LowLevelMouseProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        public class MouseHookEventArgs : EventArgs
        {
            public MSLLHOOKSTRUCT hookStruct { get; set; }
            public MouseMessages mouseMessages { get; set; }
            public int mouseData { get; set; }
            public bool IsHandled { get; set; }

            public MouseHookEventArgs(MSLLHOOKSTRUCT hookStruct, MouseMessages mouseMessages, int mouseData)
            {
                this.hookStruct = hookStruct;
                this.mouseMessages = mouseMessages;
                this.mouseData = mouseData;
            }
        }

        public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        public static IntPtr RegisterHook()
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _hookID = SetWindowsHookEx(WH_MOUSE_LL, _proc,
                    GetModuleHandle(curModule.ModuleName), 0);

                return _hookID;
            }
        }

        public static void UnregisterHook()
        {
            UnhookWindowsHookEx(_hookID);
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (MouseMessages)wParam > 0)
            {
                MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));


                bool isHandled = RaiseOnMouseHookEvent(hookStruct, (MouseMessages)wParam, (int)hookStruct.mouseData);

                if (isHandled)
                    return (IntPtr)1;
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private static bool RaiseOnMouseHookEvent(MSLLHOOKSTRUCT hookStruct, MouseMessages mouseMessages, int mouseData)
        {
            if (OnMouseHookEvent != null)
            {
                var args = new MouseHookEventArgs(hookStruct, mouseMessages, mouseData);
                OnMouseHookEvent(null, args);
                return args.IsHandled;
            }
            return false;
        }
    }

}
