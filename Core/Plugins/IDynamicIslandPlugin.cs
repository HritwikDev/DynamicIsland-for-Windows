using DynamicIsland.Core.Services;
using System.Windows.Forms;

namespace DynamicIsland.Core.Plugins;

/// <summary>
/// Phase 15 — Plugin System contract. A plugin contributes activities
/// (via <see cref="IActivityManager"/>) and/or a widget UI; the host only
/// needs a parameterless constructor to exist so it can be created via
/// reflection after loading the plugin's assembly.
/// </summary>
public interface IDynamicIslandPlugin
{
    string Name { get; }
    string Version { get; }

    /// <summary>Called once after load, with host services available for DI-style construction.</summary>
    void Initialize(IActivityManager activityManager);

    /// <summary>Optional widget content to add to the expanded island. Null if this plugin is headless.</summary>
    Control? CreateWidget();
}
