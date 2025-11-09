using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NoSleep
{
    /// <summary>
    /// Manages global hotkey registration and handling.
    /// </summary>
    internal class HotkeyManager : IDisposable
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // Modifier constants
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;
        public const uint MOD_NOREPEAT = 0x4000;

        // Windows message
        public const int WM_HOTKEY = 0x0312;

        private const int HOTKEY_ID = 1;
        private readonly IntPtr windowHandle;
        private bool isRegistered;

        public event EventHandler HotkeyPressed;

        public HotkeyManager(IntPtr windowHandle)
        {
            this.windowHandle = windowHandle;
        }

        /// <summary>
        /// Registers the global hotkey with the specified modifiers and key.
        /// </summary>
        public bool Register(uint modifiers, uint key)
        {
            // Unregister existing hotkey if any
            Unregister();

            // Always add MOD_NOREPEAT to prevent auto-repeat
            modifiers |= MOD_NOREPEAT;

            isRegistered = RegisterHotKey(windowHandle, HOTKEY_ID, modifiers, key);

            if (!isRegistered)
            {
                int error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException(
                    error == 1409
                        ? "Hotkey is already in use by another application."
                        : $"Failed to register hotkey. Error: {error}"
                );
            }

            return true;
        }

        /// <summary>
        /// Unregisters the global hotkey.
        /// </summary>
        public void Unregister()
        {
            if (isRegistered)
            {
                UnregisterHotKey(windowHandle, HOTKEY_ID);
                isRegistered = false;
            }
        }

        /// <summary>
        /// Processes window messages to detect hotkey presses.
        /// Call this from your form's WndProc override.
        /// </summary>
        public bool ProcessMessage(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                OnHotkeyPressed();
                return true;
            }
            return false;
        }

        protected virtual void OnHotkeyPressed()
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            Unregister();
        }

        /// <summary>
        /// Gets a user-friendly string representation of the hotkey.
        /// </summary>
        public static string GetHotkeyDisplayString(uint modifiers, uint key)
        {
            var parts = new System.Collections.Generic.List<string>();

            // Remove MOD_NOREPEAT from display
            modifiers &= ~MOD_NOREPEAT;

            if ((modifiers & MOD_CONTROL) != 0) parts.Add("Ctrl");
            if ((modifiers & MOD_ALT) != 0) parts.Add("Alt");
            if ((modifiers & MOD_SHIFT) != 0) parts.Add("Shift");
            if ((modifiers & MOD_WIN) != 0) parts.Add("Win");

            // Convert key code to string
            Keys keyEnum = (Keys)key;
            parts.Add(keyEnum.ToString());

            return string.Join("+", parts);
        }
    }
}
