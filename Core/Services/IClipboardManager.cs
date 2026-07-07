using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DynamicIsland.Core.Models;

namespace DynamicIsland.Core.Services;

public interface IClipboardManager
{
    event EventHandler<ClipboardEntry>? EntryAdded;

    void StartListening();
    void StopListening();
    Task<IReadOnlyList<ClipboardEntry>> GetHistoryAsync(int maxCount = 50);
    Task ClearAsync();
    void CopyToClipboard(ClipboardEntry entry);
}
