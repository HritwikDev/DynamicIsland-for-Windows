using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using DynamicIsland.Core.Models;
using DynamicIsland.Core.Services;

namespace DynamicIsland.UI.Widgets;

/// <summary>
/// Phase 6 — File Tray. Uses plain WinForms drag-drop (DataFormats.FileDrop)
/// for both dropping files in and dragging them back out — no WinRT/UWP
/// StorageFile plumbing needed, which is simpler and was really only there
/// to serve the WinUI version. "Share" (a nice-to-have from the plan) was
/// dropped in this rewrite in favor of "Reveal in Explorer," since real
/// Share requires the same WinRT interop complexity we're trying to avoid.
/// </summary>
public sealed class FileTrayWidget : UserControl
{
    private readonly IFileTrayService _fileTrayService;
    private readonly FlowLayoutPanel _flow;
    private readonly Label _emptyHint;

    public FileTrayWidget(IFileTrayService fileTrayService)
    {
        _fileTrayService = fileTrayService;
        Padding = new Padding(14, 22, 12, 8);
        AllowDrop = true;

        _flow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = false, AllowDrop = true };
        _emptyHint = new Label { Text = "Drop files here", ForeColor = Color.Silver, AutoSize = true, Location = new Point(14, 22) };

        Controls.Add(_flow);
        Controls.Add(_emptyHint);

        DragEnter += OnDragEnter;
        DragDrop += OnDrop;
        _flow.DragEnter += OnDragEnter;
        _flow.DragDrop += OnDrop;

        HandleCreated += async (_, _) => await RefreshAsync();
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
        => e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true ? DragDropEffects.Copy : DragDropEffects.None;

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] paths) return;

        foreach (var path in paths)
        {
            await _fileTrayService.AddAsync(path);
        }

        await RefreshAsync();
    }

    private async System.Threading.Tasks.Task RefreshAsync()
    {
        var items = await _fileTrayService.GetAllAsync();
        if (InvokeRequired) { BeginInvoke(() => Populate(items)); return; }
        Populate(items);
    }

    private void Populate(System.Collections.Generic.IReadOnlyList<FileTrayItem> items)
    {
        _flow.Controls.Clear();
        _emptyHint.Visible = items.Count == 0;

        foreach (var item in items)
        {
            var chip = new Label
            {
                Text = item.DisplayName,
                AutoSize = true,
                BackColor = Color.FromArgb(42, 42, 42),
                ForeColor = Color.White,
                Padding = new Padding(6, 4, 6, 4),
                Margin = new Padding(0, 0, 6, 0),
                Tag = item
            };

            chip.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left && s is Label { Tag: FileTrayItem fileItem })
                {
                    // Phase 6 — drag the tray item back out to Explorer/another app.
                    chip.DoDragDrop(new string[] { fileItem.FilePath }, DragDropEffects.Copy);
                }
            };

            chip.DoubleClick += (s, _) =>
            {
                if (s is Label { Tag: FileTrayItem fileItem }) _fileTrayService.OpenWithDefaultApp(fileItem);
            };

            chip.ContextMenuStrip = BuildContextMenu(item);
            _flow.Controls.Add(chip);
        }
    }

    private ContextMenuStrip BuildContextMenu(FileTrayItem item)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => _fileTrayService.OpenWithDefaultApp(item));
        menu.Items.Add("Reveal in Explorer", null, (_, _) =>
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{item.FilePath}\"") { UseShellExecute = true }));
        menu.Items.Add("Remove", null, async (_, _) =>
        {
            await _fileTrayService.RemoveAsync(item.Id);
            await RefreshAsync();
        });
        return menu;
    }
}
