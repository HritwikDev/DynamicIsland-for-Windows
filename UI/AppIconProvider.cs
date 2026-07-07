using System;
using System.Drawing;
using System.IO;

namespace DynamicIsland.UI;

/// <summary>Loads the app's icon.ico (copied next to the exe via the .csproj), with a safe system-icon fallback if it's ever missing.</summary>
public static class AppIconProvider
{
    private static Icon? _cached;

    public static Icon Load()
    {
        if (_cached is not null) return _cached;

        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico");
            if (File.Exists(iconPath))
            {
                _cached = new Icon(iconPath);
                return _cached;
            }
        }
        catch
        {
            // Falls through to the system default below — a missing/bad
            // icon file should never prevent the app from starting.
        }

        _cached = SystemIcons.Application;
        return _cached;
    }
}
