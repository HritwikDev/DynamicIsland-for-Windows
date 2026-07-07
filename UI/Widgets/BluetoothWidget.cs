using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DynamicIsland.Core.Models;
using DynamicIsland.Core.Services;

namespace DynamicIsland.UI.Widgets;

/// <summary>Phase 8 — Bluetooth Widget UI. Also pushes a transient Activity on connect.</summary>
public sealed class BluetoothWidget : UserControl
{
    private readonly IBluetoothService _bluetoothService;
    private readonly IActivityManager _activityManager;
    private readonly ListBox _devicesList;

    public BluetoothWidget(IBluetoothService bluetoothService, IActivityManager activityManager)
    {
        _bluetoothService = bluetoothService;
        _activityManager = activityManager;
        Padding = new Padding(14, 20, 12, 8);

        var header = new Label { Text = "Bluetooth", ForeColor = Color.Silver, Font = new Font("Segoe UI", 8f, FontStyle.Bold), Dock = DockStyle.Top, AutoSize = true };
        _devicesList = new ListBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.White, BorderStyle = BorderStyle.None };

        Controls.Add(_devicesList);
        Controls.Add(header);

        // Paired with HandleCreated/HandleDestroyed — see MediaWidget/
        // ClipboardWidget for why an unpaired subscribe on a singleton
        // service leaks a stale handler every time this transient widget
        // is recreated.
        HandleCreated += async (_, _) =>
        {
            _bluetoothService.DevicesChanged += OnDevicesChanged;
            _bluetoothService.StartWatching();
            await RefreshAsync();
        };
        HandleDestroyed += (_, _) =>
        {
            _bluetoothService.DevicesChanged -= OnDevicesChanged;
            _bluetoothService.StopWatching();
        };
    }

    private async void OnDevicesChanged(object? sender, EventArgs e) => await RefreshAsync();

    private async System.Threading.Tasks.Task RefreshAsync()
    {
        var devices = await _bluetoothService.GetPairedDevicesAsync();
        if (InvokeRequired) { BeginInvoke(() => Populate(devices)); return; }
        Populate(devices);
    }

    private void Populate(System.Collections.Generic.IReadOnlyList<BluetoothDeviceInfo> devices)
    {
        _devicesList.Items.Clear();
        foreach (var device in devices)
        {
            var battery = device.BatteryPercent.HasValue ? $"{device.BatteryPercent}%" : "—";
            _devicesList.Items.Add($"{device.Name} — {(device.IsConnected ? "Connected" : "Paired")} ({battery})");
        }

        var justConnected = devices.FirstOrDefault(d => d.IsConnected);
        if (justConnected is not null)
        {
            _activityManager.Push(new ActivityModel
            {
                Id = "bluetooth-connected",
                Kind = ActivityKind.Bluetooth,
                Title = justConnected.Name,
                Subtitle = "Connected",
                Priority = 5,
                ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(4)
            });
        }
    }
}
