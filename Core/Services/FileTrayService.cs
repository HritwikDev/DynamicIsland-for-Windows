using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DynamicIsland.Core.Models;
using DynamicIsland.Core.Storage;

namespace DynamicIsland.Core.Services;

/// <summary>
/// Phase 6 — File Tray. Files dropped onto the island are remembered here
/// (by path — the file itself stays wherever it was) so they persist
/// across restarts, and can be reopened or dragged back out to another app.
/// </summary>
public sealed class FileTrayService : IFileTrayService
{
    private readonly AppDatabase _db;

    public FileTrayService(AppDatabase db) => _db = db;

    public Task<IReadOnlyList<FileTrayItem>> GetAllAsync()
    {
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, FilePath, AddedAt FROM FileTrayItems ORDER BY AddedAt DESC";

        var results = new List<FileTrayItem>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new FileTrayItem
            {
                Id = reader.GetString(0),
                FilePath = reader.GetString(1),
                AddedAt = DateTimeOffset.Parse(reader.GetString(2))
            });
        }

        return Task.FromResult<IReadOnlyList<FileTrayItem>>(results);
    }

    public Task AddAsync(string filePath)
    {
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO FileTrayItems (Id, FilePath, AddedAt)
            VALUES (@id, @path, @addedAt)
            """;
        cmd.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
        cmd.Parameters.AddWithValue("@path", filePath);
        cmd.Parameters.AddWithValue("@addedAt", DateTimeOffset.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string id)
    {
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM FileTrayItems WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public void OpenWithDefaultApp(FileTrayItem item)
    {
        Process.Start(new ProcessStartInfo(item.FilePath) { UseShellExecute = true });
    }
}
