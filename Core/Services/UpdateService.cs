using System;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace DynamicIsland.Core.Services;

/// <summary>
/// Phase 13 — Auto Updater, via Velopack (the plan's chosen framework).
///
/// Setup still required outside this file:
///   1. Run `vpk pack` in CI/release pipeline to produce the Velopack
///      release + installer for this app.
///   2. Replace the placeholder feed URL below with your real release feed
///      (a GitHub Releases repo, S3 bucket, or static file server all work
///      with Velopack's built-in sources).
///   3. Call VelopackApp.Build().Run() as the FIRST line of Main/entry point
///      (before InitializeComponent/App construction) — Velopack needs to
///      intercept squasher/install-time arguments before WinUI starts.
/// </summary>
public sealed class UpdateService : IUpdateService
{
    // TODO: point this at your real release feed before shipping.
    private const string UpdateFeedUrl = "https://example.com/dynamicisland/releases";

    public async Task CheckAndApplyUpdatesOnStartupAsync()
    {
        try
        {
            var updateManager = new UpdateManager(new SimpleWebSource(UpdateFeedUrl));

            if (!updateManager.IsInstalled)
            {
                // Running from source/VS debugger, not an installed Velopack app.
                return;
            }

            var newVersion = await updateManager.CheckForUpdatesAsync();
            if (newVersion is null) return; // Already up to date.

            await updateManager.DownloadUpdatesAsync(newVersion);

            // Restarts the app on the new version. Call this at a safe point
            // (e.g. right after startup, or from a "Restart to update" prompt
            // in the Settings window) rather than mid-session.
            updateManager.ApplyUpdatesAndRestart(newVersion);
        }
        catch
        {
            // Network hiccups or no feed configured yet shouldn't block startup.
        }
    }
}
