using System;

namespace DynamicIsland.Core.Models;

/// <summary>
/// A locally-stored event. Phase 4 ships with manual/local events; syncing
/// from the Windows Calendar app or Outlook is a natural follow-up but
/// needs an account-connected API (Microsoft Graph) which is out of scope
/// for this pass.
/// </summary>
public sealed class CalendarEvent
{
    public required string Id { get; init; }
    public required string Title { get; set; }
    public required DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public string? Location { get; set; }

    public TimeSpan CountdownFromNow => StartsAt - DateTimeOffset.Now;
}
