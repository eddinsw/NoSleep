using System;
using System.Windows.Forms;

namespace NoSleep
{
    /// <summary>
    /// Manages the system tray icon display and interactions.
    /// </summary>
    internal class TrayIconManager
    {
        private readonly NotifyIcon trayIcon;
        private readonly TrayMenuBuilder menuBuilder;
        private DateTime? preventionStartTime;
        private System.Windows.Forms.Timer tooltipUpdateTimer;

        public event EventHandler DoubleClick;

        public TrayIconManager(NotifyIcon trayIcon, TrayMenuBuilder menuBuilder)
        {
            this.trayIcon = trayIcon ?? throw new ArgumentNullException(nameof(trayIcon));
            this.menuBuilder = menuBuilder ?? throw new ArgumentNullException(nameof(menuBuilder));

            this.trayIcon.DoubleClick += OnTrayIconDoubleClick;

            // Create timer to update tooltip every minute when running
            tooltipUpdateTimer = new System.Windows.Forms.Timer();
            tooltipUpdateTimer.Interval = 60000; // 1 minute
            tooltipUpdateTimer.Tick += (s, e) => UpdateTooltip();
        }

        /// <summary>
        /// Updates the tray icon to reflect running state (preventing sleep).
        /// </summary>
        public void SetRunningState()
        {
            if (!preventionStartTime.HasValue)
                preventionStartTime = DateTime.Now;

            trayIcon.Icon = NoSleep.Properties.Resources.wake;
            trayIcon.ContextMenuStrip = menuBuilder.RunningContextMenu;
            UpdateTooltip();

            // Start updating tooltip every minute
            tooltipUpdateTimer.Start();
        }

        /// <summary>
        /// Updates the tray icon to reflect stopped state (allowing sleep).
        /// </summary>
        public void SetStoppedState()
        {
            preventionStartTime = null;
            tooltipUpdateTimer.Stop();

            trayIcon.Icon = NoSleep.Properties.Resources.sleep;
            trayIcon.Text = "No Sleep - Stopped";
            trayIcon.ContextMenuStrip = menuBuilder.StoppedContextMenu;
        }

        /// <summary>
        /// Shows the tray icon.
        /// </summary>
        public void Show()
        {
            trayIcon.Visible = true;
        }

        /// <summary>
        /// Hides the tray icon.
        /// </summary>
        public void Hide()
        {
            tooltipUpdateTimer?.Stop();
            trayIcon.Visible = false;
        }

        /// <summary>
        /// Updates the tooltip text with current uptime.
        /// </summary>
        private void UpdateTooltip()
        {
            if (preventionStartTime.HasValue)
            {
                var uptime = DateTime.Now - preventionStartTime.Value;

                if (uptime.TotalDays >= 1)
                    trayIcon.Text = $"No Sleep - Running ({uptime.Days}d {uptime.Hours}h)";
                else if (uptime.TotalHours >= 1)
                    trayIcon.Text = $"No Sleep - Running ({uptime.Hours}h {uptime.Minutes}m)";
                else
                    trayIcon.Text = $"No Sleep - Running ({uptime.Minutes}m)";
            }
        }

        private void OnTrayIconDoubleClick(object sender, EventArgs e)
        {
            DoubleClick?.Invoke(sender, e);
        }
    }
}
