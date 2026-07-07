using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DynamicIsland.Core.Models;

namespace DynamicIsland.Core.Services;

public interface IBluetoothService : IDisposable
{
    event EventHandler? DevicesChanged;

    Task<IReadOnlyList<BluetoothDeviceInfo>> GetPairedDevicesAsync();
    void StartWatching();
    void StopWatching();
}
