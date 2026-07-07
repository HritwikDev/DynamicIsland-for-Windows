using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using DynamicIsland.Core.Models;
using DynamicIsland.Core.Storage;

namespace DynamicIsland.Core.Services;

/// <summary>
/// Phase 7 — Clipboard Manager.
///
/// Uses the plain Win32 clipboard-change notification (AddClipboardFormatListener
/// + WM_CLIPBOARDUPDATE) via a hidden message-only window, and reads content
/// through System.Windows.Forms.Clipboard — no WinRT/UWP APIs needed at all,
/// which keeps this dependency-free compared to the WinUI version.
///
/// Image bytes are intentionally NOT stored inline (clipboard images can be
/// large/frequent); only a marker entry is kept for now.
/// </summary>
public sealed class ClipboardManager : IClipboardManager, IDisposable
{
    private readonly AppDatabase _db;
    private ClipboardListenerWindow? _listenerWindow;

    public event EventHandler<ClipboardEntry>? EntryAdded;

    public ClipboardManager(AppDatabase db) => _db = db;

    public void StartListening()
    {
        if (_listenerWindow is not null) return;
        _listenerWindow = new ClipboardListenerWindow(OnClipboardChanged);
    }

    public void StopListening()
    {
        _listenerWindow?.Dispose();
        _listenerWindow = null;
    }

    private void OnClipboardChanged()
    {
        try
        {
            if (!Clipboard.ContainsText() && !Clipboard.ContainsFileDropList() && !Clipboard.ContainsImage())
                return;

            ClipboardEntry? entry = null;

            if (Clipboard.ContainsFileDropList())
            {
                var files = Clipboard.GetFileDropList().Cast<string>().ToList();
                entry = new ClipboardEntry { Id = Guid.NewGuid().ToString(), Kind = ClipboardEntryKind.Files, FilePaths = files };
            }
            else if (Clipboard.ContainsText())
            {
                var text = Clipboard.GetText();
                entry = new ClipboardEntry { Id = Guid.NewGuid().ToString(), Kind = ClipboardEntryKind.Text, TextContent = text };
            }
            else if (Clipboard.ContainsImage())
            {
                entry = new ClipboardEntry { Id = Guid.NewGuid().ToString(), Kind = ClipboardEntryKind.Image };
            }

            if (entry is null) return;

            Persist(entry);
            EntryAdded?.Invoke(this, entry);
        }
        catch
        {
            // Clipboard access can throw transiently (another app holding it) — safe to skip a tick.
        }
    }

    private void Persist(ClipboardEntry entry)
    {
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO ClipboardEntries (Id, Kind, TextContent, FilePathsJson, CreatedAt)
            VALUES (@id, @kind, @text, @paths, @createdAt)
            """;
        cmd.Parameters.AddWithValue("@id", entry.Id);
        cmd.Parameters.AddWithValue("@kind", entry.Kind.ToString());
        cmd.Parameters.AddWithValue("@text", (object?)entry.TextContent ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@paths", entry.FilePaths is null ? DBNull.Value : JsonSerializer.Serialize(entry.FilePaths));
        cmd.Parameters.AddWithValue("@createdAt", entry.CreatedAt.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    public Task<IReadOnlyList<ClipboardEntry>> GetHistoryAsync(int maxCount = 50)
    {
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, Kind, TextContent, FilePathsJson, CreatedAt
            FROM ClipboardEntries
            ORDER BY CreatedAt DESC
            LIMIT @max
            """;
        cmd.Parameters.AddWithValue("@max", maxCount);

        var results = new List<ClipboardEntry>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var kind = Enum.Parse<ClipboardEntryKind>(reader.GetString(1));
            var pathsJson = reader.IsDBNull(3) ? null : reader.GetString(3);

            results.Add(new ClipboardEntry
            {
                Id = reader.GetString(0),
                Kind = kind,
                TextContent = reader.IsDBNull(2) ? null : reader.GetString(2),
                FilePaths = pathsJson is null ? null : JsonSerializer.Deserialize<List<string>>(pathsJson),
                CreatedAt = DateTimeOffset.Parse(reader.GetString(4))
            });
        }

        return Task.FromResult<IReadOnlyList<ClipboardEntry>>(results);
    }

    public Task ClearAsync()
    {
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM ClipboardEntries";
        cmd.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public void CopyToClipboard(ClipboardEntry entry)
    {
        switch (entry.Kind)
        {
            case ClipboardEntryKind.Text:
                Clipboard.SetText(entry.TextContent ?? "");
                break;
            case ClipboardEntryKind.Files when entry.FilePaths is { Count: > 0 }:
                var collection = new System.Collections.Specialized.StringCollection();
                collection.AddRange(entry.FilePaths.ToArray());
                Clipboard.SetFileDropList(collection);
                break;
            default:
                return; // Image round-trip needs bytes we chose not to persist (see class remarks).
        }
    }

    public void Dispose() => StopListening();

    /// <summary>
    /// A tiny hidden window whose only job is receiving WM_CLIPBOARDUPDATE.
    /// This is the standard, dependency-free way to watch the clipboard on
    /// Win32 — no WinRT/UWP round-trip needed.
    /// </summary>
    private sealed class ClipboardListenerWindow : NativeWindow, IDisposable
    {
        private const int WM_CLIPBOARDUPDATE = 0x031D;
        private readonly Action _onChanged;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        public ClipboardListenerWindow(Action onChanged)
        {
            _onChanged = onChanged;
            CreateHandle(new CreateParams());
            AddClipboardFormatListener(Handle);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_CLIPBOARDUPDATE)
            {
                _onChanged();
            }
            base.WndProc(ref m);
        }

        public void Dispose()
        {
            if (Handle != IntPtr.Zero)
            {
                RemoveClipboardFormatListener(Handle);
                DestroyHandle();
            }
        }
    }
}
