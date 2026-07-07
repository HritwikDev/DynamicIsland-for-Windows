using System;
using System.Linq;
using System.Threading;

namespace DynamicIsland.Core.Services;

/// <summary>
/// Phase 9 — Activity System (part 2). ActivityManager itself is a pure
/// priority queue; this ticks every second and evicts anything past its
/// ExpiresAt so transient activities (bluetooth connect pings, brief
/// notifications) don't linger in the island forever.
/// </summary>
public sealed class ActivitySweeper : IDisposable
{
    private readonly IActivityManager _activityManager;
    private readonly System.Threading.Timer _timer;

    public ActivitySweeper(IActivityManager activityManager)
    {
        _activityManager = activityManager;
        _timer = new System.Threading.Timer(Sweep, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    private void Sweep(object? state)
    {
        var now = DateTimeOffset.UtcNow;
        var expired = _activityManager.ActiveActivities
            .Where(a => a.ExpiresAt is not null && a.ExpiresAt <= now)
            .Select(a => a.Id)
            .ToList();

        foreach (var id in expired)
        {
            _activityManager.Remove(id);
        }
    }

    public void Dispose() => _timer.Dispose();
}
