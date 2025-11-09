using System;
using System.Windows.Forms;

namespace NoSleep
{
    /// <summary>
    /// Manages the creation and state of system tray context menus.
    /// </summary>
    internal class TrayMenuBuilder
    {
        private readonly ToolStripMenuItem itemAbout;
        private readonly ToolStripMenuItem itemClose;
        private readonly ToolStripMenuItem itemStop;
        private readonly ToolStripMenuItem itemStart;
        private readonly ToolStripMenuItem itemStartWithWindows;
        private readonly ToolStripMenuItem itemCheckForUpdates;

        private readonly ContextMenuStrip runningContextMenu;
        private readonly ContextMenuStrip stoppedContextMenu;

        public event EventHandler AboutClicked;
        public event EventHandler StartClicked;
        public event EventHandler StopClicked;
        public event EventHandler CloseClicked;
        public event EventHandler StartWithWindowsClicked;
        public event EventHandler CheckForUpdatesClicked;

        public TrayMenuBuilder()
        {
            // Initialize menu items
            itemAbout = new ToolStripMenuItem("About");
            itemAbout.Click += (s, e) => AboutClicked?.Invoke(s, e);

            itemStop = new ToolStripMenuItem("Stop");
            itemStop.Click += (s, e) => StopClicked?.Invoke(s, e);

            itemStart = new ToolStripMenuItem("Start");
            itemStart.Click += (s, e) => StartClicked?.Invoke(s, e);

            itemCheckForUpdates = new ToolStripMenuItem("Check for Updates");
            itemCheckForUpdates.Click += (s, e) => CheckForUpdatesClicked?.Invoke(s, e);

            itemClose = new ToolStripMenuItem("Close");
            itemClose.Click += (s, e) => CloseClicked?.Invoke(s, e);

            // Only show startup option for administrators
            if (RegistryHelper.IsUserAdministrator())
            {
                itemStartWithWindows = new ToolStripMenuItem("Startup With Windows");
                itemStartWithWindows.Click += (s, e) => StartWithWindowsClicked?.Invoke(s, e);
                itemStartWithWindows.Checked = RegistryHelper.DoesStartUpKeyExist;
            }

            // Build the context menus
            runningContextMenu = BuildRunningMenu();
            stoppedContextMenu = BuildStoppedMenu();
        }

        /// <summary>
        /// Gets the context menu for when sleep prevention is running.
        /// </summary>
        public ContextMenuStrip RunningContextMenu => runningContextMenu;

        /// <summary>
        /// Gets the context menu for when sleep prevention is stopped.
        /// </summary>
        public ContextMenuStrip StoppedContextMenu => stoppedContextMenu;

        /// <summary>
        /// Updates the "Startup With Windows" menu item checked state.
        /// </summary>
        public void UpdateStartupMenuItemState()
        {
            if (itemStartWithWindows != null)
            {
                itemStartWithWindows.Checked = RegistryHelper.DoesStartUpKeyExist;
            }
        }

        private ContextMenuStrip BuildRunningMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add(itemAbout);
            menu.Items.Add(itemCheckForUpdates);
            if (itemStartWithWindows != null)
            {
                menu.Items.Add(itemStartWithWindows);
            }
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(itemStop);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(itemClose);
            return menu;
        }

        private ContextMenuStrip BuildStoppedMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add(itemAbout);
            menu.Items.Add(itemCheckForUpdates);
            if (itemStartWithWindows != null)
            {
                menu.Items.Add(itemStartWithWindows);
            }
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(itemStart);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(itemClose);
            return menu;
        }
    }
}
