using System;

namespace DynamicIsland.Core.Models;

public sealed class FileTrayItem
{
    public required string Id { get; init; }
    public required string FilePath { get; init; }
    public DateTimeOffset AddedAt { get; init; } = DateTimeOffset.UtcNow;

    public string DisplayName => System.IO.Path.GetFileName(FilePath);
}
