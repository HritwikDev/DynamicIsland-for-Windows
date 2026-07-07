using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace DynamicIsland.Core.Storage;

/// <summary>
/// Single SQLite file shared by all structured-data stores (clipboard
/// history, file tray items, local calendar events). Settings stay in
/// JSON (see <see cref="SettingsService"/>) since they're simple key/value
/// and don't need querying.
/// </summary>
public sealed class AppDatabase
{
    private readonly string _connectionString;

    public AppDatabase()
    {
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicIsland");
        Directory.CreateDirectory(appDataDir);

        var dbPath = Path.Combine(appDataDir, "dynamicisland.db");
        _connectionString = $"Data Source={dbPath}";

        EnsureSchema();
    }

    public SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private void EnsureSchema()
    {
        using var connection = OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS ClipboardEntries (
                Id TEXT PRIMARY KEY,
                Kind TEXT NOT NULL,
                TextContent TEXT,
                FilePathsJson TEXT,
                CreatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS FileTrayItems (
                Id TEXT PRIMARY KEY,
                FilePath TEXT NOT NULL,
                AddedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS CalendarEvents (
                Id TEXT PRIMARY KEY,
                Title TEXT NOT NULL,
                StartsAt TEXT NOT NULL,
                EndsAt TEXT,
                Location TEXT
            );

            CREATE TABLE IF NOT EXISTS FavoriteItems (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                LaunchPath TEXT NOT NULL,
                SortOrder INTEGER NOT NULL DEFAULT 0
            );
            """;
        cmd.ExecuteNonQuery();
    }
}
