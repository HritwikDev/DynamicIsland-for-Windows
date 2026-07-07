using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace DynamicIsland.Core.Storage;

public sealed class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsPath;

    public AppSettings Current { get; private set; } = new();

    public SettingsService()
    {
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicIsland");

        Directory.CreateDirectory(appDataDir);
        _settingsPath = Path.Combine(appDataDir, "settings.json");
    }

    public async Task LoadAsync()
    {
        if (!File.Exists(_settingsPath))
        {
            Current = new AppSettings();
            await SaveAsync();
            return;
        }

        await using var stream = File.OpenRead(_settingsPath);
        Current = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions)
                  ?? new AppSettings();
    }

    public async Task SaveAsync()
    {
        await using var stream = File.Create(_settingsPath);
        await JsonSerializer.SerializeAsync(stream, Current, JsonOptions);
    }
}
