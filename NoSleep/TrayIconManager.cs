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

        public event EventHandler DoubleClick;

        public TrayIconManager(NotifyIcon trayIcon, TrayMenuBuilder menuBuilder)
        {
            this.trayIcon = trayIcon ?? throw new ArgumentNullException(nameof(trayIcon));
            this.menuBuilder = menuBuilder ?? throw new ArgumentNullException(nameof(menuBuilder));

            this.trayIcon.DoubleClick += OnTrayIconDoubleClick;
        }

        /// <summary>
        /// Updates the tray icon to reflect running state (preventing sleep).
        /// </summary>
        public void SetRunningState()
        {
            trayIcon.Icon = NoSleep.Properties.Resources.wake;
            trayIcon.Text = "No Sleep - Running";
            trayIcon.ContextMenuStrip = menuBuilder.RunningContextMenu;
        }

        /// <summary>
        /// Updates the tray icon to reflect stopped state (allowing sleep).
        /// </summary>
        public void SetStoppedState()
        {
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
            trayIcon.Visible = false;
        }

        private void OnTrayIconDoubleClick(object sender, EventArgs e)
        {
            DoubleClick?.Invoke(sender, e);
        }
    }
}
