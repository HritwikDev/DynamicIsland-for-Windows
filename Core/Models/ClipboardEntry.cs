using System;
using System.Collections.Generic;

namespace DynamicIsland.Core.Models;

public enum ClipboardEntryKind
{
    Text,
    Html,
    Image,
    Files
}

public sealed class ClipboardEntry
{
    public required string Id { get; init; }
    public required ClipboardEntryKind Kind { get; init; }
    public string? TextContent { get; init; }
    public IReadOnlyList<string>? FilePaths { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Short one-line summary used in list UIs.</summary>
    public string Preview => Kind switch
    {
        ClipboardEntryKind.Text => Truncate(TextContent),
        ClipboardEntryKind.Html => "HTML content",
        ClipboardEntryKind.Image => "Image",
        ClipboardEntryKind.Files => $"{FilePaths?.Count ?? 0} file(s)",
        _ => ""
    };

    private static string Truncate(string? text, int max = 60)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        text = text.Replace('\n', ' ').Replace('\r', ' ');
        return text.Length <= max ? text : text[..max] + "…";
    }
}
