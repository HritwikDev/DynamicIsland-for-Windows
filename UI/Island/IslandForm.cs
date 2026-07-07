using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using DynamicIsland.Core.Models;
using DynamicIsland.Core.Services;
using DynamicIsland.UI.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace DynamicIsland.UI.Island;

/// <summary>
/// Phase 1/2/9/10/11 — the floating pill itself.
///
/// Simplifications versus the WinUI version (deliberate, per the switch to
/// WinForms for lower build complexity):
///   - Rounded corners via Form.Region (a clipped rectangle path) rather
///     than a per-pixel-alpha layered window. The boundary itself is still
///     a hard pixel cut (Region has no partial-alpha concept), but a native
///     CS_DROPSHADOW window class style + an anti-aliased inner border
///     stroke (see CreateParams / OnPaint below) go a long way toward
///     making the edge *read* as soft without the complexity of manually
///     compositing every child control into a layered bitmap.
///   - Animation is a plain Timer-driven eased tween of Size, not a
///     platform animation/composition API.
/// </summary>
public sealed class IslandForm : Form
{
    private static readonly Size CollapsedSize = new(220, 37);
    private static readonly Size CompactSize = new(320, 44);
    private static readonly Size ExpandedSize = new(360, 220);
    private static readonly Size NotchSize = new(140, 37);
    private const int CornerRadius = 20;
    private const int TopMarginPx = 8;
    private const int CS_DROPSHADOW = 0x00020000;

    private readonly IServiceProvider _services;
    private readonly IActivityManager _activityManager;
    private readonly List<Func<Control>> _widgetFactories;
    private int _currentWidgetIndex;
    private bool _isExpanded;

    private readonly Label _statusLabel;
    private readonly Panel _widgetHost;
    private readonly Panel _header;
    private readonly Label _widgetNameLabel;
    private readonly PillAnimator _animator;
    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _quickMenu;
    private static readonly string[] WidgetNames = { "Media", "Calendar", "Favorites", "Files", "Clipboard", "Bluetooth" };

    /// <summary>Adds a soft native drop-shadow around this borderless window — a cheap, real visual softener with no manual compositing needed.</summary>
    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ClassStyle |= CS_DROPSHADOW;
            return cp;
        }
    }

    public IslandForm(IServiceProvider services, IActivityManager activityManager)
    {
        _services = services;
        _activityManager = activityManager;

        // --- Window chrome ---
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        ShowInTaskbar = false;
        BackColor = Color.FromArgb(18, 18, 18);
        Opacity = 0.94;
        Size = CollapsedSize;
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

        var appIcon = UI.AppIconProvider.Load();
        Icon = appIcon;

        // --- Quick-access context menu (right-click the pill) ---
        _quickMenu = new ContextMenuStrip();
        _quickMenu.Items.Add("Settings", null, (_, _) => OpenSettings());
        _quickMenu.Items.Add(new ToolStripSeparator());
        _quickMenu.Items.Add("Exit", null, (_, _) => Application.Exit());

        // --- System tray icon: the app's only real quit/settings path,
        // since ShowInTaskbar=false and there's no title bar to close from. ---
        _trayIcon = new NotifyIcon
        {
            Icon = appIcon,
            Text = "Dynamic Island",
            Visible = true,
            ContextMenuStrip = _quickMenu
        };
        _trayIcon.DoubleClick += (_, _) => ToggleExpanded();

        // Header bar: always exists, but only visible while expanded. Gives
        // reliable prev/next/close buttons — WinForms MouseClick/MouseWheel
        // don't bubble from a Dock=Fill child up to the parent Form, so
        // relying on Form-level click/wheel handlers alone (an earlier
        // version of this file did that) meant clicking the pill did
        // nothing at all once a child control covered the whole surface.
        _header = new Panel { Dock = DockStyle.Top, Height = 24, Visible = false, BackColor = Color.FromArgb(28, 28, 28) };
        var prevButton = MakeHeaderButton("‹");
        var nextButton = MakeHeaderButton("›");
        var closeButton = MakeHeaderButton("✕");
        prevButton.Dock = DockStyle.Left;
        nextButton.Dock = DockStyle.Right;
        closeButton.Dock = DockStyle.Right;
        _widgetNameLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.Silver, Font = new Font("Segoe UI", 8f) };

        prevButton.Click += (_, _) => CycleWidget(-1);
        nextButton.Click += (_, _) => CycleWidget(+1);
        closeButton.Click += (_, _) => Collapse();

        _header.Controls.Add(_widgetNameLabel);
        _header.Controls.Add(nextButton);
        _header.Controls.Add(closeButton);
        _header.Controls.Add(prevButton);
        _header.ContextMenuStrip = _quickMenu;
        Controls.Add(_header);

        _statusLabel = new Label
        {
            Text = "Dynamic Island",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9f),
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 12, 0),
            Cursor = Cursors.Hand,
            ContextMenuStrip = _quickMenu
        };
        var statusTip = new ToolTip();
        statusTip.SetToolTip(_statusLabel, "Click to expand · Right-click for options");

        // Attached directly to the Label, since it's what's actually under
        // the cursor when the pill is collapsed/compact — this is the fix
        // for clicks previously doing nothing at all.
        _statusLabel.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) Expand(); };
        // A subtle hover highlight so the pill visibly reads as interactive.
        _statusLabel.MouseEnter += (_, _) => _statusLabel.ForeColor = Color.FromArgb(220, 235, 255);
        _statusLabel.MouseLeave += (_, _) => _statusLabel.ForeColor = Color.White;
        Controls.Add(_statusLabel);

        _widgetHost = new Panel { Dock = DockStyle.Fill, Visible = false };
        Controls.Add(_widgetHost);

        _animator = new PillAnimator(this);

        Resize += (_, _) => ApplyRoundedRegion();
        Paint += OnPaintBorder;
        FormClosing += (_, _) => _trayIcon.Visible = false;

        _widgetFactories = new List<Func<Control>>
        {
            () => _services.GetRequiredService<UI.Widgets.MediaWidget>(),
            () => _services.GetRequiredService<UI.Widgets.CalendarWidget>(),
            () => _services.GetRequiredService<UI.Widgets.FavoritesWidget>(),
            () => _services.GetRequiredService<UI.Widgets.FileTrayWidget>(),
            () => _services.GetRequiredService<UI.Widgets.ClipboardWidget>(),
            () => _services.GetRequiredService<UI.Widgets.BluetoothWidget>(),
        };

        _activityManager.TopActivityChanged += OnTopActivityChanged;

        Load += (_, _) =>
        {
            PositionAtTopCenter();
            ApplyRoundedRegion();
        };
    }

    private static Button MakeHeaderButton(string text)
    {
        var button = new Button
        {
            Text = text,
            Width = 28,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(28, 28, 28),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(48, 48, 48);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(64, 64, 64);
        return button;
    }

    private void OpenSettings()
    {
        var settingsForm = _services.GetRequiredService<SettingsForm>();
        settingsForm.ShowDialog(this);
    }

    private void ToggleExpanded()
    {
        if (_isExpanded) Collapse(); else Expand();
    }

    private void PositionAtTopCenter()
    {
        var workArea = Screen.PrimaryScreen!.WorkingArea;
        var x = workArea.X + (workArea.Width - Width) / 2;
        var y = workArea.Y + TopMarginPx;
        Location = new Point(x, y);
    }

    /// <summary>Clips the rectangular Win32 window down to a rounded-pill silhouette.</summary>
    private void ApplyRoundedRegion()
    {
        var path = RoundedRectPath(new Rectangle(0, 0, Width, Height), CornerRadius);
        Region?.Dispose();
        Region = new Region(path);
        Invalidate();
    }

    /// <summary>Draws a soft, anti-aliased 1px border just inside the clip boundary — the Region cut is still hard, but this makes the visible edge read as deliberate/finished rather than jagged.</summary>
    private void OnPaintBorder(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = RoundedRectPath(new Rectangle(0, 0, Width - 1, Height - 1), CornerRadius);
        using var pen = new Pen(Color.FromArgb(90, 255, 255, 255), 1f);
        e.Graphics.DrawPath(pen, path);
    }

    private static GraphicsPath RoundedRectPath(Rectangle bounds, int radius)
    {
        int d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private void OnTopActivityChanged(object? sender, ActivityModel? top)
    {
        if (InvokeRequired) { BeginInvoke(() => OnTopActivityChanged(sender, top)); return; }

        if (top is null)
        {
            _statusLabel.Text = "Dynamic Island";
            if (!_isExpanded) AnimateTo(CollapsedSize);
            return;
        }

        var subtitle = top.Subtitle is { Length: > 0 } s ? $" · {s}" : "";
        bool isTransient = top.ExpiresAt is not null && (top.ExpiresAt - DateTimeOffset.UtcNow) < TimeSpan.FromSeconds(10);

        if (isTransient && !_isExpanded)
        {
            _statusLabel.Text = $"{top.Title}{subtitle}";
            AnimateTo(NotchSize, pulse: true);
            return;
        }

        if (!_isExpanded)
        {
            _statusLabel.Text = $"{top.Title}{subtitle}";
            AnimateTo(CompactSize);
        }
    }

    /// <summary>Expands the pill into the full widget panel.</summary>
    private void Expand()
    {
        if (_isExpanded) return;
        _isExpanded = true;

        _statusLabel.Visible = false;
        _header.Visible = true;
        _widgetHost.Visible = true;
        ShowWidget(_currentWidgetIndex);
        AnimateTo(ExpandedSize, recenter: true);
    }

    private void Collapse()
    {
        if (!_isExpanded) return;
        _isExpanded = false;

        _header.Visible = false;
        _widgetHost.Visible = false;
        _statusLabel.Visible = true;
        ClearWidgetHost();
        AnimateTo(CollapsedSize, recenter: true);
    }

    /// <summary>Phase 11 — Scroll Navigation: cycle widgets via header buttons (reliable) or mouse wheel (bonus, works when the active widget doesn't consume it itself).</summary>
    private void CycleWidget(int direction)
    {
        _currentWidgetIndex = (_currentWidgetIndex + direction + _widgetFactories.Count) % _widgetFactories.Count;
        ShowWidget(_currentWidgetIndex);
    }

    private void ShowWidget(int index)
    {
        _widgetNameLabel.Text = WidgetNames[index];
        ClearWidgetHost();
        var widget = _widgetFactories[index]();
        widget.Dock = DockStyle.Fill;
        _widgetHost.Controls.Add(widget);
    }

    /// <summary>
    /// Disposes the outgoing widget before removing it. Controls.Clear()
    /// alone does NOT call Dispose() on removed children — without this,
    /// every widget switch left the previous widget's Timer / clipboard
    /// listener / bluetooth watcher / event subscriptions still running,
    /// silently accumulating (each one still firing, updating a control
    /// that's no longer shown, and — for widgets that subscribe to a
    /// singleton service's events — piling up duplicate handlers on every
    /// revisit). Explicitly disposing here ensures each widget's
    /// HandleDestroyed cleanup actually runs.
    /// </summary>
    private void ClearWidgetHost()
    {
        foreach (Control control in _widgetHost.Controls)
        {
            control.Dispose();
        }
        _widgetHost.Controls.Clear();
    }

    private void AnimateTo(Size target, bool pulse = false, bool recenter = false)
    {
        var workArea = Screen.PrimaryScreen!.WorkingArea;
        int targetX = recenter ? workArea.X + (workArea.Width - target.Width) / 2 : Location.X - (target.Width - Width) / 2;
        var targetLocation = new Point(targetX, Location.Y);

        _animator.AnimateTo(target, targetLocation, pulse);
    }

    /// <summary>
    /// Drives Size/Location tweening with a Timer, re-clipping the rounded
    /// Region each frame so corners stay correct mid-animation. `pulse`
    /// briefly overshoots before settling (Phase 10 - Spring Notches).
    /// </summary>
    private sealed class PillAnimator
    {
        private readonly Form _form;
        private readonly System.Windows.Forms.Timer _timer;
        private DateTime _startTime;
        private Size _startSize;
        private Point _startLocation;
        private Size _targetSize;
        private Point _targetLocation;
        private bool _pulse;
        private const int DurationMs = 260;

        public PillAnimator(Form form)
        {
            _form = form;
            _timer = new System.Windows.Forms.Timer { Interval = 15 };
            _timer.Tick += OnTick;
        }

        public void AnimateTo(Size targetSize, Point targetLocation, bool pulse)
        {
            _startSize = _form.Size;
            _startLocation = _form.Location;
            _targetSize = targetSize;
            _targetLocation = targetLocation;
            _pulse = pulse;
            _startTime = DateTime.UtcNow;
            _timer.Start();
        }

        private void OnTick(object? sender, EventArgs e)
        {
            var elapsed = (DateTime.UtcNow - _startTime).TotalMilliseconds;
            var t = Math.Clamp(elapsed / DurationMs, 0.0, 1.0);
            var eased = EaseOutBack(t);

            // Pulse overshoots ~12% at the midpoint then settles — a cheap
            // stand-in for a real spring curve, cheap to compute per tick.
            var overshoot = _pulse ? (float)(Math.Sin(t * Math.PI) * 0.12) : 0f;
            var scale = eased + overshoot;

            var w = Lerp(_startSize.Width, _targetSize.Width, scale);
            var h = Lerp(_startSize.Height, _targetSize.Height, scale);
            var x = Lerp(_startLocation.X, _targetLocation.X, (float)eased);
            var y = Lerp(_startLocation.Y, _targetLocation.Y, (float)eased);

            _form.Size = new Size(Math.Max(1, w), Math.Max(1, h));
            _form.Location = new Point(x, y);

            if (_form is IslandForm island) island.ApplyRoundedRegion();

            if (t >= 1.0)
            {
                _timer.Stop();
                _form.Size = _targetSize;
                _form.Location = _targetLocation;
                if (_form is IslandForm island2) island2.ApplyRoundedRegion();
            }
        }

        private static int Lerp(int a, int b, double t) => (int)(a + (b - a) * t);

        private static double EaseOutBack(double t)
        {
            const double c1 = 1.4;
            const double c3 = c1 + 1;
            return 1 + c3 * Math.Pow(t - 1, 3) + c1 * Math.Pow(t - 1, 2);
        }
    }
}
