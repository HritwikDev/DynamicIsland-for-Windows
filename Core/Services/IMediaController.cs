using System;
using System.Threading.Tasks;

namespace DynamicIsland.Core.Services;

public sealed record MediaInfo(
    string Title,
    string Artist,
    byte[]? AlbumArtPng,
    TimeSpan Position,
    TimeSpan Duration,
    bool IsPlaying);

/// <summary>
/// Wraps Windows' System Media Transport Controls session manager so the
/// island can show/control whatever is currently playing (Spotify, browser
/// media, etc.) without app-specific integrations.
/// </summary>
public interface IMediaController : IAsyncDisposable
{
    event EventHandler<MediaInfo?>? NowPlayingChanged;

    MediaInfo? Current { get; }

    Task InitializeAsync();
    Task PlayPauseAsync();
    Task NextAsync();
    Task PreviousAsync();
    Task SeekAsync(TimeSpan position);
}
