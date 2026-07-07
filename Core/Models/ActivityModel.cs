using System;

namespace DynamicIsland.Core.Models;

/// <summary>
/// A single activity the island can surface (media playback, a download,
/// a timer, a bluetooth event, etc). The Activity System (Phase 9) manages
/// a priority-ordered collection of these; only the top one is rendered
/// in the collapsed pill.
/// </summary>
public sealed class ActivityModel
{
    public required string Id { get; init; }
    public required ActivityKind Kind { get; init; }
    public required string Title { get; set; }
    public string? Subtitle { get; set; }
    public double? ProgressPercent { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
    public int Priority { get; set; } = 0;
}

public enum ActivityKind
{
    Media,
    Download,
    Bluetooth,
    Calendar,
    Clipboard,
    Notification,
    Custom
}
