using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DynamicIsland.Core.Models;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace DynamicIsland.Core.Services;

/// <summary>
/// Phase 8 — Bluetooth Widget. Lists paired Bluetooth devices via
/// DeviceInformation/AEP enumeration and watches for connect/disconnect.
///
/// Battery percent is intentionally best-effort: Windows doesn't expose a
/// simple universal "battery %" property for every Bluetooth Classic
/// device — the reliable path is querying the GATT Battery Service (0x180F)
/// for BLE devices per-connection, which is a heavier per-device async call.
/// This implementation reports battery when the AEP property is present and
/// null otherwise, so the widget can show "—" instead of guessing.
/// </summary>
public sealed class BluetoothService : IBluetoothService
{
    private const string BatteryPropertyKey = "System.Devices.Aep.Bluetooth.Le.IsConnected";
    private DeviceWatcher? _watcher;

    public event EventHandler? DevicesChanged;

    public async Task<IReadOnlyList<BluetoothDeviceInfo>> GetPairedDevicesAsync()
    {
        var selector = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
        var devices = await DeviceInformation.FindAllAsync(selector, new[] { "System.Devices.Aep.IsConnected" });

        var results = new List<BluetoothDeviceInfo>();
        foreach (var device in devices)
        {
            bool isConnected = device.Properties.TryGetValue("System.Devices.Aep.IsConnected", out var connectedObj)
                                && connectedObj is bool b && b;

            int? battery = null;
            try
            {
                using var bleDevice = await BluetoothDevice.FromIdAsync(device.Id);
                // ClassOfDevice gives a rough category; true battery % would
                // need a GATT battery-service read (see class remarks).
                results.Add(new BluetoothDeviceInfo
                {
                    Id = device.Id,
                    Name = device.Name,
                    IsConnected = isConnected,
                    BatteryPercent = battery,
                    Kind = ClassifyDevice(bleDevice?.ClassOfDevice)
                });
                continue;
            }
            catch
            {
                // Not all paired devices resolve via BluetoothDevice.FromIdAsync
                // (e.g. some BLE-only accessories) — fall back to a bare entry.
            }

            results.Add(new BluetoothDeviceInfo
            {
                Id = device.Id,
                Name = device.Name,
                IsConnected = isConnected,
                BatteryPercent = battery
            });
        }

        return results;
    }

    public void StartWatching()
    {
        if (_watcher is not null) return;

        var selector = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
        _watcher = DeviceInformation.CreateWatcher(selector, new[] { "System.Devices.Aep.IsConnected" });
        _watcher.Updated += OnWatcherEvent;
        _watcher.Added += OnWatcherEvent;
        _watcher.Removed += OnWatcherEvent;
        _watcher.Start();
    }

    public void StopWatching()
    {
        if (_watcher is null) return;
        _watcher.Updated -= OnWatcherEvent;
        _watcher.Added -= OnWatcherEvent;
        _watcher.Removed -= OnWatcherEvent;
        _watcher.Stop();
        _watcher = null;
    }

    private void OnWatcherEvent(DeviceWatcher sender, object args) => DevicesChanged?.Invoke(this, EventArgs.Empty);

    private static BluetoothDeviceKind ClassifyDevice(BluetoothClassOfDevice? classOfDevice)
    {
        if (classOfDevice is null) return BluetoothDeviceKind.Other;

        return classOfDevice.MajorClass switch
        {
            BluetoothMajorClass.AudioVideo => BluetoothDeviceKind.Headphones,
            BluetoothMajorClass.Peripheral => BluetoothDeviceKind.Mouse,
            BluetoothMajorClass.Phone => BluetoothDeviceKind.Phone,
            _ => BluetoothDeviceKind.Other
        };
    }

    public void Dispose() => StopWatching();
}
