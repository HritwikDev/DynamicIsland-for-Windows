using System;
using System.Collections.Generic;
using System.Linq;
using DynamicIsland.Core.Models;

namespace DynamicIsland.Core.Services;

public sealed class ActivityManager : IActivityManager
{
    private readonly List<ActivityModel> _activities = new();
    private readonly object _lock = new();

    public event EventHandler<ActivityModel?>? TopActivityChanged;

    public IReadOnlyList<ActivityModel> ActiveActivities
    {
        get { lock (_lock) { return _activities.ToList(); } }
    }

    public void Push(ActivityModel activity)
    {
        lock (_lock)
        {
            _activities.RemoveAll(a => a.Id == activity.Id);
            _activities.Add(activity);
        }
        RaiseTopChanged();
    }

    public void Update(string activityId, Action<ActivityModel> mutate)
    {
        ActivityModel? target;
        lock (_lock)
        {
            target = _activities.FirstOrDefault(a => a.Id == activityId);
            if (target is null) return;
            mutate(target);
        }
        RaiseTopChanged();
    }

    public void Remove(string activityId)
    {
        lock (_lock)
        {
            _activities.RemoveAll(a => a.Id == activityId);
        }
        RaiseTopChanged();
    }

    private void RaiseTopChanged()
    {
        var top = GetTop();
        TopActivityChanged?.Invoke(this, top);
    }

    private ActivityModel? GetTop()
    {
        lock (_lock)
        {
            return _activities
                .OrderByDescending(a => a.Priority)
                .ThenByDescending(a => a.CreatedAt)
                .FirstOrDefault();
        }
    }
}
