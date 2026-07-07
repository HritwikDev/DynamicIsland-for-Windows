using System;
using System.Drawing;
using System.Windows.Forms;

namespace DynamicIsland.UI.Widgets;

/// <summary>Small modal used by CalendarWidget's "+ Add Event" button.</summary>
public sealed class AddEventDialog : Form
{
    private readonly TextBox _titleBox;
    private readonly DateTimePicker _dateTimePicker;

    public string EventTitle => _titleBox.Text;
    public DateTime EventStartsAt => _dateTimePicker.Value;

    public AddEventDialog()
    {
        Text = "Add Calendar Event";
        Size = new Size(340, 180);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var titleLabel = new Label { Text = "Title", Location = new Point(16, 16), AutoSize = true };
        _titleBox = new TextBox { Location = new Point(16, 36), Width = 290 };

        var dateLabel = new Label { Text = "When", Location = new Point(16, 68), AutoSize = true };
        _dateTimePicker = new DateTimePicker
        {
            Location = new Point(16, 88),
            Width = 290,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "MMM d, yyyy  h:mm tt",
            Value = DateTime.Now.AddHours(1)
        };

        var okButton = new Button { Text = "Add", DialogResult = DialogResult.OK, Location = new Point(140, 124), Width = 80 };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(226, 124), Width = 80 };

        AcceptButton = okButton;
        CancelButton = cancelButton;

        Controls.AddRange(new Control[] { titleLabel, _titleBox, dateLabel, _dateTimePicker, okButton, cancelButton });
    }
}
