using Microsoft.Win32;
using System;
using System.Drawing;
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

        private bool isRunning = false;
        private bool clickedClosed = false;

        private readonly string AppName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
        private readonly string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        private const int MoveMouseDistance = 1;

        public About()
        {
            InitializeComponent();

            InitializeMenuItems();

            tmrNoSleep.Tick += TmrNoSleep_Tick;

            TrayIcon.DoubleClick += TrayIcon_DoubleClick;
            TrayIcon.Visible = true;

            FormClosing += About_FormClosing;

            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;

            if (DoesStartUpKeyExist) ItemStart_Click(this, null);
            else ItemStop_Click(this, null);
        }

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
            if (isRunning) ItemStop_Click(this, null);
            else ItemStart_Click(this, null);
        }

        private void InitializeMenuItems()
        {
            itemAbout = new MenuItem("About");
            itemAbout.Click += ItemAbout_Click;
            itemStop = new MenuItem("Stop");
            itemStop.Click += ItemStop_Click;
            itemStart = new MenuItem("Start");
            itemStart.Click += ItemStart_Click;
            itemStartWithWindows = new MenuItem("Windows Startup");
            itemStartWithWindows.Click += ItemStartWithWindows_Click;
            itemStartWithWindows.Checked = DoesStartUpKeyExist;
            itemClose = new MenuItem("Close");
            itemClose.Click += ItemClose_Click;
        }

        private MenuItem MenuItemSeparator() { return new MenuItem("-"); }

        private ContextMenu LoadRunningContextMenu()
        {
            ContextMenu menu = new ContextMenu();

            menu.MenuItems.Add(itemAbout);
            menu.MenuItems.Add(itemStartWithWindows);
            menu.MenuItems.Add(MenuItemSeparator());
            menu.MenuItems.Add(itemStop);
            menu.MenuItems.Add(MenuItemSeparator());
            menu.MenuItems.Add(itemClose);

            return menu;
        }

        private ContextMenu LoadStoppedContextMenu()
        {
            ContextMenu menu = new ContextMenu();

            menu.MenuItems.Add(itemAbout);
            menu.MenuItems.Add(itemStartWithWindows);
            menu.MenuItems.Add(MenuItemSeparator());
            menu.MenuItems.Add(itemStart);
            menu.MenuItems.Add(MenuItemSeparator());
            menu.MenuItems.Add(itemClose);

            return menu;
        }

        private void ItemAbout_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;
        }

        private void ItemStart_Click(object sender, EventArgs e)
        {
            TrayIcon.Icon = new Icon("icons/noSleepOn.ico");
            TrayIcon.ContextMenu = LoadRunningContextMenu();
            tmrNoSleep.Enabled = true;
            isRunning = true;
        }

        private void ItemStop_Click(object sender, EventArgs e)
        {
            TrayIcon.Icon = new Icon("icons/noSleepOff.ico");
            TrayIcon.ContextMenu = LoadStoppedContextMenu();
            tmrNoSleep.Enabled = false;
            isRunning = false;
        }

        private void ItemClose_Click(object sender, EventArgs e)
        {
            clickedClosed = true;
            Close();
        }

        private void TmrNoSleep_Tick(object sender, EventArgs e)
        {
            MoveCursor();
        }

        private void MoveCursor()
        {
            Cursor = new Cursor(Cursor.Current.Handle);
            Cursor.Position = new Point(Cursor.Position.X - MoveMouseDistance, Cursor.Position.Y);
            Cursor.Position = new Point(Cursor.Position.X + MoveMouseDistance, Cursor.Position.Y);
        }

        private void ItemStartWithWindows_Click(object sender, EventArgs e)
        {
            if (!DoesStartUpKeyExist)
                GetStartUpRun(true).SetValue(AppName, Application.ExecutablePath);
            else
                GetStartUpRun(true).DeleteValue(AppName, false);

            itemStartWithWindows.Checked = DoesStartUpKeyExist;
        }

        private bool DoesStartUpKeyExist => GetStartUpRun().GetValue(AppName, null) != null;

        private RegistryKey GetStartUpRun(bool writeable = false)
        {
            return Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writeable);
        }
    }
}
