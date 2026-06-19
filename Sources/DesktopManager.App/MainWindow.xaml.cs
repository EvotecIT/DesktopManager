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
    private readonly ObservableCollection<WindowRuleOption> _ruleOptions = new();
    private readonly List<MonitorOption> _monitorOptions = new();
    private readonly WindowLayoutRuleExecutor _layoutRuleExecutor = new();
    private readonly string[] _placementOptions = {
        WindowPlacements.Restore,
        WindowPlacements.LeftHalf,
        WindowPlacements.RightHalf,
        WindowPlacements.Maximize,
        WindowPlacements.ExactRectangle
    };
    private readonly global::DesktopManager.Monitors _monitors = new();
    private HotkeyProfile _profile = HotkeyProfileDefaults.CreateDefaultProfile();
    private string? _profileLoadError;
    private bool _loadingProfile = true;
    private bool _startupRulesApplied;

    /// <summary>
    /// Initializes the main hotkey window and loads the first profile.
    /// </summary>
    public MainWindow() {
        InitializeComponent();

        Title = "DesktopManager";
        _profilePath = HotkeyProfileStore.GetDefaultProfilePath();
        ActionLogList.ItemsSource = _logEntries;
        AdvancedColorList.ItemsSource = _advancedColorOptions;
        RulesList.ItemsSource = _ruleOptions;
        SelectedPlacementComboBox.ItemsSource = _placementOptions;
        _runtime.StatusChanged += Runtime_StatusChanged;
        Closed += MainWindow_Closed;
        InitializeTray();
        RefreshMonitorOptions();
        RefreshAdvancedColorStatus();
        LoadProfileRecoverably();
        LoadProfileIntoView();
        StartRuntime();
    }

    private void ReloadButton_Click(object sender, RoutedEventArgs e) {
        LoadProfileRecoverably();
        LoadProfileIntoView();
        StartRuntime();
    }

    private void NewActionButton_Click(object sender, RoutedEventArgs e) {
        if (TryBlockRecoveryProfileEdit()) {
            return;
        }

        _profile.Functions ??= new List<HotkeyFunctionDefinition>();
        HotkeyFunctionDefinition function = HotkeyProfileEditor.CreateCustomWindowAction(_profile.Functions);
        _profile.Functions.Add(function);
        SaveProfile();
        LoadProfileIntoView();
        FunctionsList.SelectedItem = function;
        AddLog("Added new disabled window action.");
    }

    private void EnabledSwitch_Toggled(object sender, RoutedEventArgs e) {
        if (_loadingProfile) {
            return;
        }

        if (TryBlockRecoveryProfileEdit()) {
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

        if (TryBlockRecoveryProfileEdit()) {
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

        if (TryBlockRecoveryProfileEdit()) {
            return;
        }

        _profile.MinimizeToTray = MinimizeToTraySwitch.IsOn;
        SaveProfile();
        AddLog(_profile.MinimizeToTray ? "Close-to-tray enabled." : "Close-to-tray disabled.");
    }

    private void ApplyRulesOnStartupSwitch_Toggled(object sender, RoutedEventArgs e) {
        if (_loadingProfile) {
            return;
        }

        if (TryBlockRecoveryProfileEdit()) {
            return;
        }

        _profile.ApplyRulesOnStartup = ApplyRulesOnStartupSwitch.IsOn;
        SaveProfile();
        AddLog(_profile.ApplyRulesOnStartup ? "Layout rules will apply at startup." : "Layout rules are manual.");
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

    private void DiagnoseHotkeyButton_Click(object sender, RoutedEventArgs e) {
        if (FunctionsList.SelectedItem is not HotkeyFunctionDefinition function) {
            AddLog("No function selected.");
            return;
        }

        ShowHotkeyDiagnostic(function, addLog: true);
    }

    private void ApplyRulesButton_Click(object sender, RoutedEventArgs e) {
        ApplyLayoutRules(addDetailsToLog: true);
    }

    private void SaveActionButton_Click(object sender, RoutedEventArgs e) {
        if (TryBlockRecoveryProfileEdit()) {
            return;
        }

        if (FunctionsList.SelectedItem is not HotkeyFunctionDefinition function) {
            AddLog("No function selected.");
            return;
        }

        SaveSelectedFunctionEdits(function);
        SaveProfile();
        LoadProfileIntoView();
        FunctionsList.SelectedItem = function;
        StartRuntime();
        AddLog($"Saved {function.Name}.");
    }

    private void DeleteActionButton_Click(object sender, RoutedEventArgs e) {
        if (TryBlockRecoveryProfileEdit()) {
            return;
        }

        if (FunctionsList.SelectedItem is not HotkeyFunctionDefinition function) {
            AddLog("No function selected.");
            return;
        }

        _profile.Functions.Remove(function);
        SaveProfile();
        LoadProfileIntoView();
        StartRuntime();
        AddLog($"Deleted {function.Name}.");
    }

    private void AddRuleButton_Click(object sender, RoutedEventArgs e) {
        if (TryBlockRecoveryProfileEdit()) {
            return;
        }

        if (FunctionsList.SelectedItem is not HotkeyFunctionDefinition function || function.WindowAction == null) {
            AddLog("No window action selected.");
            return;
        }

        SaveSelectedFunctionEdits(function);
        WindowLayoutProfileDefinition layout = GetOrCreateDefaultLayout();
        WindowRuleDefinition rule = HotkeyProfileEditor.CreateRuleFromFunction(
            function,
            RuleTitlePatternBox.Text,
            RuleProcessPatternBox.Text,
            layout.Rules);

        layout.Rules.Add(rule);
        SaveProfile();
        LoadProfileIntoView();
        FunctionsList.SelectedItem = function;
        AddLog($"Added rule '{rule.Name}' to {layout.Name}.");
    }

    private void ToggleRuleButton_Click(object sender, RoutedEventArgs e) {
        if (TryBlockRecoveryProfileEdit()) {
            return;
        }

        if (!TryGetSelectedRule(out WindowRuleDefinition? rule, out _) || rule == null) {
            AddLog("No layout rule selected.");
            return;
        }

        rule.Enabled = !rule.Enabled;
        SaveProfile();
        RefreshRuleOptions(rule.Id);
        AddLog(rule.Enabled ? $"Enabled rule '{rule.Name}'." : $"Disabled rule '{rule.Name}'.");
    }

    private void DeleteRuleButton_Click(object sender, RoutedEventArgs e) {
        if (TryBlockRecoveryProfileEdit()) {
            return;
        }

        if (!TryGetSelectedRule(out WindowRuleDefinition? rule, out WindowLayoutProfileDefinition? layout) ||
            rule == null ||
            layout?.Rules == null) {
            AddLog("No layout rule selected.");
            return;
        }

        layout.Rules.Remove(rule);
        SaveProfile();
        LoadProfileIntoView();
        AddLog($"Deleted rule '{rule.Name}'.");
    }

    private void SaveSelectedFunctionEdits(HotkeyFunctionDefinition function) {
        if (function.WindowAction == null) {
            throw new InvalidOperationException("Selected function does not have a window action.");
        }

        function.Name = SelectedNameText.Text.Trim();
        if (MonitorIndexComboBox.SelectedItem is MonitorOption selectedMonitor) {
            function.WindowAction.MonitorIndex = selectedMonitor.Index;
        }

        function.Enabled = SelectedActionEnabledSwitch.IsOn;
        function.Hotkey = SelectedHotkeyBox.Text.Trim();
        if (SelectedPlacementComboBox.SelectedItem is string selectedPlacement) {
            function.WindowAction.Placement = selectedPlacement;
        }

        function.WindowAction.VerifyAfterAction = SelectedVerifySwitch.IsOn;
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
            ApplyRulesOnStartupSwitch.IsOn = _profile.ApplyRulesOnStartup;
            ProfilePathText.Text = $"{_profile.ProfileName} profile - {_profilePath}";
            FunctionCountText.Text = FormatProfileCountText(functions.Count);
            FunctionsList.ItemsSource = functions;
            FunctionsList.SelectedIndex = functions.Count > 0 ? 0 : -1;
            RefreshRuleOptions();
        } finally {
            _loadingProfile = false;
        }

        List<string> validationMessages = validation.Errors.ToList();
        if (!string.IsNullOrWhiteSpace(_profileLoadError)) {
            validationMessages.Insert(0, _profileLoadError);
        }

        ValidationInfo.IsOpen = validationMessages.Count > 0;
        ValidationInfo.Message = string.Join(" ", validationMessages);
    }

    private void StartRuntime() {
        HotkeyProfileValidationResult validation = HotkeyProfileValidator.Validate(_profile);
        if (!string.IsNullOrWhiteSpace(_profileLoadError)) {
            RuntimeStatusText.Text = "Runtime not started: profile could not be loaded.";
            _runtime.Stop();
            UpdateTrayTooltip();
            return;
        }

        if (!validation.IsValid) {
            RuntimeStatusText.Text = "Runtime not started: profile has validation errors.";
            _runtime.Stop();
            UpdateTrayTooltip();
            return;
        }

        try {
            _runtime.Start(_profile);
            RuntimeStatusText.Text = $"Runtime active: {_runtime.RegisteredCount} hotkey(s) registered.";
            UpdateTrayTooltip();
            if (_profile.ApplyRulesOnStartup && !_startupRulesApplied) {
                _startupRulesApplied = true;
                ApplyLayoutRules(addDetailsToLog: false);
            }
        } catch (Exception ex) {
            RuntimeStatusText.Text = $"Runtime failed: {ex.Message}";
            UpdateTrayTooltip();
        }
    }

    private void LoadProfileRecoverably() {
        try {
            _profile = HotkeyProfileStore.LoadOrCreate(_profilePath);
            _profileLoadError = null;
        } catch (Exception ex) {
            _profile = HotkeyProfileDefaults.CreateDefaultProfile();
            _profile.Enabled = false;
            _profile.ProfileName = "Recovery";
            _profileLoadError = $"Could not load hotkey profile. Fix or replace '{_profilePath}', then reload. {ex.Message}";
            AddLog(_profileLoadError);
        }
    }

    private bool TryBlockRecoveryProfileEdit() {
        if (string.IsNullOrWhiteSpace(_profileLoadError)) {
            return false;
        }

        AddLog("Profile changes are disabled until the profile file reloads successfully.");
        LoadProfileIntoView();
        StartRuntime();
        return true;
    }

    private void ShowSelectedFunction(HotkeyFunctionDefinition? function) {
        if (function == null) {
            SelectedNameText.Text = "No function selected";
            SelectedNameText.IsEnabled = false;
            SelectedActionEnabledSwitch.IsOn = false;
            SelectedActionEnabledSwitch.IsEnabled = false;
            SelectedHotkeyBox.Text = string.Empty;
            SelectedHotkeyBox.IsEnabled = false;
            SelectedTargetText.Text = string.Empty;
            SelectedMonitorText.Text = string.Empty;
            SelectedPlacementComboBox.SelectedIndex = -1;
            SelectedPlacementComboBox.IsEnabled = false;
            DiagnosticSummaryText.Text = "No diagnostic selected";
            DiagnosticDetailsText.Text = string.Empty;
            SelectedVerifySwitch.IsOn = false;
            MonitorIndexComboBox.SelectedIndex = 0;
            TestActionButton.IsEnabled = false;
            DiagnoseHotkeyButton.IsEnabled = false;
            SaveActionButton.IsEnabled = false;
            DeleteActionButton.IsEnabled = false;
            AddRuleButton.IsEnabled = false;
            RuleTitlePatternBox.Text = string.Empty;
            RuleProcessPatternBox.Text = string.Empty;
            return;
        }

        SelectedNameText.Text = function.Name;
        SelectedNameText.IsEnabled = true;
        SelectedHotkeyBox.Text = function.Hotkey;
        if (function.WindowAction == null) {
            SelectedTargetText.Text = "Invalid window action";
            SelectedMonitorText.Text = string.Empty;
            SelectedActionEnabledSwitch.IsOn = function.Enabled;
            SelectedActionEnabledSwitch.IsEnabled = true;
            SelectedHotkeyBox.Text = function.Hotkey;
            SelectedHotkeyBox.IsEnabled = true;
            SelectedPlacementComboBox.SelectedIndex = -1;
            SelectedPlacementComboBox.IsEnabled = false;
            DiagnosticSummaryText.Text = "Action is not diagnosable";
            DiagnosticDetailsText.Text = string.Empty;
            SelectedVerifySwitch.IsOn = false;
            MonitorIndexComboBox.SelectedIndex = 0;
            TestActionButton.IsEnabled = false;
            DiagnoseHotkeyButton.IsEnabled = false;
            SaveActionButton.IsEnabled = false;
            DeleteActionButton.IsEnabled = false;
            AddRuleButton.IsEnabled = false;
            RuleTitlePatternBox.Text = string.Empty;
            RuleProcessPatternBox.Text = string.Empty;
            return;
        }

        TestActionButton.IsEnabled = true;
        DiagnoseHotkeyButton.IsEnabled = true;
        SaveActionButton.IsEnabled = true;
        DeleteActionButton.IsEnabled = true;
        AddRuleButton.IsEnabled = true;
        RuleTitlePatternBox.Text = "*";
        RuleProcessPatternBox.Text = "*";
        SelectedActionEnabledSwitch.IsEnabled = true;
        SelectedActionEnabledSwitch.IsOn = function.Enabled;
        SelectedHotkeyBox.IsEnabled = true;
        SelectedHotkeyBox.Text = function.Hotkey;
        SelectedTargetText.Text = function.WindowAction.Target;
        SelectedMonitorText.Text = FormatMonitorText(function.WindowAction);
        SelectedPlacementComboBox.IsEnabled = true;
        SelectedPlacementComboBox.SelectedItem = function.WindowAction.Placement;
        SelectedVerifySwitch.IsOn = function.WindowAction.VerifyAfterAction;
        SelectMonitorOption(function.WindowAction.MonitorIndex);
        ShowHotkeyDiagnostic(function, addLog: false);
    }

    private void Runtime_StatusChanged(object? sender, string message) {
        DispatcherQueue.TryEnqueue(() => {
            RuntimeStatusText.Text = message;
            UpdateTrayTooltip();
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

    private void ApplyLayoutRules(bool addDetailsToLog) {
        try {
            WindowLayoutRuleExecutionResult result = _layoutRuleExecutor.ApplyRules(_profile.Layouts);
            AddLog(result.ToStatusMessage());
            if (addDetailsToLog) {
                foreach (string message in result.Messages.Take(10)) {
                    AddLog(message);
                }
            }
        } catch (Exception ex) {
            AddLog($"Layout rule application failed: {ex.Message}");
        }
    }

    private void ToggleHotkeysFromTray() {
        _profile.Enabled = !_profile.Enabled;
        _loadingProfile = true;
        try {
            EnabledSwitch.IsOn = _profile.Enabled;
        } finally {
            _loadingProfile = false;
        }

        SaveProfile();
        StartRuntime();
        AddLog(_profile.Enabled ? "Hotkeys enabled from tray." : "Hotkeys disabled from tray.");
    }

    private void ReloadProfileFromTray() {
        LoadProfileRecoverably();
        LoadProfileIntoView();
        StartRuntime();
        AddLog("Profile reloaded from tray.");
    }

    private void UpdateTrayTooltip() {
        UpdateTrayTooltip(RuntimeStatusSummary.FormatTrayTooltip(
            _profile.Enabled,
            _runtime.RegisteredCount,
            _profile.Layouts?.Count ?? 0,
            _profile.Layouts?.Sum(layout => layout.Rules?.Count ?? 0) ?? 0,
            _profile.ProfileName));
    }

    private string FormatProfileCountText(int functionCount) {
        int layoutCount = _profile.Layouts?.Count ?? 0;
        int ruleCount = _profile.Layouts?.Sum(layout => layout.Rules?.Count ?? 0) ?? 0;
        return $"{functionCount} function(s), {layoutCount} layout(s), {ruleCount} rule(s) loaded";
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

        foreach (global::DesktopManager.MonitorTopologyItem item in _monitors.GetMonitorTopology(refresh: true).Items) {
            _monitorOptions.Add(new MonitorOption(
                item.Monitor.Index,
                item.DisplayName,
                item.Identity.StableKey,
                item.TopologyName));
        }

        MonitorIndexComboBox.ItemsSource = _monitorOptions;
        MonitorIndexComboBox.SelectedIndex = 0;
    }

    private void RefreshRuleOptions(string? selectedRuleId = null) {
        _ruleOptions.Clear();
        if (_profile.Layouts != null) {
            foreach (WindowLayoutProfileDefinition layout in _profile.Layouts) {
                if (layout.Rules == null) {
                    continue;
                }

                foreach (WindowRuleDefinition rule in layout.Rules) {
                    _ruleOptions.Add(new WindowRuleOption(layout, rule));
                }
            }
        }

        RulesList.SelectedItem = selectedRuleId == null
            ? null
            : _ruleOptions.FirstOrDefault(rule => string.Equals(rule.RuleId, selectedRuleId, StringComparison.OrdinalIgnoreCase));
        bool hasRules = _ruleOptions.Count > 0;
        ToggleRuleButton.IsEnabled = hasRules;
        DeleteRuleButton.IsEnabled = hasRules;
    }

    private bool TryGetSelectedRule(out WindowRuleDefinition? rule, out WindowLayoutProfileDefinition? layout) {
        rule = null;
        layout = null;
        if (RulesList.SelectedItem is not WindowRuleOption selectedRule || _profile.Layouts == null) {
            return false;
        }

        foreach (WindowLayoutProfileDefinition candidateLayout in _profile.Layouts) {
            if (!string.Equals(candidateLayout.Id, selectedRule.LayoutId, StringComparison.OrdinalIgnoreCase) ||
                candidateLayout.Rules == null) {
                continue;
            }

            WindowRuleDefinition? candidateRule = candidateLayout.Rules.FirstOrDefault(item => string.Equals(item.Id, selectedRule.RuleId, StringComparison.OrdinalIgnoreCase));
            if (candidateRule != null) {
                rule = candidateRule;
                layout = candidateLayout;
                return true;
            }
        }

        return false;
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
        if (option == null && monitorIndex.HasValue) {
            option = new MonitorOption(monitorIndex.Value, $"{monitorIndex.Value}: unavailable saved monitor");
            _monitorOptions.Add(option);
            MonitorIndexComboBox.ItemsSource = null;
            MonitorIndexComboBox.ItemsSource = _monitorOptions;
        }

        MonitorIndexComboBox.SelectedItem = option ?? _monitorOptions[0];
    }

    private void ShowHotkeyDiagnostic(HotkeyFunctionDefinition function, bool addLog) {
        try {
            HotkeyDiagnosticSummary diagnostic = HotkeyDiagnosticsReader.ReadLatest(function);
            DiagnosticSummaryText.Text = diagnostic.Summary;
            DiagnosticDetailsText.Text = FormatDiagnosticDetails(diagnostic);
            if (addLog) {
                AddLog(diagnostic.Summary);
            }
        } catch (Exception ex) {
            DiagnosticSummaryText.Text = $"Diagnostic read failed: {ex.Message}";
            DiagnosticDetailsText.Text = string.Empty;
            AddLog(DiagnosticSummaryText.Text);
        }
    }

    private static string FormatDiagnosticDetails(HotkeyDiagnosticSummary diagnostic) {
        List<string> parts = new();
        if (diagnostic.Timestamp.HasValue) {
            parts.Add(diagnostic.Timestamp.Value.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        if (!string.IsNullOrWhiteSpace(diagnostic.EventName)) {
            parts.Add(diagnostic.EventName);
        }

        if (!string.IsNullOrWhiteSpace(diagnostic.Details)) {
            parts.Add(diagnostic.Details);
        }

        if (!string.IsNullOrWhiteSpace(diagnostic.Path)) {
            parts.Add(diagnostic.Path);
        }

        return string.Join(" | ", parts);
    }

    private WindowLayoutProfileDefinition GetOrCreateDefaultLayout() {
        _profile.Layouts ??= new List<WindowLayoutProfileDefinition>();
        WindowLayoutProfileDefinition? layout = _profile.Layouts.FirstOrDefault(item => string.Equals(item.Id, "default", StringComparison.OrdinalIgnoreCase));
        if (layout != null) {
            return layout;
        }

        layout = new WindowLayoutProfileDefinition {
            Id = "default",
            Name = "Default layout"
        };
        foreach (global::DesktopManager.MonitorTopologyItem item in _monitors.GetMonitorTopology(refresh: true).Items) {
            layout.MonitorStableKeys.Add(item.Identity.StableKey);
        }

        _profile.Layouts.Add(layout);
        return layout;
    }

    private static string FormatMonitorText(WindowHotkeyActionDefinition action) {
        return action.MonitorIndex.HasValue
            ? $"{action.Monitor} (index {action.MonitorIndex.Value})"
            : action.Monitor;
    }
}
