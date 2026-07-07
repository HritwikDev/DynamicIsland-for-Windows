using System.Drawing;
using System.Windows.Forms;
using DynamicIsland.Core.Models;
using DynamicIsland.Core.Services;

namespace DynamicIsland.UI.Widgets;

/// <summary>Phase 7 — Clipboard Manager UI. Click any past entry to re-copy it.</summary>
public sealed class ClipboardWidget : UserControl
{
    private readonly IClipboardManager _clipboardManager;
    private readonly ListBox _historyList;

    public ClipboardWidget(IClipboardManager clipboardManager)
    {
        _clipboardManager = clipboardManager;
        Padding = new Padding(14, 20, 12, 8);

        var header = new Label { Text = "Clipboard History", ForeColor = Color.Silver, Font = new Font("Segoe UI", 8f, FontStyle.Bold), Dock = DockStyle.Top, AutoSize = true };
        _historyList = new ListBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.White, BorderStyle = BorderStyle.None };
        _historyList.DisplayMember = nameof(ClipboardEntry.Preview);
        _historyList.DoubleClick += OnEntryDoubleClicked;

        Controls.Add(_historyList);
        Controls.Add(header);

        // Paired with HandleCreated/HandleDestroyed rather than subscribed
        // once in the constructor: IClipboardManager is a singleton but
        // this widget is transient, so an unpaired subscribe would leave a
        // stale handler (referencing a disposed widget) registered every
        // time you scroll back to Clipboard.
        HandleCreated += async (_, _) =>
        {
            _clipboardManager.EntryAdded += OnEntryAdded;
            _clipboardManager.StartListening();
            await RefreshAsync();
        };
        HandleDestroyed += (_, _) =>
        {
            _clipboardManager.EntryAdded -= OnEntryAdded;
            _clipboardManager.StopListening();
        };
    }

    private async void OnEntryAdded(object? sender, ClipboardEntry e) => await RefreshAsync();

    private async System.Threading.Tasks.Task RefreshAsync()
    {
        var history = await _clipboardManager.GetHistoryAsync();
        if (InvokeRequired) { BeginInvoke(() => Populate(history)); return; }
        Populate(history);
    }

    private void Populate(System.Collections.Generic.IReadOnlyList<ClipboardEntry> history)
    {
        _historyList.Items.Clear();
        foreach (var entry in history) _historyList.Items.Add(entry);
    }

    private void OnEntryDoubleClicked(object? sender, System.EventArgs e)
    {
        if (_historyList.SelectedItem is ClipboardEntry entry)
        {
            _clipboardManager.CopyToClipboard(entry);
        }
    }
}
