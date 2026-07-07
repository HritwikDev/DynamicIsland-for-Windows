using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using DynamicIsland.Core.Services;

namespace DynamicIsland.Core.Plugins;

/// <summary>
/// Phase 15 — Plugin System. Loads any assembly dropped into
/// %LocalAppData%\DynamicIsland\Plugins implementing
/// <see cref="IDynamicIslandPlugin"/>, in its own <see cref="AssemblyLoadContext"/>
/// so a broken plugin can't take down the host and (with more work later)
/// could be unloaded/reloaded at runtime.
///
/// This is a deliberately small first cut — no dependency isolation beyond
/// the load context, no versioned plugin API, no sandboxing/permissions.
/// A "Plugin Marketplace" (Future Enhancement in the plan) would need a
/// signing/verification step before loading anything downloaded remotely.
/// </summary>
public sealed class PluginHost
{
    private readonly IActivityManager _activityManager;
    private readonly List<IDynamicIslandPlugin> _loadedPlugins = new();

    public IReadOnlyList<IDynamicIslandPlugin> LoadedPlugins => _loadedPlugins;

    public PluginHost(IActivityManager activityManager) => _activityManager = activityManager;

    public void LoadAllFromDefaultDirectory()
    {
        var pluginsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicIsland", "Plugins");

        Directory.CreateDirectory(pluginsDir);

        foreach (var dllPath in Directory.EnumerateFiles(pluginsDir, "*.dll"))
        {
            TryLoadPlugin(dllPath);
        }
    }

    private void TryLoadPlugin(string dllPath)
    {
        try
        {
            var loadContext = new AssemblyLoadContext(Path.GetFileNameWithoutExtension(dllPath), isCollectible: true);
            var assembly = loadContext.LoadFromAssemblyPath(dllPath);

            var pluginTypes = assembly.GetTypes()
                .Where(t => typeof(IDynamicIslandPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in pluginTypes)
            {
                if (Activator.CreateInstance(type) is not IDynamicIslandPlugin plugin) continue;

                plugin.Initialize(_activityManager);
                _loadedPlugins.Add(plugin);
            }
        }
        catch (Exception ex)
        {
            // A malformed plugin shouldn't take the whole app down.
            System.Diagnostics.Debug.WriteLine($"[PluginHost] Failed to load {dllPath}: {ex.Message}");
        }
    }
}
