using System;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace NoSleep
{
    /// <summary>
    /// Manages application updates using Velopack
    /// </summary>
    internal class UpdateService
    {
        private readonly UpdateManager updateManager;
        private const string GITHUB_REPO_URL = "https://github.com/eddinsw/NoSleep";

        public UpdateService()
        {
            try
            {
                // Use GitHub releases as update source
                var source = new GithubSource(GITHUB_REPO_URL, null, false);
                updateManager = new UpdateManager(source);
            }
            catch
            {
                // If UpdateManager fails to initialize (e.g., not installed via Velopack),
                // updateManager will be null and all operations will safely no-op
                updateManager = null;
            }
        }

        /// <summary>
        /// Gets whether update manager is available (app was installed via Velopack)
        /// </summary>
        public bool IsAvailable => updateManager != null;

        /// <summary>
        /// Checks for updates and returns update info if available
        /// </summary>
        public async Task<UpdateInfo> CheckForUpdatesAsync()
        {
            if (!IsAvailable) return null;

            try
            {
                return await updateManager.CheckForUpdatesAsync();
            }
            catch
            {
                // Silently fail if update check fails (e.g., no internet)
                return null;
            }
        }

        /// <summary>
        /// Downloads the specified update with optional progress callback
        /// </summary>
        public async Task DownloadUpdateAsync(UpdateInfo update, Action<int> progressCallback = null)
        {
            if (!IsAvailable || update == null) return;
            await updateManager.DownloadUpdatesAsync(update, progressCallback);
        }

        /// <summary>
        /// Applies the update and restarts the application
        /// </summary>
        public void ApplyUpdateAndRestart(UpdateInfo update)
        {
            if (!IsAvailable || update == null) return;
            updateManager.ApplyUpdatesAndRestart(update);
        }

        /// <summary>
        /// Applies the update and exits (user must manually restart)
        /// </summary>
        public void ApplyUpdateAndExit(UpdateInfo update)
        {
            if (!IsAvailable || update == null) return;
            updateManager.ApplyUpdatesAndExit(update);
        }

        /// <summary>
        /// Gets the currently installed version
        /// </summary>
        public string GetCurrentVersion()
        {
            if (!IsAvailable) return "Unknown";
            return updateManager.CurrentVersion?.ToString() ?? "Unknown";
        }
    }
}
