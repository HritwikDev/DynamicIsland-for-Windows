namespace DynamicIsland.Core.Storage;

/// <summary>
/// Lightweight user preferences, persisted as JSON (fast, human-editable,
/// no schema migrations needed). Structured/queryable data — clipboard
/// history, file tray items — belongs in SQLite instead (see Phase 6/7).
/// </summary>
public sealed class AppSettings
{
    public bool LaunchOnStartup { get; set; } = true;
    public bool ShowOnAllMonitors { get; set; } = false;
    public string Theme { get; set; } = "Dark";
    public double IslandOpacity { get; set; } = 0.92;
    public bool EnableMediaWidget { get; set; } = true;
    public bool EnableCalendarWidget { get; set; } = true;
    public bool EnableBluetoothWidget { get; set; } = true;
    public bool EnableClipboardManager { get; set; } = true;
}
