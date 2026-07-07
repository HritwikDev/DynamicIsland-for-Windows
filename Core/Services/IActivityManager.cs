using System;
using System.Collections.Generic;
using DynamicIsland.Core.Models;

namespace DynamicIsland.Core.Services;

/// <summary>
/// Owns the set of currently active Activities and decides which one
/// (if any) is currently "top of stack" and should render in the island.
/// Widgets (media, bluetooth, calendar, etc.) push/pop activities into
/// this manager rather than touching the UI directly.
/// </summary>
public interface IActivityManager
{
    event EventHandler<ActivityModel?>? TopActivityChanged;

    IReadOnlyList<ActivityModel> ActiveActivities { get; }

    void Push(ActivityModel activity);
    void Update(string activityId, Action<ActivityModel> mutate);
    void Remove(string activityId);
}
