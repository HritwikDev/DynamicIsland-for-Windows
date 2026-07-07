using System.Drawing;
using System.Windows.Forms;
using DynamicIsland.Core.Services;
using DynamicIsland.Core.Storage;

namespace DynamicIsland.UI.Settings;

/// <summary>Phase 14 — Settings. A plain window (not part of the island shell) for preferences.</summary>
public sealed class SettingsForm : Form
{
    private readonly ISettingsService _settingsService;
    private readonly IUpdateService _updateService;
    private bool _isLoading = true;

    private readonly CheckBox _launchOnStartupCheck;
    private readonly CheckBox _showOnAllMonitorsCheck;
    private readonly CheckBox _mediaWidgetCheck;
    private readonly CheckBox _calendarWidgetCheck;
    private readonly CheckBox _bluetoothWidgetCheck;
    private readonly CheckBox _clipboardWidgetCheck;
    private readonly TrackBar _opacitySlider;

    public SettingsForm(ISettingsService settingsService, IUpdateService updateService)
    {
        _settingsService = settingsService;
        _updateService = updateService;

        Text = "Dynamic Island Settings";
        Size = new Size(420, 460);
        StartPosition = FormStartPosition.CenterScreen;
        Icon = AppIconProvider.Load();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(20),
            AutoScroll = true
        };

        _launchOnStartupCheck = new CheckBox { Text = "Launch on startup", AutoSize = true };
        _showOnAllMonitorsCheck = new CheckBox { Text = "Show on all monitors", AutoSize = true };

        var widgetsHeader = new Label { Text = "Widgets", Font = new Font("Segoe UI", 10f, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 16, 0, 4) };
        _mediaWidgetCheck = new CheckBox { Text = "Media controller", AutoSize = true };
        _calendarWidgetCheck = new CheckBox { Text = "Calendar", AutoSize = true };
        _bluetoothWidgetCheck = new CheckBox { Text = "Bluetooth", AutoSize = true };
        _clipboardWidgetCheck = new CheckBox { Text = "Clipboard manager", AutoSize = true };

        var appearanceHeader = new Label { Text = "Appearance", Font = new Font("Segoe UI", 10f, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 16, 0, 4) };
        var opacityLabel = new Label { Text = "Island opacity", AutoSize = true };
        _opacitySlider = new TrackBar { Minimum = 50, Maximum = 100, Width = 300, TickFrequency = 10 };

        var checkForUpdatesButton = new Button { Text = "Check for updates", AutoSize = true, Margin = new Padding(0, 20, 0, 0) };
        checkForUpdatesButton.Click += async (_, _) => await _updateService.CheckAndApplyUpdatesOnStartupAsync();

        layout.Controls.AddRange(new Control[]
        {
            _launchOnStartupCheck, _showOnAllMonitorsCheck,
            widgetsHeader, _mediaWidgetCheck, _calendarWidgetCheck, _bluetoothWidgetCheck, _clipboardWidgetCheck,
            appearanceHeader, opacityLabel, _opacitySlider,
            checkForUpdatesButton
        });

        Controls.Add(layout);

        foreach (var check in new[] { _launchOnStartupCheck, _showOnAllMonitorsCheck, _mediaWidgetCheck, _calendarWidgetCheck, _bluetoothWidgetCheck, _clipboardWidgetCheck })
        {
            check.CheckedChanged += async (_, _) => await OnAnyChangedAsync();
        }
        _opacitySlider.ValueChanged += async (_, _) => await OnAnyChangedAsync();

        LoadFromSettings();
        _isLoading = false;
    }

    private void LoadFromSettings()
    {
        var s = _settingsService.Current;
        _launchOnStartupCheck.Checked = s.LaunchOnStartup;
        _showOnAllMonitorsCheck.Checked = s.ShowOnAllMonitors;
        _mediaWidgetCheck.Checked = s.EnableMediaWidget;
        _calendarWidgetCheck.Checked = s.EnableCalendarWidget;
        _bluetoothWidgetCheck.Checked = s.EnableBluetoothWidget;
        _clipboardWidgetCheck.Checked = s.EnableClipboardManager;
        _opacitySlider.Value = (int)(s.IslandOpacity * 100);
    }

    private async System.Threading.Tasks.Task OnAnyChangedAsync()
    {
        if (_isLoading) return;

        var s = _settingsService.Current;
        s.LaunchOnStartup = _launchOnStartupCheck.Checked;
        s.ShowOnAllMonitors = _showOnAllMonitorsCheck.Checked;
        s.EnableMediaWidget = _mediaWidgetCheck.Checked;
        s.EnableCalendarWidget = _calendarWidgetCheck.Checked;
        s.EnableBluetoothWidget = _bluetoothWidgetCheck.Checked;
        s.EnableClipboardManager = _clipboardWidgetCheck.Checked;
        s.IslandOpacity = _opacitySlider.Value / 100.0;

        await _settingsService.SaveAsync();
    }
}
