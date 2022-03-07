using System;
using System.Windows.Forms;

namespace NoSleep
{
    public partial class About : Form
    {
        private MenuItem itemAbout;
        private MenuItem itemClose;
        private MenuItem itemStop;
        private MenuItem itemStart;
        private MenuItem itemStartWithWindows;

        private MenuItem NewMenuItemSeparator => new MenuItem("-");

        private bool clickedClosed = false;

        public About()
        {
            InitializeComponent();

            InitializeMenuItems();

            TrayIcon.DoubleClick += TrayIcon_DoubleClick;
            TrayIcon.Visible = true;

            FormClosing += About_FormClosing;

            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;

            if (RegistryHelper.DoesStartUpKeyExist) ItemStart_Click(this, null);
            else ItemStop_Click(this, null);
        }

        #region FormEvents

        private void About_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!clickedClosed)
            {
                e.Cancel = true;
                WindowState = FormWindowState.Minimized;
                ShowInTaskbar = false;
            }
        }

        private void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            if (SleepManagment.PreventingSleep) ItemStop_Click(this, null);
            else ItemStart_Click(this, null);
        }

        #endregion FormEvents

        #region ContextMenus

        private void InitializeMenuItems()
        {
            itemAbout = new MenuItem("About");
            itemAbout.Click += ItemAbout_Click;
            itemStop = new MenuItem("Stop");
            itemStop.Click += ItemStop_Click;
            itemStart = new MenuItem("Start");
            itemStart.Click += ItemStart_Click;
            itemStartWithWindows = new MenuItem("Startup With Windows");
            itemStartWithWindows.Click += ItemStartWithWindows_Click;
            itemStartWithWindows.Checked = RegistryHelper.DoesStartUpKeyExist;
            itemClose = new MenuItem("Close");
            itemClose.Click += ItemClose_Click;
        }

        private ContextMenu LoadRunningContextMenu()
        {
            ContextMenu menu = new ContextMenu();

            menu.MenuItems.Add(itemAbout);
            menu.MenuItems.Add(itemStartWithWindows);
            menu.MenuItems.Add(NewMenuItemSeparator);
            menu.MenuItems.Add(itemStop);
            menu.MenuItems.Add(NewMenuItemSeparator);
            menu.MenuItems.Add(itemClose);

            return menu;
        }

        private ContextMenu LoadStoppedContextMenu()
        {
            ContextMenu menu = new ContextMenu();

            menu.MenuItems.Add(itemAbout);
            menu.MenuItems.Add(itemStartWithWindows);
            menu.MenuItems.Add(NewMenuItemSeparator);
            menu.MenuItems.Add(itemStart);
            menu.MenuItems.Add(NewMenuItemSeparator);
            menu.MenuItems.Add(itemClose);

            return menu;
        }

        #endregion ContextMenus

        #region ContextMenuEvents

        private void ItemAbout_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;
        }

        private void ItemStart_Click(object sender, EventArgs e)
        {
            TrayIcon.Icon = NoSleep.Properties.Resources.wake;
            TrayIcon.ContextMenu = LoadRunningContextMenu();
            SleepManagment.PreventSleep();
        }

        private void ItemStop_Click(object sender, EventArgs e)
        {
            TrayIcon.Icon = NoSleep.Properties.Resources.sleep;
            TrayIcon.ContextMenu = LoadStoppedContextMenu();
            SleepManagment.AllowSleep();
        }

        private void ItemClose_Click(object sender, EventArgs e)
        {
            clickedClosed = true;
            Close();
        }

        private void ItemStartWithWindows_Click(object sender, EventArgs e)
        {
            if (!RegistryHelper.DoesStartUpKeyExist)
                RegistryHelper.AddStartup();
            else
                RegistryHelper.RemoveStartup();

            itemStartWithWindows.Checked = RegistryHelper.DoesStartUpKeyExist;
        }

        #endregion ContextMenuEvents

        #region ShutdownWithWindows

        private static int WM_QUERYENDSESSION = 0x11;

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            if (m.Msg == WM_QUERYENDSESSION)
            {
                ItemClose_Click(this, null);
            }

            base.WndProc(ref m);
        }

        #endregion ShutdownWithWindows
    }
}