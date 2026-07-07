using System;
using DynamicIsland.Core.Plugins;
using DynamicIsland.Core.Services;
using DynamicIsland.Core.Storage;
using DynamicIsland.UI.Island;
using DynamicIsland.UI.Settings;
using Microsoft.Extensions.DependencyInjection;
using Velopack;

namespace DynamicIsland;

internal static class Program
{
    public static IServiceProvider Services { get; private set; } = null!;

    [STAThread]
    private static void Main()
    {
        // Must run before anything else — handles Velopack's install/update/
        // uninstall lifecycle hooks (Phase 13 - Auto Updater).
        VelopackApp.Build().Run();

        System.Windows.Forms.Application.SetHighDpiMode(System.Windows.Forms.HighDpiMode.PerMonitorV2);
        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

        Services = ConfigureServices();

        try
        {
            // Startup sequence
            var settingsService = Services.GetRequiredService<ISettingsService>();
            settingsService.LoadAsync().GetAwaiter().GetResult();

            var updateService = Services.GetRequiredService<IUpdateService>();
            updateService.CheckAndApplyUpdatesOnStartupAsync().GetAwaiter().GetResult();

            // Phase 9 - Activity System: sweeps expired transient activities.
            using var activitySweeper = new ActivitySweeper(Services.GetRequiredService<IActivityManager>());

            // Phase 12 - Notifications: no-ops gracefully pre-MSIX-packaging (see class remarks).
            var notificationListener = Services.GetRequiredService<NotificationListenerService>();
            _ = notificationListener.TryInitializeAsync();

            // Phase 15 - Plugin System: load anything dropped into the Plugins folder.
            var pluginHost = Services.GetRequiredService<PluginHost>();
            pluginHost.LoadAllFromDefaultDirectory();

            var islandForm = Services.GetRequiredService<IslandForm>();
            System.Windows.Forms.Application.Run(islandForm);
        }
        finally
        {
            // Cleanly tears down singleton services holding OS resources —
            // BluetoothService's DeviceWatcher, ClipboardManager's listener
            // window — instead of leaving that to process termination.
            if (Services is IDisposable disposableProvider)
            {
                disposableProvider.Dispose();
            }
        }
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Storage
        services.AddSingleton<AppDatabase>();
        services.AddSingleton<ISettingsService, SettingsService>();

        // Core / Activity System (Phase 9)
        services.AddSingleton<IActivityManager, ActivityManager>();

        // Phase 3 - Media
        services.AddSingleton<IMediaController, MediaController>();

        // Phase 4 - Calendar
        services.AddSingleton<ICalendarService, CalendarService>();

        // Phase 5 - Favorites
        services.AddSingleton<IFavoritesService, FavoritesService>();

        // Phase 6 - File Tray
        services.AddSingleton<IFileTrayService, FileTrayService>();

        // Phase 7 - Clipboard
        services.AddSingleton<IClipboardManager, ClipboardManager>();

        // Phase 8 - Bluetooth
        services.AddSingleton<IBluetoothService, BluetoothService>();

        // Phase 12 - Notifications
        services.AddSingleton<NotificationListenerService>();

        // Phase 13 - Auto Updater
        services.AddSingleton<IUpdateService, UpdateService>();

        // Phase 15 - Plugin System
        services.AddSingleton<PluginHost>();

        // Widgets — transient so each widget swap gets a fresh instance.
        services.AddTransient<UI.Widgets.MediaWidget>();
        services.AddTransient<UI.Widgets.CalendarWidget>();
        services.AddTransient<UI.Widgets.FavoritesWidget>();
        services.AddTransient<UI.Widgets.FileTrayWidget>();
        services.AddTransient<UI.Widgets.ClipboardWidget>();
        services.AddTransient<UI.Widgets.BluetoothWidget>();

        // UI
        services.AddSingleton<IslandForm>();
        services.AddTransient<SettingsForm>();

        return services.BuildServiceProvider();
    }
}
