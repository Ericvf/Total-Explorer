using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;


namespace Fex.TotalExplorer.Tools
{
    public class KeyboardHook
    {
        // Structure contain information about low-level keyboard input event
        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public Keys key;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr extra;
        }

        //System level functions to be used for hook and unhook keyboard input
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int id, LowLevelKeyboardProc callback, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hook, int nCode, IntPtr wp, IntPtr lp);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string name);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern short GetAsyncKeyState(Keys key);


        //Declaring Global objects
        private static IntPtr ptrHook;
        private static LowLevelKeyboardProc objKeyboardProcess;


        public static event EventHandler<KeyHookEventArgs> OnKeyHookEvent;
        public static Dictionary<Keys, bool> KeyboardState = new Dictionary<Keys, bool>();

        public class KeyHookEventArgs : EventArgs
        {
            public bool IsHandled { get; set; }
            public Keys Key { get; set; }
            public bool IsDown { get; set; }

            public KeyHookEventArgs(Keys key, bool isDown)
            {
                this.Key = key;
                this.IsDown = isDown;
                this.IsHandled = false;
            }
        }

        private static bool RaiseOnKeyHookEvent(Keys key, int wp)
        {
            if (OnKeyHookEvent != null)
            {
                bool isDown = ((int)wp == 256) || ((int)wp == 260);
                var args = new KeyHookEventArgs(key, isDown);
                OnKeyHookEvent(null, args);
                return args.IsHandled;
            }
            return false;
        }

        public static void RegisterHook()
        {
            ProcessModule objCurrentModule = Process.GetCurrentProcess().MainModule;
            objKeyboardProcess = new LowLevelKeyboardProc(captureKey);
            ptrHook = SetWindowsHookEx(13, objKeyboardProcess, GetModuleHandle(objCurrentModule.ModuleName), 0);
        }

        public static void UnregisterHook()
        {
            UnhookWindowsHookEx(ptrHook);
        }

        private static IntPtr captureKey(int nCode, IntPtr wp, IntPtr lp)
        {
            Debug.WriteLine("MESSAGE" + nCode + "  wp " + wp + " kp " + lp);
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT objKeyInfo = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lp, typeof(KBDLLHOOKSTRUCT));
                bool isHandled = RaiseOnKeyHookEvent((Keys)objKeyInfo.key, (int)wp);
                registerKeyState(objKeyInfo.key, ((int)wp == 256) || ((int)wp == 260));
                if (isHandled)
                    return (IntPtr)1;
            }
            return CallNextHookEx(ptrHook, nCode, wp, lp);
        }

        private static void registerKeyState(Keys keys, bool isDown)
        {
            KeyboardState[keys] = isDown;
        }


        public static bool IsDown(Keys keys)
        {
            if (KeyboardState.ContainsKey(keys))
                return KeyboardState[keys];

            return false;
        }
    }
}
