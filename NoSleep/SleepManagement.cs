using System;
using System.Runtime.InteropServices;
using System.Timers;

namespace NoSleep
{
    internal static class SleepManagement
    {
        private static readonly Timer timer;
        private const int TMRINTERVALTIME = 60000;

        static SleepManagement()
        {
            timer = new Timer(TMRINTERVALTIME);
            timer.Elapsed += TmrNoSleep_Tick;
        }

        public static bool PreventingSleep => timer.Enabled;

        public static void PreventSleep()
        {
            SetThreadExecutionState(ExecutionState.EsContinuous | ExecutionState.EsSystemRequired | ExecutionState.EsDisplayRequired);

            if (!timer.Enabled)
            {
                timer.Start();
            }
        }

        public static void AllowSleep()
        {
            SetThreadExecutionState(ExecutionState.EsContinuous);

            if (timer.Enabled)
            {
                timer.Stop();
            }
        }

        private static void TmrNoSleep_Tick(object sender, EventArgs e)
        {
            SetThreadExecutionState(ExecutionState.EsContinuous | ExecutionState.EsSystemRequired | ExecutionState.EsDisplayRequired);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);

        [FlagsAttribute]
        private enum ExecutionState : uint
        {
            EsAwaymodeRequired = 0x00000040,
            EsContinuous = 0x80000000,
            EsDisplayRequired = 0x00000002,
            EsSystemRequired = 0x00000001
        }
    }
}