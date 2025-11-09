using System;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Toolkit.Uwp.Notifications;
using Velopack;

namespace NoSleep
{
    internal static class Program
    {
        private const string MutexName = "NoSleep_SingleInstance_Mutex";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            // Note: Toast notification activation handling can be added here if needed
            // For basic notifications, ToastContentBuilder.Show() is sufficient

            // Velopack: Bootstrap hooks for install/update/uninstall
            VelopackApp.Build()
                .OnBeforeUninstallFastCallback((v) =>
                {
                    // Clean up registry entry when uninstalling
                    try
                    {
                        RegistryHelper.SetStartup(false);
                    }
                    catch { /* Ignore errors during uninstall */ }
                })
                .Run();

            using (Mutex mutex = new Mutex(true, MutexName, out bool createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("NoSleep is already running.", "Already Running", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());

                // Keep mutex alive until application exits
                GC.KeepAlive(mutex);
            }
        }
    }
}