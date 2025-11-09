using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Velopack;

namespace NoSleep
{
    /// <summary>
    /// Main application form that displays the About dialog and coordinates the system tray application.
    /// </summary>
    public partial class MainForm : Form
    {
        private readonly TrayMenuBuilder menuBuilder;
        private readonly TrayIconManager trayIconManager;
        private readonly SleepStateController stateController;
        private readonly UpdateService updateService;
        private HotkeyManager hotkeyManager;  // Not readonly - lazy initialized when handle is available
        private readonly NotificationService notificationService;
        private System.Windows.Forms.Timer updateCheckTimer;

        private bool clickedClosed = false;
        private UpdateInfo pendingUpdate;

        public MainForm()
        {
            InitializeComponent();

            // Initialize components
            menuBuilder = new TrayMenuBuilder();
            trayIconManager = new TrayIconManager(TrayIcon, menuBuilder);
            stateController = new SleepStateController();
            updateService = new UpdateService();
            // hotkeyManager will be initialized in InitializeHotkey() when handle is available
            notificationService = new NotificationService(TrayIcon);

            // Wire up events
            WireUpEvents();

            // Set initial form state
            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;
            FormClosing += MainForm_FormClosing;

            // Show tray icon
            trayIconManager.Show();

            // Initialize state based on startup settings
            InitializeApplicationState();

            // Start periodic update checks
            InitializeUpdateChecking();

            // Initialize hotkey
            InitializeHotkey();
        }

        #region Initialization

        private void WireUpEvents()
        {
            // Menu events
            menuBuilder.AboutClicked += OnAboutClicked;
            menuBuilder.StartClicked += OnStartClicked;
            menuBuilder.StopClicked += OnStopClicked;
            menuBuilder.CloseClicked += OnCloseClicked;
            menuBuilder.StartWithWindowsClicked += OnStartWithWindowsClicked;
            menuBuilder.CheckForUpdatesClicked += OnCheckForUpdatesClicked;

            // Tray icon events
            trayIconManager.DoubleClick += OnTrayIconDoubleClick;

            // State controller events
            stateController.StateChanged += OnSleepStateChanged;

            // Hotkey events will be wired up in InitializeHotkey()
        }

        private void InitializeApplicationState()
        {
            // If the app is set to start with Windows, automatically start sleep prevention
            if (RegistryHelper.DoesStartUpKeyExist)
            {
                stateController.Start();
            }
            else
            {
                stateController.Stop();
            }
        }

        private void InitializeHotkey()
        {
            if (!Properties.Settings.Default.HotkeyEnabled)
                return;

            // Initialize HotkeyManager now that the window handle is available
            if (hotkeyManager == null)
            {
                hotkeyManager = new HotkeyManager(this.Handle);
                hotkeyManager.HotkeyPressed += OnHotkeyPressed;
            }

            uint modifiers = Properties.Settings.Default.HotkeyModifiers;
            uint key = Properties.Settings.Default.HotkeyKey;

            if (key == 0)
            {
                // Set default hotkey: Ctrl+Shift+F9
                modifiers = HotkeyManager.MOD_CONTROL | HotkeyManager.MOD_SHIFT;
                key = (uint)Keys.F9;

                Properties.Settings.Default.HotkeyModifiers = modifiers;
                Properties.Settings.Default.HotkeyKey = key;
                Properties.Settings.Default.Save();
            }

            try
            {
                hotkeyManager.Register(modifiers, key);
            }
            catch (Exception ex)
            {
                // Show notification if hotkey registration fails
                notificationService.ShowNotification(
                    "Hotkey Registration Failed",
                    ex.Message,
                    NotificationType.Warning
                );
            }
        }

        #endregion

        #region Form Events

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!clickedClosed)
            {
                // Minimize instead of closing
                e.Cancel = true;
                WindowState = FormWindowState.Minimized;
                ShowInTaskbar = false;
            }
            else
            {
                // Cleanup hotkey on actual close
                hotkeyManager?.Dispose();
            }
        }

        protected override void WndProc(ref Message m)
        {
            // Handle hotkey messages
            if (hotkeyManager?.ProcessMessage(ref m) == true)
            {
                return;
            }

            const int WM_QUERYENDSESSION = 0x11;

            if (m.Msg == WM_QUERYENDSESSION)
            {
                // Windows is shutting down, close the application properly
                clickedClosed = true;
                Close();
            }

            base.WndProc(ref m);
        }

        #endregion

        #region Menu Event Handlers

        private void OnAboutClicked(object sender, EventArgs e)
        {
            // Show the About form
            WindowState = FormWindowState.Normal;
            ShowInTaskbar = true;
        }

        private void OnStartClicked(object sender, EventArgs e)
        {
            stateController.Start();
        }

        private void OnStopClicked(object sender, EventArgs e)
        {
            stateController.Stop();
        }

        private void OnCloseClicked(object sender, EventArgs e)
        {
            clickedClosed = true;
            Close();
        }

        private void OnStartWithWindowsClicked(object sender, EventArgs e)
        {
            RegistryHelper.SetStartup(!RegistryHelper.DoesStartUpKeyExist);
            menuBuilder.UpdateStartupMenuItemState();
        }

        private async void OnCheckForUpdatesClicked(object sender, EventArgs e)
        {
            await CheckForUpdatesAsync();
        }

        #endregion

        #region Tray Icon Event Handlers

        private void OnTrayIconDoubleClick(object sender, EventArgs e)
        {
            stateController.Toggle();
        }

        #endregion

        #region Hotkey Event Handlers

        private void OnHotkeyPressed(object sender, EventArgs e)
        {
            // Toggle sleep state when hotkey is pressed
            stateController.Toggle();
        }

        #endregion

        #region State Controller Event Handlers

        private void OnSleepStateChanged(object sender, SleepStateChangedEventArgs e)
        {
            if (e.IsPreventingSleep)
            {
                trayIconManager.SetRunningState();
            }
            else
            {
                trayIconManager.SetStoppedState();
            }

            // Show notification on state change
            notificationService.ShowStateChangeNotification(e.IsPreventingSleep);
        }

        #endregion

        #region Update Management

        private void InitializeUpdateChecking()
        {
            if (!updateService.IsAvailable)
                return;

            // Check for updates on startup (after a short delay to not slow down app start)
            Task.Delay(5000).ContinueWith(async _ =>
            {
                await CheckForUpdatesAsync();
            });

            // Check for updates every 24 hours
            updateCheckTimer = new System.Windows.Forms.Timer();
            updateCheckTimer.Interval = 24 * 60 * 60 * 1000; // 24 hours
            updateCheckTimer.Tick += async (s, e) => await CheckForUpdatesAsync();
            updateCheckTimer.Start();
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                var updateInfo = await updateService.CheckForUpdatesAsync();
                if (updateInfo != null)
                {
                    // Store pending update for balloon click handler
                    pendingUpdate = updateInfo;
                    NotifyUpdateAvailable(updateInfo);
                }
            }
            catch
            {
                // Silently fail - don't disturb user with update errors
            }
        }

        private void NotifyUpdateAvailable(UpdateInfo updateInfo)
        {
            // Unsubscribe any previous handlers to avoid duplicates
            TrayIcon.BalloonTipClicked -= OnBalloonTipClicked;
            TrayIcon.BalloonTipClicked += OnBalloonTipClicked;

            TrayIcon.ShowBalloonTip(
                5000,
                "Update Available",
                $"NoSleep {updateInfo.TargetFullRelease.Version} is available. Click to install.",
                ToolTipIcon.Info
            );
        }

        private async void OnBalloonTipClicked(object sender, EventArgs e)
        {
            if (pendingUpdate != null)
            {
                await DownloadAndApplyUpdateAsync(pendingUpdate);
            }
        }

        private async Task DownloadAndApplyUpdateAsync(UpdateInfo updateInfo)
        {
            try
            {
                // Save original tray text
                string originalText = TrayIcon.Text;

                // Show progress in tray icon text
                TrayIcon.Text = "Downloading update...";

                await updateService.DownloadUpdateAsync(updateInfo, progress =>
                {
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() => TrayIcon.Text = $"Downloading... {progress}%"));
                    }
                    else
                    {
                        TrayIcon.Text = $"Downloading... {progress}%";
                    }
                });

                TrayIcon.Text = "Installing update...";

                // Apply update and restart
                updateService.ApplyUpdateAndRestart(updateInfo);
            }
            catch (Exception ex)
            {
                TrayIcon.ShowBalloonTip(
                    5000,
                    "Update Failed",
                    $"Failed to install update: {ex.Message}",
                    ToolTipIcon.Error
                );
            }
        }

        #endregion
    }
}
