namespace DynamicIsland.Core.Models;

public enum BluetoothDeviceKind
{
    Headphones,
    Speaker,
    Mouse,
    Keyboard,
    Phone,
    Other
}

public sealed class BluetoothDeviceInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required bool IsConnected { get; init; }
    public int? BatteryPercent { get; init; }
    public BluetoothDeviceKind Kind { get; init; } = BluetoothDeviceKind.Other;
}
