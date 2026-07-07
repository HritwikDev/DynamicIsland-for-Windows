namespace DynamicIsland.Core.Models;

/// <summary>A pinned app/file/URL shortcut shown in the Favorites tray.</summary>
public sealed class FavoriteItem
{
    public required string Id { get; init; }
    public required string Name { get; set; }

    /// <summary>Path to an .exe, a file, or a URL — resolved with Process.Start.</summary>
    public required string LaunchPath { get; set; }

    public int SortOrder { get; set; }
}
