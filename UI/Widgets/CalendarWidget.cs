using System;
using System.Drawing;
using System.Windows.Forms;
using DynamicIsland.Core.Models;
using DynamicIsland.Core.Services;

namespace DynamicIsland.UI.Widgets;

/// <summary>
/// Phase 4 — Calendar Widget. Polls for the next event every 30s.
///
/// This is a local, manually-entered event store (SQLite-backed) — it does
/// NOT read your real Windows Calendar or Outlook (that needs Microsoft
/// Graph / account sign-in, out of scope here — see README). Use the
/// "+ Add Event" button below to add a test event; otherwise this will
/// always show "No upcoming events" since the store starts empty and
/// nothing else populates it.
/// </summary>
public sealed class CalendarWidget : UserControl
{
    private const string ActivityId = "calendar-next-event";
    private static readonly TimeSpan HeadsUpWindow = TimeSpan.FromHours(1);

    private readonly ICalendarService _calendarService;
    private readonly IActivityManager _activityManager;
    private readonly System.Windows.Forms.Timer _pollTimer;
    private readonly Label _eventLabel;
    private readonly Label _countdownLabel;
    private readonly Button _addEventButton;

    public CalendarWidget(ICalendarService calendarService, IActivityManager activityManager)
    {
        _calendarService = calendarService;
        _activityManager = activityManager;

        _eventLabel = new Label
        {
            ForeColor = Color.White, Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Location = new Point(14, 24), AutoSize = true
        };
        _countdownLabel = new Label
        {
            ForeColor = Color.Silver, Font = new Font("Segoe UI", 8f),
            Location = new Point(14, 44), AutoSize = true
        };
        _addEventButton = new Button
        {
            Text = "+ Add Event", FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White, BackColor = Color.FromArgb(42, 42, 42),
            Location = new Point(14, 70), AutoSize = true, Cursor = Cursors.Hand
        };
        _addEventButton.FlatAppearance.BorderSize = 0;
        _addEventButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 60, 60);
        _addEventButton.Click += OnAddEventClicked;

        Controls.AddRange(new Control[] { _eventLabel, _countdownLabel, _addEventButton });

        _pollTimer = new System.Windows.Forms.Timer { Interval = 30_000 };
        _pollTimer.Tick += async (_, _) => await RefreshAsync();

        HandleCreated += async (_, _) =>
        {
            await RefreshAsync();
            _pollTimer.Start();
        };
        HandleDestroyed += (_, _) => _pollTimer.Stop();
    }

    private async void OnAddEventClicked(object? sender, EventArgs e)
    {
        using var dialog = new AddEventDialog();
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (string.IsNullOrWhiteSpace(dialog.EventTitle)) return;

        await _calendarService.AddAsync(new CalendarEvent
        {
            Id = Guid.NewGuid().ToString(),
            Title = dialog.EventTitle,
            StartsAt = dialog.EventStartsAt
        });

        await RefreshAsync();
    }

    private async System.Threading.Tasks.Task RefreshAsync()
    {
        var next = await _calendarService.GetNextAsync();

        if (InvokeRequired) { BeginInvoke(() => ApplyResult(next)); return; }
        ApplyResult(next);
    }

    private void ApplyResult(CalendarEvent? next)
    {
        if (next is null)
        {
            _eventLabel.Text = "No upcoming events";
            _countdownLabel.Text = "";
            _activityManager.Remove(ActivityId);
            return;
        }

        var countdown = next.CountdownFromNow;
        _eventLabel.Text = next.Title;
        _countdownLabel.Text = FormatCountdown(countdown);

        if (countdown <= HeadsUpWindow)
        {
            _activityManager.Push(new ActivityModel
            {
                Id = ActivityId,
                Kind = ActivityKind.Calendar,
                Title = next.Title,
                Subtitle = FormatCountdown(countdown),
                Priority = 4,
                ExpiresAt = next.EndsAt ?? next.StartsAt
            });
        }
        else
        {
            _activityManager.Remove(ActivityId);
        }
    }

    private static string FormatCountdown(TimeSpan span)
    {
        if (span.TotalMinutes < 1) return "Starting now";
        if (span.TotalHours < 1) return $"in {span.Minutes} min";
        if (span.TotalDays < 1) return $"in {(int)span.TotalHours}h {span.Minutes}m";
        return $"in {(int)span.TotalDays}d";
    }
}
