# Velopack Auto-Update Setup Guide

## Overview
NoSleep now includes Velopack auto-update functionality. This allows users to automatically receive and install updates with minimal friction.

## What Was Added

### 1. NuGet Package
- **Velopack 0.0.1298** added to NoSleep.csproj

### 2. Code Changes

#### Program.cs
- Added `using Velopack;`
- Added `VelopackApp.Build().Run()` bootstrap call
- Added `string[] args` parameter to Main()

#### UpdateService.cs (NEW)
- Manages update checking and installation
- Connects to GitHub Releases for updates
- Handles errors gracefully (no-op if not installed via Velopack)

#### MainForm.cs
- Added automatic update checking (24-hour interval)
- Added balloon notification when updates available
- Added progress reporting during download
- Integrated with menu system

#### TrayMenuBuilder.cs
- Added "Check for Updates" menu item
- Available in both running and stopped menus

### 3. GitHub Actions Workflow
- **File**: `.github/workflows/release.yml`
- Triggers on version tags (e.g., `v1.0.3`)
- Builds, packages, and publishes releases automatically

### 4. Project Configuration
- Updated to version **1.0.3**
- Cleaned up old ClickOnce deployment settings
- Single source of truth for versioning

## How to Create a Release

### Step 1: Update Version
Version is now in `.csproj` file:
```xml
<Version>1.0.3</Version>
```

### Step 2: Commit Changes
```bash
git add .
git commit -m "Release v1.0.3 with Velopack auto-updates"
```

### Step 3: Create and Push Tag
```bash
git tag v1.0.3
git push origin v1.0.3
```

### Step 4: GitHub Actions Runs Automatically
- Workflow builds the application
- Creates Velopack packages
- Generates delta updates (if applicable)
- Publishes to GitHub Releases

### Step 5: Users Get Updates
- Installed users see balloon notification within 24 hours
- Click notification to download and install
- App restarts automatically with new version

## Testing Locally (Optional)

### Install vpk CLI Tool
```bash
dotnet tool install -g vpk
```

### Build and Package Locally
```bash
# Publish the application
dotnet publish NoSleep\NoSleep.csproj -c Release --self-contained -r win-x64 -o .\publish

# Package with Velopack
vpk pack --packId NoSleep --packVersion 1.0.3 --packDir .\publish --mainExe NoSleep.exe --icon NoSleep\Resources\wake.ico

# Installer created at: .\Releases\NoSleep-1.0.3-win-Setup.exe
```

## User Experience

### First-Time Installation
1. Download `NoSleep-1.0.3-win-Setup.exe` from GitHub Releases
2. Run installer (one-click, no UAC)
3. Application installs and launches

### Automatic Updates
1. NoSleep checks for updates every 24 hours in background
2. Balloon notification appears: "Update Available - NoSleep 1.0.4 is available. Click to install."
3. User clicks notification
4. Download progress shown: "Downloading... 45%"
5. "Installing update..." appears
6. App restarts automatically in ~2 seconds
7. Update complete!

### Manual Update Check
1. Right-click tray icon
2. Select "Check for Updates"
3. Same update flow as automatic

## Features

### Delta Updates
- Users only download changes between versions
- Typical update: ~200KB instead of 5MB+
- Requires at least 2 releases to generate deltas

### Fast Installation
- Updates apply in ~2 seconds
- No UAC prompts
- No Windows Installer overhead

### Graceful Degradation
- If app not installed via Velopack, update features are disabled
- No errors or crashes
- Works seamlessly in development

### Error Handling
- Network failures are silently ignored
- Download errors show user notification
- Doesn't interrupt workflow

## Important Notes

### First Velopack Release (v1.0.3)
- Users must manually download and install
- This is the baseline for future auto-updates
- No delta updates for first release

### Subsequent Releases (v1.0.4+)
- Automatic for users on v1.0.3+
- Delta updates generated automatically
- Much smaller download sizes

### Version Numbering
- Use semantic versioning: MAJOR.MINOR.PATCH
- Tags must match: `v1.0.3`
- No pre-release tags initially (e.g., avoid `v1.0.3-beta`)

## Troubleshooting

### "Update check failed"
- Normal if no internet connection
- Will retry on next check (24 hours)
- No user action needed

### "Already running" message after update
- Very rare, caused by timing issue
- Just close and restart
- Mutex ensures only one instance

### GitHub Actions workflow fails
- Check workflow logs in GitHub Actions tab
- Ensure tag format is correct: `v1.0.3`
- Verify repository has write permissions

## Future Enhancements

Possible additions:
- Settings for update frequency
- "Download only" option (install on next restart)
- Update notifications in About dialog
- Release notes display
- Beta/stable channel support

## Resources

- **Velopack Docs**: https://docs.velopack.io/
- **GitHub Releases**: https://github.com/eddinsw/NoSleep/releases
- **Workflow File**: `.github/workflows/release.yml`

## Summary

NoSleep now has professional auto-update capabilities with:
- ✅ Automatic background checking (24 hours)
- ✅ User-friendly balloon notifications
- ✅ One-click install and restart
- ✅ Delta updates for bandwidth efficiency
- ✅ Fully automated GitHub workflow
- ✅ Zero cost for open source
- ✅ No user friction

**Ready to release v1.0.3 with Velopack!**
