using System.Drawing;
using System.Windows.Forms;

namespace DynamicIsland.UI.Widgets;

/// <summary>Small modal used by FavoritesWidget's "+ Add" button.</summary>
public sealed class AddFavoriteDialog : Form
{
    private readonly TextBox _nameBox;
    private readonly TextBox _pathBox;

    public string FavoriteName => _nameBox.Text;
    public string FavoritePath => _pathBox.Text;

    public AddFavoriteDialog()
    {
        Text = "Add Favorite";
        Size = new Size(380, 200);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var nameLabel = new Label { Text = "Name", Location = new Point(16, 16), AutoSize = true };
        _nameBox = new TextBox { Location = new Point(16, 36), Width = 330 };

        var pathLabel = new Label { Text = "App, file, or URL", Location = new Point(16, 68), AutoSize = true };
        _pathBox = new TextBox { Location = new Point(16, 88), Width = 250 };
        var browseButton = new Button { Text = "Browse…", Location = new Point(272, 87), Width = 74 };
        browseButton.Click += OnBrowseClicked;

        var okButton = new Button { Text = "Add", DialogResult = DialogResult.OK, Location = new Point(180, 130), Width = 80 };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(266, 130), Width = 80 };

        AcceptButton = okButton;
        CancelButton = cancelButton;

        Controls.AddRange(new Control[] { nameLabel, _nameBox, pathLabel, _pathBox, browseButton, okButton, cancelButton });
    }

    private void OnBrowseClicked(object? sender, System.EventArgs e)
    {
        using var dialog = new OpenFileDialog { Title = "Choose an app or file" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        _pathBox.Text = dialog.FileName;
        if (string.IsNullOrWhiteSpace(_nameBox.Text))
        {
            _nameBox.Text = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
        }
    }
}
