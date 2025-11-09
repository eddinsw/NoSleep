using System;
using System.Windows.Forms;
using Microsoft.Toolkit.Uwp.Notifications;

namespace NoSleep
{
    /// <summary>
    /// Manages user notifications with fallback support.
    /// </summary>
    internal class NotificationService
    {
        private readonly NotifyIcon trayIcon;
        private readonly bool supportsToast;

        public NotificationService(NotifyIcon trayIcon)
        {
            this.trayIcon = trayIcon ?? throw new ArgumentNullException(nameof(trayIcon));
            this.supportsToast = IsWindows10OrGreater();
        }

        /// <summary>
        /// Shows a notification with the specified title and message.
        /// </summary>
        public void ShowNotification(string title, string message, NotificationType type = NotificationType.Info)
        {
            if (!Properties.Settings.Default.NotificationsEnabled)
                return;

            if (supportsToast)
            {
                ShowToast(title, message, type);
            }
            else
            {
                ShowBalloonTip(title, message, type);
            }
        }

        /// <summary>
        /// Shows a notification when sleep state changes.
        /// </summary>
        public void ShowStateChangeNotification(bool isPreventingSleep)
        {
            string title = "NoSleep Status Changed";
            string message = isPreventingSleep
                ? "Sleep prevention is now active"
                : "Sleep prevention is now disabled";

            ShowNotification(title, message, NotificationType.Info);
        }

        private void ShowToast(string title, string message, NotificationType type)
        {
            try
            {
                var builder = new ToastContentBuilder()
                    .AddText(title)
                    .AddText(message);

                // Set scenario based on type
                if (type == NotificationType.Warning)
                {
                    builder.SetToastScenario(ToastScenario.Reminder);
                }

                builder.Show();
            }
            catch
            {
                // Fallback to balloon tip if toast fails
                ShowBalloonTip(title, message, type);
            }
        }

        private void ShowBalloonTip(string title, string message, NotificationType type)
        {
            ToolTipIcon icon = type switch
            {
                NotificationType.Warning => ToolTipIcon.Warning,
                NotificationType.Error => ToolTipIcon.Error,
                _ => ToolTipIcon.Info
            };

            trayIcon.ShowBalloonTip(5000, title, message, icon);
        }

        private bool IsWindows10OrGreater()
        {
            var os = Environment.OSVersion;
            return os.Platform == PlatformID.Win32NT && os.Version.Major >= 10;
        }
    }

    internal enum NotificationType
    {
        Info,
        Warning,
        Error
    }
}
