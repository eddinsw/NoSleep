using Microsoft.Win32;
using System;
using System.Reflection;
using System.Security.Principal;
using System.Windows.Forms;

namespace NoSleep
{
    internal static class RegistryHelper
    {
        private static readonly string AppName = Assembly.GetExecutingAssembly().GetName().Name;
        private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";


        /// <summary>
        /// Checks if the application is set to start with Windows.
        /// </summary>
        public static bool DoesStartUpKeyExist
        {
            get
            {
                using (var key = GetStartUpRun())
                {
                    return key?.GetValue(AppName) != null;
                }
            }
        }

        public static void SetStartup(bool enable)
        {
            try
            {
                using (var key = GetStartUpRun(true))
                {
                    if (enable)
                        key?.SetValue(AppName, Application.ExecutablePath);
                    else
                        key?.DeleteValue(AppName, false);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Error setting startup: {ex.Message}");
            }
        }

        /// <summary>
        /// Opens the registry key for startup programs.
        /// </summary>
        /// <param name="writeable">True to open the key in write mode, false for read mode.</param>
        /// <returns>The registry key object.</returns>
        private static RegistryKey GetStartUpRun(bool writeable = false)
        {
            return Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writeable);
        }
        /// <summary>
        /// Checks if the current user has administrative privileges.
        /// </summary>
        /// <returns>True if the user is an administrator, otherwise false.</returns>
        public static bool IsUserAdministrator()
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to check user privileges: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

    }
}