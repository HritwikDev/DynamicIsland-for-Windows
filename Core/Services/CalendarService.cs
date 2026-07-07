using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DynamicIsland.Core.Models;
using DynamicIsland.Core.Storage;

namespace DynamicIsland.Core.Services;

public sealed class CalendarService : ICalendarService
{
    private readonly AppDatabase _db;

    public CalendarService(AppDatabase db) => _db = db;

    public Task<IReadOnlyList<CalendarEvent>> GetUpcomingAsync(int maxCount = 5)
    {
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, Title, StartsAt, EndsAt, Location
            FROM CalendarEvents
            WHERE StartsAt >= @now
            ORDER BY StartsAt ASC
            LIMIT @max
            """;
        cmd.Parameters.AddWithValue("@now", DateTimeOffset.Now.ToString("O"));
        cmd.Parameters.AddWithValue("@max", maxCount);

        var results = new List<CalendarEvent>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new CalendarEvent
            {
                Id = reader.GetString(0),
                Title = reader.GetString(1),
                StartsAt = DateTimeOffset.Parse(reader.GetString(2)),
                EndsAt = reader.IsDBNull(3) ? null : DateTimeOffset.Parse(reader.GetString(3)),
                Location = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
        }

        return Task.FromResult<IReadOnlyList<CalendarEvent>>(results);
    }

    public Task AddAsync(CalendarEvent calendarEvent)
    {
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO CalendarEvents (Id, Title, StartsAt, EndsAt, Location)
            VALUES (@id, @title, @startsAt, @endsAt, @location)
            """;
        cmd.Parameters.AddWithValue("@id", calendarEvent.Id);
        cmd.Parameters.AddWithValue("@title", calendarEvent.Title);
        cmd.Parameters.AddWithValue("@startsAt", calendarEvent.StartsAt.ToString("O"));
        cmd.Parameters.AddWithValue("@endsAt", (object?)calendarEvent.EndsAt?.ToString("O") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@location", (object?)calendarEvent.Location ?? DBNull.Value);
        cmd.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string id)
    {
        using var connection = _db.OpenConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM CalendarEvents WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public async Task<CalendarEvent?> GetNextAsync()
        => (await GetUpcomingAsync(1)).FirstOrDefault();
}
