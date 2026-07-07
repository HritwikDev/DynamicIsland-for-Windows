using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace DynamicIsland.Core.Services;

/// <summary>
/// Phase 3 — Media Controller.
/// Uses GlobalSystemMediaTransportControlsSessionManager, the same API
/// behind Windows 11's own media flyout, so this works against Spotify,
/// browser tabs, and any other app that reports SMTC — no per-app SDKs.
/// </summary>
public sealed class MediaController : IMediaController
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _session;
    private bool _initialized;

    public event EventHandler<MediaInfo?>? NowPlayingChanged;

    public MediaInfo? Current { get; private set; }

    public async Task InitializeAsync()
    {
        // This controller is a DI singleton, but MediaWidget (its caller) is
        // transient — a new widget instance calls InitializeAsync() every
        // time you scroll back to Media. Without this guard, each call
        // would re-request the session manager and stack another
        // CurrentSessionChanged subscription, causing refreshes to fire
        // multiple times over and pile up further with each revisit.
        if (_initialized) return;
        _initialized = true;

        _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        _manager.CurrentSessionChanged += OnCurrentSessionChanged;
        AttachToSession(_manager.GetCurrentSession());
    }

    private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
        => AttachToSession(sender.GetCurrentSession());

    private void AttachToSession(GlobalSystemMediaTransportControlsSession? session)
    {
        if (_session is not null)
        {
            _session.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            _session.PlaybackInfoChanged -= OnPlaybackInfoChanged;
            _session.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
        }

        _session = session;

        if (_session is null)
        {
            Current = null;
            NowPlayingChanged?.Invoke(this, null);
            return;
        }

        _session.MediaPropertiesChanged += OnMediaPropertiesChanged;
        _session.PlaybackInfoChanged += OnPlaybackInfoChanged;
        _session.TimelinePropertiesChanged += OnTimelinePropertiesChanged;

        _ = RefreshAsync();
    }

    private async void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        => await RefreshAsync();

    private async void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
        => await RefreshAsync();

    private async void OnTimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
        => await RefreshAsync();

    private async Task RefreshAsync()
    {
        if (_session is null) return;

        var props = await _session.TryGetMediaPropertiesAsync();
        var playback = _session.GetPlaybackInfo();
        var timeline = _session.GetTimelineProperties();

        byte[]? albumArt = null;
        if (props.Thumbnail is not null)
        {
            albumArt = await ReadThumbnailAsync(props.Thumbnail);
        }

        Current = new MediaInfo(
            Title: props.Title ?? "Unknown",
            Artist: props.Artist ?? "",
            AlbumArtPng: albumArt,
            Position: timeline.Position,
            Duration: timeline.EndTime - timeline.StartTime,
            IsPlaying: playback.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing);

        NowPlayingChanged?.Invoke(this, Current);
    }

    private static async Task<byte[]?> ReadThumbnailAsync(IRandomAccessStreamReference thumbnailRef)
    {
        try
        {
            using var stream = await thumbnailRef.OpenReadAsync();
            using var netStream = stream.AsStreamForRead();
            using var ms = new MemoryStream();
            await netStream.CopyToAsync(ms);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }

    public Task PlayPauseAsync() => _session?.TryTogglePlayPauseAsync().AsTask() ?? Task.CompletedTask;

    public Task NextAsync() => _session?.TrySkipNextAsync().AsTask() ?? Task.CompletedTask;

    public Task PreviousAsync() => _session?.TrySkipPreviousAsync().AsTask() ?? Task.CompletedTask;

    public Task SeekAsync(TimeSpan position) =>
        _session?.TryChangePlaybackPositionAsync(position.Ticks).AsTask() ?? Task.CompletedTask;

    public ValueTask DisposeAsync()
    {
        if (_manager is not null)
            _manager.CurrentSessionChanged -= OnCurrentSessionChanged;

        if (_session is not null)
        {
            _session.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            _session.PlaybackInfoChanged -= OnPlaybackInfoChanged;
            _session.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
        }

        return ValueTask.CompletedTask;
    }
}
