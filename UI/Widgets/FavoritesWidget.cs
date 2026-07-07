using System;
using System.Drawing;
using System.Windows.Forms;
using DynamicIsland.Core.Models;
using DynamicIsland.Core.Services;

namespace DynamicIsland.UI.Widgets;

/// <summary>Phase 5 — Favorites. A scrollable row of pinned app/file/URL shortcuts.</summary>
public sealed class FavoritesWidget : UserControl
{
    private readonly IFavoritesService _favoritesService;
    private readonly FlowLayoutPanel _flow;

    public FavoritesWidget(IFavoritesService favoritesService)
    {
        _favoritesService = favoritesService;

        var header = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(0, 8, 0, 0) };
        var addButton = new Button
        {
            Text = "+ Add", Dock = DockStyle.Left, Width = 70,
            FlatStyle = FlatStyle.Flat, ForeColor = Color.White,
            BackColor = Color.FromArgb(42, 42, 42), Cursor = Cursors.Hand
        };
        addButton.FlatAppearance.BorderSize = 0;
        addButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 60, 60);
        addButton.Click += OnAddClicked;
        header.Controls.Add(addButton);

        _flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            AutoScroll = true,
            WrapContents = false,
            Padding = new Padding(14, 8, 12, 8)
        };

        Controls.Add(_flow);
        Controls.Add(header);

        HandleCreated += async (_, _) => await RefreshAsync();
    }

    private async void OnAddClicked(object? sender, EventArgs e)
    {
        using var dialog = new AddFavoriteDialog();
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (string.IsNullOrWhiteSpace(dialog.FavoriteName) || string.IsNullOrWhiteSpace(dialog.FavoritePath)) return;

        await _favoritesService.AddAsync(new FavoriteItem
        {
            Id = Guid.NewGuid().ToString(),
            Name = dialog.FavoriteName,
            LaunchPath = dialog.FavoritePath
        });

        await RefreshAsync();
    }

    private async System.Threading.Tasks.Task RefreshAsync()
    {
        var items = await _favoritesService.GetAllAsync();

        if (InvokeRequired) { BeginInvoke(() => Populate(items)); return; }
        Populate(items);
    }

    private void Populate(System.Collections.Generic.IReadOnlyList<FavoriteItem> items)
    {
        _flow.Controls.Clear();
        foreach (var item in items)
        {
            var button = new Button
            {
                Text = item.Name,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(42, 42, 42),
                Margin = new Padding(0, 0, 6, 0),
                Tag = item,
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 60, 60);

            button.Click += (s, _) =>
            {
                if (((Button)s!).Tag is FavoriteItem favorite) _favoritesService.Launch(favorite);
            };

            var menu = new ContextMenuStrip();
            menu.Items.Add("Remove", null, async (_, _) =>
            {
                await _favoritesService.RemoveAsync(item.Id);
                await RefreshAsync();
            });
            button.ContextMenuStrip = menu;

            _flow.Controls.Add(button);
        }
    }
}
