# NoSleep

A lightweight system tray application that prevents your Windows machine from sleeping or locking.

## Features

- **System Tray Integration**: Runs minimized in the system tray with contextual menu controls
- **One-Click Toggle**: Double-click the tray icon to start/stop sleep prevention
- **Global Hotkey**: Press `Ctrl+Shift+F9` to toggle sleep prevention from anywhere (configurable)
- **Toast Notifications**: Modern Windows 10/11 notifications show status changes
- **Auto-Start Option**: Configure the app to start automatically with Windows (requires admin privileges)
- **Auto-Update**: Automatic update checking and installation via Velopack
- **Visual Status**: Different tray icons indicate whether sleep prevention is active or inactive
- **Clean Exit**: Properly handles Windows shutdown events

## What's New in v2.0.0

- ⬆️ Migrated to .NET 8 for better performance and modern C# features
- 🔐 Smart elevation handling - "Startup With Windows" now works for all users with automatic UAC prompting
- ⏱️ Added uptime display in tray icon tooltip (e.g., "Running 2h 15m")
- ⌨️ Added global hotkey support (default: `Ctrl+Shift+F9`) with conflict detection
- 🔔 Added Windows toast notifications for state changes
- 🔄 Added automatic update system with Velopack
- 🏗️ Refactored codebase with improved architecture (ApplicationContext-based)
- 🐛 Fixed memory leaks and improved resource management

## How It Works

The application uses Windows API calls (`SetThreadExecutionState`) to prevent the system from entering sleep mode. A timer refreshes this state every 60 seconds to ensure continuous operation.

## Requirements

- **Operating System**: Windows 10 version 1809 (build 17763) or later, or Windows 11
- **.NET Runtime**: .NET 8 (included in installer)
- **Administrator Privileges**: Automatically requested when needed (e.g., for auto-startup configuration)

## Installation & Usage

1. Build the solution or download the release
2. Run `NoSleep.exe`
3. The application will minimize to your system tray
4. Use the application:
   - **Double-click** the tray icon to toggle sleep prevention
   - **Press `Ctrl+Shift+F9`** to toggle from anywhere (configurable)
   - **Right-click** the tray icon to access controls:
     - **Start/Stop**: Toggle sleep prevention
     - **About**: View application information, version, and admin status
     - **Check for Updates**: Manually check for updates
     - **Startup With Windows**: Configure auto-start (handles elevation automatically if needed)
     - **Close**: Exit the application

## Configuration

### Hotkey Settings
The global hotkey can be configured by editing the user settings:
- Default: `Ctrl+Shift+F9`
- Settings location: `%LOCALAPPDATA%\NoSleep\user.config`

### Notifications
Toast notifications can be disabled in the settings if desired.

## Building

```bash
dotnet build NoSleep.sln
```

For release build:
```bash
dotnet build NoSleep.sln -c Release
```
