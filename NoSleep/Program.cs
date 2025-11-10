using System;
using System.Threading;
using System.Windows.Forms;
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

            // Handle startup registry modification arguments (when running as admin)
            if (args.Length > 0)
            {
                if (args[0] == "--enable-startup" || args[0] == "--disable-startup")
                {
                    bool enable = args[0] == "--enable-startup";

                    try
                    {
                        RegistryHelper.SetStartup(enable);

                        // Restart as normal user after registry modification
                        RegistryHelper.RestartAsNormalUser();
                        return; // Exit this elevated instance
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Failed to modify startup settings: {ex.Message}",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                        return;
                    }
                }
            }

            using (Mutex mutex = new Mutex(true, MutexName, out bool createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("NoSleep is already running.", "Already Running", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Run the tray application
                Application.Run(new TrayApplication());

                // Keep mutex alive until application exits
                GC.KeepAlive(mutex);
            }
        }
    }
}