using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DynamicIsland.Core.Models;
using DynamicIsland.Core.Storage;

namespace DynamicIsland.Core.Services;

public sealed class FavoritesService : IFavoritesService
{
    private readonly AppDatabase _db;

    public FavoritesService(AppDatabase db) => _db = db;

    public Task<IReadOnlyList<FavoriteItem>> GetAllAsync()
    {
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, LaunchPath, SortOrder FROM FavoriteItems ORDER BY SortOrder ASC";

        var results = new List<FavoriteItem>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new FavoriteItem
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1),
                LaunchPath = reader.GetString(2),
                SortOrder = reader.GetInt32(3)
            });
        }

        return Task.FromResult<IReadOnlyList<FavoriteItem>>(results);
    }

    public Task AddAsync(FavoriteItem item)
    {
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO FavoriteItems (Id, Name, LaunchPath, SortOrder)
            VALUES (@id, @name, @path, @sort)
            """;
        cmd.Parameters.AddWithValue("@id", item.Id);
        cmd.Parameters.AddWithValue("@name", item.Name);
        cmd.Parameters.AddWithValue("@path", item.LaunchPath);
        cmd.Parameters.AddWithValue("@sort", item.SortOrder);
        cmd.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string id)
    {
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM FavoriteItems WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public void Launch(FavoriteItem item)
    {
        Process.Start(new ProcessStartInfo(item.LaunchPath) { UseShellExecute = true });
    }
}
