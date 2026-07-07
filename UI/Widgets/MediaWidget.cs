using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using DynamicIsland.Core.Models;
using DynamicIsland.Core.Services;

namespace DynamicIsland.UI.Widgets;

/// <summary>Phase 3 — Media Controller UI. Renders whatever IMediaController reports.</summary>
public sealed class MediaWidget : UserControl
{
    private readonly IMediaController _mediaController;
    private readonly IActivityManager _activityManager;
    private const string ActivityId = "media-now-playing";

    // Segoe MDL2 Assets is Windows' built-in icon font — these codepoints
    // always render as crisp vector icons. The previous version used
    // emoji-style glyphs (⏮⏯⏭) directly as Button.Text, which several
    // Windows font-fallback configurations render as blank/garbled boxes
    // instead of an icon — that's the "not visible" bug from the screenshot.
    private const string GlyphPrevious = "\uE892";
    private const string GlyphPlay = "\uE768";
    private const string GlyphPause = "\uE769";
    private const string GlyphNext = "\uE893";
    private static readonly Font IconFont = new("Segoe MDL2 Assets", 13f);

    private readonly PictureBox _albumArt;
    private readonly Label _titleLabel;
    private readonly Label _artistLabel;
    private readonly Button _playPauseButton;
    private readonly ProgressBar _progressBar;

    public MediaWidget(IMediaController mediaController, IActivityManager activityManager)
    {
        _mediaController = mediaController;
        _activityManager = activityManager;
        BackColor = Color.Transparent;

        _albumArt = new PictureBox
        {
            Size = new Size(48, 48),
            Location = new Point(14, 28),
            BackColor = Color.FromArgb(51, 51, 51),
            SizeMode = PictureBoxSizeMode.StretchImage
        };
        ApplyRoundedCorners(_albumArt, 8);

        _titleLabel = new Label
        {
            ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Location = new Point(72, 32), Size = new Size(270, 18), AutoEllipsis = true
        };
        _artistLabel = new Label
        {
            ForeColor = Color.Silver, Font = new Font("Segoe UI", 8.5f),
            Location = new Point(72, 52), Size = new Size(270, 16), AutoEllipsis = true
        };

        _progressBar = new ProgressBar
        {
            Location = new Point(14, 86), Size = new Size(328, 4),
            Style = ProgressBarStyle.Continuous, Minimum = 0, Maximum = 100
        };

        // Transport controls, centered as a row beneath everything else.
        // The play/pause button is visually emphasized (filled accent
        // circle) since it's the primary action; prev/next are flat.
        var prevButton = MakeTransportButton(GlyphPrevious, accent: false);
        _playPauseButton = MakeTransportButton(GlyphPlay, accent: true);
        var nextButton = MakeTransportButton(GlyphNext, accent: false);

        const int buttonSize = 36;
        const int gap = 12;
        int totalWidth = buttonSize * 3 + gap * 2;
        int startX = (356 - totalWidth) / 2;
        prevButton.Location = new Point(startX, 102);
        _playPauseButton.Location = new Point(startX + buttonSize + gap, 102);
        nextButton.Location = new Point(startX + (buttonSize + gap) * 2, 102);

        prevButton.Click += async (_, _) => await _mediaController.PreviousAsync();
        _playPauseButton.Click += async (_, _) => await _mediaController.PlayPauseAsync();
        nextButton.Click += async (_, _) => await _mediaController.NextAsync();

        Controls.AddRange(new Control[]
        {
            _albumArt, _titleLabel, _artistLabel, _progressBar, prevButton, _playPauseButton, nextButton
        });

        // Subscribing/unsubscribing paired with HandleCreated/HandleDestroyed
        // (not just the constructor) matters because IMediaController is a
        // singleton but this widget is transient — a fresh MediaWidget is
        // created every time you scroll back to it. Subscribing once in the
        // constructor with no matching unsubscribe would leave a stale
        // handler registered on the singleton every time, each one still
        // firing (and trying to update a disposed widget) forever after.
        HandleCreated += async (_, _) =>
        {
            _mediaController.NowPlayingChanged += OnNowPlayingChanged;
            await _mediaController.InitializeAsync();
            // InitializeAsync no-ops if already initialized by an earlier
            // widget instance, so it won't re-fire NowPlayingChanged —
            // render whatever's already known right away instead of
            // waiting for the next actual change.
            OnNowPlayingChanged(this, _mediaController.Current);
        };
        HandleDestroyed += (_, _) => _mediaController.NowPlayingChanged -= OnNowPlayingChanged;
    }

    private static Button MakeTransportButton(string glyph, bool accent)
    {
        var button = new Button
        {
            Text = glyph,
            Font = IconFont,
            Size = new Size(36, 36),
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = accent ? Color.FromArgb(10, 132, 255) : Color.FromArgb(45, 45, 45),
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter,
            UseCompatibleTextRendering = false
        };
        ApplyRoundedCorners(button, 18);
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = accent
            ? Color.FromArgb(48, 156, 255)
            : Color.FromArgb(64, 64, 64);
        button.FlatAppearance.MouseDownBackColor = accent
            ? Color.FromArgb(0, 100, 210)
            : Color.FromArgb(30, 30, 30);
        return button;
    }

    private static void ApplyRoundedCorners(Control control, int radius)
    {
        var path = new GraphicsPath();
        var bounds = new Rectangle(0, 0, control.Width, control.Height);
        int d = radius * 2;
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        control.Region = new Region(path);
    }

    private void OnNowPlayingChanged(object? sender, MediaInfo? info)
    {
        if (InvokeRequired) { BeginInvoke(() => OnNowPlayingChanged(sender, info)); return; }

        if (info is null)
        {
            _titleLabel.Text = "Nothing playing";
            _artistLabel.Text = "Play something on Spotify, a browser tab, etc.";
            _albumArt.Image = null;
            _progressBar.Value = 0;
            _playPauseButton.Text = GlyphPlay;
            _activityManager.Remove(ActivityId);
            return;
        }

        _titleLabel.Text = info.Title;
        _artistLabel.Text = info.Artist;
        _playPauseButton.Text = info.IsPlaying ? GlyphPause : GlyphPlay;

        if (info.AlbumArtPng is { Length: > 0 } bytes)
        {
            using var ms = new MemoryStream(bytes);
            _albumArt.Image = Image.FromStream(ms);
        }

        var progress = info.Duration.TotalSeconds > 0
            ? info.Position.TotalSeconds / info.Duration.TotalSeconds * 100
            : 0;
        _progressBar.Value = Math.Clamp((int)progress, 0, 100);

        _activityManager.Push(new ActivityModel
        {
            Id = ActivityId,
            Kind = ActivityKind.Media,
            Title = info.Title,
            Subtitle = info.Artist,
            ProgressPercent = progress,
            Priority = info.IsPlaying ? 3 : 1
        });
    }
}
