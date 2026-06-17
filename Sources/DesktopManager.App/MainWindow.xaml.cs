using DesktopManager.App.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace DesktopManager.App;

/// <summary>
/// Main DesktopManager hotkey configuration window.
/// </summary>
public sealed partial class MainWindow : Window {
    private readonly string _profilePath;
    private readonly HotkeyProfileRuntime _runtime = new();
    private readonly ObservableCollection<RuntimeLogEntry> _logEntries = new();
    private readonly ObservableCollection<MonitorAdvancedColorOption> _advancedColorOptions = new();
    private readonly List<MonitorOption> _monitorOptions = new();
    private readonly global::DesktopManager.Monitors _monitors = new();
    private HotkeyProfile _profile;
    private bool _loadingProfile = true;

    /// <summary>
    /// Initializes the main hotkey window and loads the first profile.
    /// </summary>
    public MainWindow() {
        InitializeComponent();

        Title = "DesktopManager";
        _profilePath = HotkeyProfileStore.GetDefaultProfilePath();
        ActionLogList.ItemsSource = _logEntries;
        AdvancedColorList.ItemsSource = _advancedColorOptions;
        _runtime.StatusChanged += Runtime_StatusChanged;
        Closed += MainWindow_Closed;
        InitializeTray();
        RefreshMonitorOptions();
        RefreshAdvancedColorStatus();
        _profile = HotkeyProfileStore.LoadOrCreate(_profilePath);
        LoadProfileIntoView();
        StartRuntime();
    }

    private void ReloadButton_Click(object sender, RoutedEventArgs e) {
        _profile = HotkeyProfileStore.LoadOrCreate(_profilePath);
        LoadProfileIntoView();
        StartRuntime();
    }

    private void EnabledSwitch_Toggled(object sender, RoutedEventArgs e) {
        if (_loadingProfile) {
            return;
        }

        _profile.Enabled = EnabledSwitch.IsOn;
        SaveProfile();
        StartRuntime();
    }

    private void StartWithWindowsSwitch_Toggled(object sender, RoutedEventArgs e) {
        if (_loadingProfile) {
            return;
        }

        _profile.StartWithWindows = StartWithWindowsSwitch.IsOn;
        StartupRegistrationService.SetEnabled(_profile.StartWithWindows);
        SaveProfile();
        AddLog(_profile.StartWithWindows ? "Startup registration enabled." : "Startup registration disabled.");
    }

    private void MinimizeToTraySwitch_Toggled(object sender, RoutedEventArgs e) {
        if (_loadingProfile) {
            return;
        }

        _profile.MinimizeToTray = MinimizeToTraySwitch.IsOn;
        SaveProfile();
        AddLog(_profile.MinimizeToTray ? "Close-to-tray enabled." : "Close-to-tray disabled.");
    }

    private void FunctionsList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
        ShowSelectedFunction(FunctionsList.SelectedItem as HotkeyFunctionDefinition);
    }

    private void TestActionButton_Click(object sender, RoutedEventArgs e) {
        if (FunctionsList.SelectedItem is not HotkeyFunctionDefinition function) {
            AddLog("No function selected.");
            return;
        }

        _runtime.Execute(function, _windowHandle);
    }

    private void SaveActionButton_Click(object sender, RoutedEventArgs e) {
        if (FunctionsList.SelectedItem is not HotkeyFunctionDefinition function) {
            AddLog("No function selected.");
            return;
        }

        function.WindowAction.MonitorIndex = (MonitorIndexComboBox.SelectedItem as MonitorOption)?.Index;
        function.WindowAction.VerifyAfterAction = SelectedVerifySwitch.IsOn;
        SaveProfile();
        LoadProfileIntoView();
        FunctionsList.SelectedItem = function;
        StartRuntime();
        AddLog($"Saved {function.Name}.");
    }

    private void RefreshHdrButton_Click(object sender, RoutedEventArgs e) {
        RefreshAdvancedColorStatus();
    }

    private void EnableHdrButton_Click(object sender, RoutedEventArgs e) {
        SetSelectedHdr(enabled: true);
    }

    private void DisableHdrButton_Click(object sender, RoutedEventArgs e) {
        SetSelectedHdr(enabled: false);
    }

    private void LoadProfileIntoView() {
        HotkeyProfileValidationResult validation = HotkeyProfileValidator.Validate(_profile);
        List<HotkeyFunctionDefinition> functions = _profile.Functions ?? new List<HotkeyFunctionDefinition>();
        _loadingProfile = true;
        try {
            EnabledSwitch.IsOn = _profile.Enabled;
            StartWithWindowsSwitch.IsOn = _profile.StartWithWindows && StartupRegistrationService.IsEnabled();
            MinimizeToTraySwitch.IsOn = _profile.MinimizeToTray;
            ProfilePathText.Text = $"{_profile.ProfileName} profile - {_profilePath}";
            FunctionCountText.Text = $"{functions.Count} function(s) loaded";
            FunctionsList.ItemsSource = functions;
            FunctionsList.SelectedIndex = functions.Count > 0 ? 0 : -1;
        } finally {
            _loadingProfile = false;
        }

        ValidationInfo.IsOpen = !validation.IsValid;
        ValidationInfo.Message = validation.IsValid ? string.Empty : string.Join(" ", validation.Errors);
    }

    private void StartRuntime() {
        HotkeyProfileValidationResult validation = HotkeyProfileValidator.Validate(_profile);
        if (!validation.IsValid) {
            RuntimeStatusText.Text = "Runtime not started: profile has validation errors.";
            _runtime.Stop();
            return;
        }

        try {
            _runtime.Start(_profile);
            RuntimeStatusText.Text = $"Runtime active: {_runtime.RegisteredCount} hotkey(s) registered.";
        } catch (Exception ex) {
            RuntimeStatusText.Text = $"Runtime failed: {ex.Message}";
        }
    }

    private void ShowSelectedFunction(HotkeyFunctionDefinition? function) {
        if (function == null) {
            SelectedNameText.Text = "No function selected";
            SelectedHotkeyText.Text = string.Empty;
            SelectedTargetText.Text = string.Empty;
            SelectedMonitorText.Text = string.Empty;
            SelectedPlacementText.Text = string.Empty;
            SelectedVerifySwitch.IsOn = false;
            MonitorIndexComboBox.SelectedIndex = 0;
            TestActionButton.IsEnabled = false;
            SaveActionButton.IsEnabled = false;
            return;
        }

        TestActionButton.IsEnabled = true;
        SaveActionButton.IsEnabled = true;
        SelectedNameText.Text = function.Name;
        SelectedHotkeyText.Text = function.Hotkey;
        SelectedTargetText.Text = function.WindowAction.Target;
        SelectedMonitorText.Text = FormatMonitorText(function.WindowAction);
        SelectedPlacementText.Text = function.WindowAction.Placement;
        SelectedVerifySwitch.IsOn = function.WindowAction.VerifyAfterAction;
        SelectMonitorOption(function.WindowAction.MonitorIndex);
    }

    private void Runtime_StatusChanged(object? sender, string message) {
        DispatcherQueue.TryEnqueue(() => {
            RuntimeStatusText.Text = message;
            AddLog(message);
        });
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args) {
        _runtime.Dispose();
        DisposeTray();
    }

    private void SaveProfile() {
        HotkeyProfileStore.Save(_profilePath, _profile);
    }

    private void AddLog(string message) {
        _logEntries.Insert(0, new RuntimeLogEntry(DateTimeOffset.Now, message));
        while (_logEntries.Count > 100) {
            _logEntries.RemoveAt(_logEntries.Count - 1);
        }
    }

    private void RefreshMonitorOptions() {
        _monitorOptions.Clear();
        _monitorOptions.Add(new MonitorOption(null, "Automatic"));

        foreach (global::DesktopManager.Monitor monitor in _monitors.GetMonitors(connectedOnly: true, refresh: true).OrderBy(monitor => monitor.Index)) {
            _monitorOptions.Add(new MonitorOption(
                monitor.Index,
                $"{monitor.Index}: {monitor.DeviceName} ({monitor.PositionLeft},{monitor.PositionTop})"));
        }

        MonitorIndexComboBox.ItemsSource = _monitorOptions;
        MonitorIndexComboBox.SelectedIndex = 0;
    }

    private void RefreshAdvancedColorStatus() {
        _advancedColorOptions.Clear();

        foreach (global::DesktopManager.Monitor monitor in _monitors.GetMonitors(connectedOnly: true, refresh: true).OrderBy(monitor => monitor.Index)) {
            try {
                _advancedColorOptions.Add(new MonitorAdvancedColorOption(_monitors.GetMonitorAdvancedColor(monitor.DeviceId)));
            } catch (Exception ex) {
                AddLog($"HDR query failed for monitor {monitor.Index}: {ex.Message}");
            }
        }

        if (_advancedColorOptions.Count > 0 && AdvancedColorList.SelectedIndex < 0) {
            AdvancedColorList.SelectedIndex = 0;
        }
    }

    private void SetSelectedHdr(bool enabled) {
        if (AdvancedColorList.SelectedItem is not MonitorAdvancedColorOption option) {
            AddLog("No HDR monitor selected.");
            return;
        }

        if (!option.CanToggleHdr) {
            AddLog($"Monitor {option.Index} does not report HDR support.");
            return;
        }

        try {
            _monitors.SetMonitorHdr(option.DeviceId, enabled);
            AddLog(enabled ? $"Enabled HDR on monitor {option.Index}." : $"Disabled HDR on monitor {option.Index}.");
            RefreshAdvancedColorStatus();
        } catch (Exception ex) {
            AddLog($"HDR update failed for monitor {option.Index}: {ex.Message}");
        }
    }

    private void SelectMonitorOption(int? monitorIndex) {
        MonitorOption? option = _monitorOptions.FirstOrDefault(item => item.Index == monitorIndex);
        MonitorIndexComboBox.SelectedItem = option ?? _monitorOptions[0];
    }

    private static string FormatMonitorText(WindowHotkeyActionDefinition action) {
        return action.MonitorIndex.HasValue
            ? $"{action.Monitor} (index {action.MonitorIndex.Value})"
            : action.Monitor;
    }
}
