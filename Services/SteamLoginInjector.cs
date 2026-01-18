using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Execor.Services
{
    public static class SteamLoginInjector
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        public static void InjectLogin(string username, string password)
        {
            Debug.WriteLine("[Injector] InjectLogin started");
            

            // 1. wait until SDL_app “Sign in to Steam” is *really* there
            IntPtr hWnd;
            do
            {
                hWnd = FindWindow("SDL_app", "Sign in to Steam");
                if (hWnd == IntPtr.Zero) Thread.Sleep(500);
            } while (hWnd == IntPtr.Zero);

            // 2. force-foreground (SAM trick)
            uint targetThread = GetWindowThreadProcessId(hWnd, out _);
            uint ourThread = GetCurrentThreadId();
            AttachThreadInput(ourThread, targetThread, true);
            SetForegroundWindow(hWnd);
            AttachThreadInput(ourThread, targetThread, false);

            // 3. wait until window accepts keystrokes
            bool ready = false;
            for (int i = 0; i < 30; i++)
            {
                try
                {
                    SendKeys.SendWait(" ");           // harmless probe
                    SendKeys.SendWait("{BACKSPACE}"); // if it arrives, we’re good
                    ready = true;
                    break;
                }
                catch { Thread.Sleep(500); }
            }
            if (!ready) throw new Exception("Window never became ready for input.");

            // 4. focus first field (tab spam) then type
            for (int j = 0; j < 5; j++) SendKeys.SendWait("{TAB}");

            SendKeys.SendWait("^{HOME}");
            SendKeys.SendWait("^+{END}");
            SendKeys.SendWait("{DEL}");

            SendKeys.SendWait(Escape(username));
            SendKeys.SendWait("{TAB}");
            SendKeys.SendWait(Escape(password));
            SendKeys.SendWait("{ENTER}");

            Debug.WriteLine("[Injector] Send-keys complete");

            static string Escape(string s) =>
                s.Replace("{", "{{").Replace("}", "}}")
                 .Replace("+", "{+}").Replace("^", "{^}")
                 .Replace("%", "{%}").Replace("~", "{~}")
                 .Replace("(", "{(}").Replace(")", "{)}");
        }
    }
}