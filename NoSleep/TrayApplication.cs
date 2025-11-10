using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Velopack;

namespace NoSleep
{
    /// <summary>
    /// Main application class that coordinates the system tray application without using a Form.
    /// </summary>
    public class TrayApplication : ApplicationContext
    {
        private readonly NotifyIcon trayIcon;
        private readonly TrayMenuBuilder menuBuilder;
        private readonly TrayIconManager trayIconManager;
        private readonly SleepStateController stateController;
        private readonly UpdateService updateService;
        private HotkeyManager hotkeyManager;  // Not readonly - lazy initialized when handle is available
        private readonly NotificationService notificationService;
        private System.Windows.Forms.Timer updateCheckTimer;
        private HiddenWindow hiddenWindow;

        private UpdateInfo pendingUpdate;

        public TrayApplication()
        {
            // Create NotifyIcon first
            trayIcon = new NotifyIcon();

            // Initialize components
            menuBuilder = new TrayMenuBuilder();
            trayIconManager = new TrayIconManager(trayIcon, menuBuilder);
            stateController = new SleepStateController();
            updateService = new UpdateService();
            notificationService = new NotificationService(trayIcon);

            // Create hidden window for hotkey support
            hiddenWindow = new HiddenWindow();
            hiddenWindow.MessageReceived += OnWindowMessageReceived;

            // Wire up events
            WireUpEvents();

            // Set default icon state (stopped)
            trayIconManager.SetStoppedState();

            // Initialize state based on startup settings (this may change the icon)
            InitializeApplicationState();

            // Show tray icon (after state is initialized)
            trayIconManager.Show();

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
                hotkeyManager = new HotkeyManager(hiddenWindow.Handle);
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
                // Show better error message with option to disable
                var result = MessageBox.Show(
                    $"Failed to register global hotkey Ctrl+Shift+F9.\n\n" +
                    $"Reason: {ex.Message}\n\n" +
                    "This usually means another application is using this hotkey.\n\n" +
                    "Would you like to disable the global hotkey feature?",
                    "Hotkey Registration Failed",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (result == DialogResult.Yes)
                {
                    Properties.Settings.Default.HotkeyEnabled = false;
                    Properties.Settings.Default.Save();

                    notificationService.ShowNotification(
                        "Hotkey Disabled",
                        "Global hotkey has been disabled. You can still toggle via tray icon.",
                        NotificationType.Info
                    );
                }
            }
        }

        #endregion

        #region Window Message Handling

        private void OnWindowMessageReceived(ref Message m)
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
                ExitApplication();
            }
        }

        #endregion

        #region Menu Event Handlers

        private void OnAboutClicked(object sender, EventArgs e)
        {
            // Show enhanced About information
            string version = updateService.GetCurrentVersion() ?? "Unknown";
            bool isAdmin = RegistryHelper.IsUserAdministrator();
            string hotkeyStatus = Properties.Settings.Default.HotkeyEnabled ? "Ctrl+Shift+F9" : "Disabled";

            MessageBox.Show(
                $"NoSleep v{version}\n" +
                $"Running as: {(isAdmin ? "Administrator" : "Standard User")}\n\n" +
                "Prevents Windows from sleeping or locking.\n\n" +
                $"Global Hotkey: {hotkeyStatus}\n\n" +
                "Created By: Will Eddins",
                "About NoSleep",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
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
            ExitApplication();
        }

        private void OnStartWithWindowsClicked(object sender, EventArgs e)
        {
            bool currentlyEnabled = RegistryHelper.DoesStartUpKeyExist;
            bool desiredState = !currentlyEnabled;

            // Check if we're running as admin
            if (RegistryHelper.IsUserAdministrator())
            {
                // Admin user - toggle directly
                RegistryHelper.SetStartup(desiredState);
                menuBuilder.UpdateStartupMenuItemState();
            }
            else
            {
                // Non-admin user - need to elevate
                string action = desiredState ? "enable" : "disable";
                var result = MessageBox.Show(
                    $"Administrator privileges are required to {action} startup with Windows.\n\n" +
                    "Would you like to restart as administrator?",
                    "Administrator Required",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    // Restart with elevation and pass the desired action as command-line argument
                    string argument = desiredState ? "--enable-startup" : "--disable-startup";
                    if (RegistryHelper.RestartAsAdministrator(argument))
                    {
                        // Successfully restarted, close this instance
                        ExitApplication();
                    }
                    // If false, user cancelled UAC or error occurred (already handled in RegistryHelper)
                }
            }
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
            trayIcon.BalloonTipClicked -= OnBalloonTipClicked;
            trayIcon.BalloonTipClicked += OnBalloonTipClicked;

            trayIcon.ShowBalloonTip(
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
                string originalText = trayIcon.Text;

                // Show progress in tray icon text
                trayIcon.Text = "Downloading update...";

                await updateService.DownloadUpdateAsync(updateInfo, progress =>
                {
                    trayIcon.Text = $"Downloading... {progress}%";
                });

                trayIcon.Text = "Installing update...";

                // Apply update and restart
                updateService.ApplyUpdateAndRestart(updateInfo);
            }
            catch (Exception ex)
            {
                trayIcon.ShowBalloonTip(
                    5000,
                    "Update Failed",
                    $"Failed to install update: {ex.Message}",
                    ToolTipIcon.Error
                );
            }
        }

        #endregion

        #region Application Lifetime

        private void ExitApplication()
        {
            // Cleanup resources
            hotkeyManager?.Dispose();
            updateCheckTimer?.Stop();
            updateCheckTimer?.Dispose();
            trayIconManager?.Hide();
            trayIcon?.Dispose();
            hiddenWindow?.DestroyHandle();

            // Exit the application
            ExitThread();
        }

        #endregion

        #region Hidden Window for Message Processing

        /// <summary>
        /// Hidden window class for processing Windows messages (needed for hotkey support).
        /// </summary>
        private class HiddenWindow : NativeWindow
        {
            public delegate void MessageReceivedHandler(ref Message m);
            public event MessageReceivedHandler MessageReceived;

            public HiddenWindow()
            {
                CreateHandle(new CreateParams());
            }

            protected override void WndProc(ref Message m)
            {
                // Raise event for message processing
                MessageReceived?.Invoke(ref m);

                base.WndProc(ref m);
            }
        }

        #endregion
    }
}
