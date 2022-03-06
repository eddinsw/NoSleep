using Microsoft.Win32;
using System.Reflection;
using System.Windows.Forms;

namespace NoSleep
{
    internal static class RegistryHelper
    {
        private static readonly string AppName = Assembly.GetExecutingAssembly().GetName().Name;
        private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public static void AddStartup()
        {
            GetStartUpRun(true).SetValue(AppName, Application.ExecutablePath);
        }

        public static void RemoveStartup()
        {
            GetStartUpRun(true).SetValue(AppName, Application.ExecutablePath);
        }

        public static bool DoesStartUpKeyExist => GetStartUpRun().GetValue(AppName, null) != null;

        private static RegistryKey GetStartUpRun(bool writeable = false)
        {
            return Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writeable);
        }
    }
}