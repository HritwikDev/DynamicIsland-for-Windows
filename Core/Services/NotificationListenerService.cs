using System;
using System.Threading.Tasks;
using DynamicIsland.Core.Models;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace DynamicIsland.Core.Services;

/// <summary>
/// Phase 12 — Notifications.
///
/// IMPORTANT: <see cref="UserNotificationListener"/> requires the app to
/// have a package identity (MSIX) and the "User Notification Listener"
/// restricted capability declared + user consent granted in Settings.
/// This project is currently configured unpackaged (see csproj comments in
/// Phase 1) for easier always-on-top window control, which means this
/// service will throw at RequestAccessAsync() until the app is packaged.
///
/// Two ways forward:
///   1. Package this app as MSIX for this feature only (bigger change), or
///   2. Skip live notification mirroring and only show island-native
///      activities (media/calendar/bluetooth/etc.) which don't need it.
///
/// The implementation below is written correctly against the real API so
/// it's ready to use the moment packaging is added; it fails soft
/// (IsAvailable = false) rather than crashing the app when it can't.
/// </summary>
public sealed class NotificationListenerService
{
    private readonly IActivityManager _activityManager;
    private UserNotificationListener? _listener;

    public bool IsAvailable { get; private set; }

    public NotificationListenerService(IActivityManager activityManager) => _activityManager = activityManager;

    public async Task<bool> TryInitializeAsync()
    {
        try
        {
            _listener = UserNotificationListener.Current;
            var accessStatus = await _listener.RequestAccessAsync();
            IsAvailable = accessStatus == UserNotificationListenerAccessStatus.Allowed;

            if (IsAvailable)
            {
                _listener.NotificationChanged += OnNotificationChanged;
            }
        }
        catch
        {
            // Expected pre-MSIX-packaging — see class remarks.
            IsAvailable = false;
        }

        return IsAvailable;
    }

    private void OnNotificationChanged(UserNotificationListener sender, UserNotificationChangedEventArgs args)
    {
        if (args.ChangeKind != UserNotificationChangedKind.Added) return;

        try
        {
            var notification = sender.GetNotification(args.UserNotificationId);
            if (notification is null) return;

            var binding = notification.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric);
            var texts = binding?.GetTextElements();
            var title = texts?.Count > 0 ? texts[0].Text : notification.AppInfo.DisplayInfo.DisplayName;
            var body = texts?.Count > 1 ? texts[1].Text : "";

            _activityManager.Push(new ActivityModel
            {
                Id = $"notification-{args.UserNotificationId}",
                Kind = ActivityKind.Notification,
                Title = title,
                Subtitle = body,
                Priority = 6,
                ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(6)
            });
        }
        catch
        {
            // Individual notification could have been dismissed already.
        }
    }
}
