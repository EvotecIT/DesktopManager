using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Text.Json;

namespace DesktopManager.App;

/// <summary>Thin app controls and timer ownership for the shared workspace recovery engine.</summary>
public sealed partial class WorkspaceRecoveryPanel : UserControl, IDisposable {
    private readonly WorkspaceRecoveryService _service = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly string _snapshotPath = WorkspaceRecoverySettings.GetSnapshotPath(App.WorkspaceDirectory);
    private WorkspaceRecoverySettings _settings = new();
    private DispatcherQueueTimer? _timer;
    private bool _busy;
    private bool _started;
    private bool _automaticCaptureReady;
    private bool _settingsFailed;

    /// <summary>Loads settings without enabling recovery or changing startup registration.</summary>
    public WorkspaceRecoveryPanel() {
        InitializeComponent();
        SnapshotPathText.Text = _snapshotPath;
        try {
            _settings = WorkspaceRecoverySettings.Load(App.WorkspaceDirectory);
            VirtualDesktopsCheck.IsChecked = _settings.IncludeVirtualDesktops;
            ExplorerFoldersCheck.IsChecked = _settings.IncludeExplorerFolders;
            ReopenExplorerCheck.IsChecked = _settings.ReopenExplorerFolders;
            ApplicationPinsCheck.IsChecked = _settings.RestoreApplicationPins;
            RestoreOnStartupCheck.IsChecked = _settings.RestoreOnStartup;
            AutoCaptureCheck.IsChecked = _settings.AutoCapture;
            RefreshSnapshotStatus();
        } catch (Exception ex) {
            _settingsFailed = true;
            SaveSettingsButton.IsEnabled = false;
            ShowStatus("Recovery settings could not be loaded. The existing file is preserved. " + ex.Message, InfoBarSeverity.Error);
        }
    }

    /// <summary>Starts the optional recovery host once, even when the tab has not been selected.</summary>
    public async void StartRuntime() {
        if (_started || _settingsFailed) { return; }
        _started = true;
        _timer = DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromMinutes(5);
        _timer.Tick += Timer_Tick;
        _timer.Start();
        if (_settings.RestoreOnStartup && File.Exists(_snapshotPath)) {
            await RestoreAsync(startup: true);
        }
    }

    private void CaptureButton_Click(object sender, RoutedEventArgs e) {
        if (_busy) { return; }
        try {
            WindowLayout layout = _service.Capture(CaptureOptions());
            // Incomplete experimental capture must not erase a previously complete snapshot.
            if (layout.CaptureWarnings.Count != 0 || layout.Windows.Count == 0) {
                ResultsText.Text = string.Join(Environment.NewLine, layout.CaptureWarnings);
                ShowStatus("Capture was incomplete. The previous snapshot was preserved.", InfoBarSeverity.Warning);
                return;
            }
            WorkspaceRecoveryService.Write(_snapshotPath, layout);
            _automaticCaptureReady = layout.Windows.Count > 0;
            RefreshSnapshotStatus();
            ShowStatus($"Captured {layout.Windows.Count} windows and {layout.Desktops.Count} virtual desktops.", InfoBarSeverity.Success);
        } catch (Exception ex) { ShowStatus(ex.Message, InfoBarSeverity.Error); }
    }

    private void PreviewButton_Click(object sender, RoutedEventArgs e) {
        if (_busy) { return; }
        try {
            WindowLayoutPlan plan = _service.Plan(WorkspaceRecoveryService.Read(_snapshotPath), VirtualDesktopsCheck.IsChecked == true);
            ResultsText.Text = string.Join(Environment.NewLine, plan.Windows.Select(window => $"{window.Status}: {window.SavedWindow.Title}").Concat(plan.Warnings));
            ShowStatus($"{plan.Windows.Count(window => window.Status == WindowLayoutMatchStatus.Matched)} of {plan.Windows.Count} windows have a unique match. Preview changed no windows.", InfoBarSeverity.Informational);
        } catch (Exception ex) { ShowStatus(ex.Message, InfoBarSeverity.Error); }
    }

    private void InspectDesktopsButton_Click(object sender, RoutedEventArgs e) {
        if (VirtualDesktopsCheck.IsChecked != true) {
            ShowStatus("Select experimental virtual desktop support before inspecting the shell.", InfoBarSeverity.Warning);
            return;
        }
        try {
            using var desktops = new ExperimentalVirtualDesktopService();
            ResultsText.Text = JsonSerializer.Serialize(desktops.GetDesktops(), new JsonSerializerOptions { WriteIndented = true });
            ShowStatus("Live virtual desktop identities and wallpapers inspected. No state was changed.", InfoBarSeverity.Informational);
        } catch (Exception ex) { ShowStatus(ex.Message, InfoBarSeverity.Error); }
    }

    private async void RestoreButton_Click(object sender, RoutedEventArgs e) {
        if (_busy) { return; }
        var dialog = new ContentDialog {
            XamlRoot = XamlRoot, Title = "Restore this workspace?",
            Content = "Matching windows will be repositioned. Only the selected optional desktop, pin, folder and wallpaper actions will run.",
            PrimaryButtonText = "Restore", CloseButtonText = "Cancel", DefaultButton = ContentDialogButton.Close
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary) { await RestoreAsync(startup: false); }
    }

    private async Task RestoreAsync(bool startup) {
        SetBusy(true);
        try {
            WindowLayout layout = WorkspaceRecoveryService.Read(_snapshotPath);
            WorkspaceRestoreResult result = await _service.RestoreAsync(layout, new WorkspaceRestoreOptions {
                RestoreVirtualDesktops = startup ? _settings.IncludeVirtualDesktops : VirtualDesktopsCheck.IsChecked == true,
                RestoreApplicationPins = startup ? _settings.RestoreApplicationPins : ApplicationPinsCheck.IsChecked == true,
                ReopenExplorerFolders = startup ? _settings.ReopenExplorerFolders : ReopenExplorerCheck.IsChecked == true,
                RestoreWallpapers = !startup && WallpapersCheck.IsChecked == true
            }, maximumAttempts: 12, cancellationToken: _lifetime.Token);
            _automaticCaptureReady = result.Windows.All(window => window.PositionApplied && window.VirtualDesktopApplied != false && window.Error == null) && result.Warnings.Count == 0;
            ResultsText.Text = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            ShowStatus(_automaticCaptureReady ? "Workspace restored." : "Recovery is incomplete. Review the results; periodic capture is paused.",
                _automaticCaptureReady ? InfoBarSeverity.Success : InfoBarSeverity.Warning);
        } catch (OperationCanceledException) { /* The owning app window closed. */ }
        catch (Exception ex) {
            _automaticCaptureReady = false;
            ShowStatus(ex.Message, InfoBarSeverity.Error);
        } finally { SetBusy(false); }
    }

    private void SaveSettingsButton_Click(object sender, RoutedEventArgs e) {
        if (_busy || _settingsFailed) { return; }
        try {
            var settings = new WorkspaceRecoverySettings {
                RestoreOnStartup = RestoreOnStartupCheck.IsChecked == true,
                AutoCapture = AutoCaptureCheck.IsChecked == true,
                IncludeVirtualDesktops = VirtualDesktopsCheck.IsChecked == true,
                IncludeExplorerFolders = ExplorerFoldersCheck.IsChecked == true,
                ReopenExplorerFolders = ReopenExplorerCheck.IsChecked == true,
                RestoreApplicationPins = ApplicationPinsCheck.IsChecked == true
            };
            if (settings.RestoreApplicationPins && !settings.IncludeVirtualDesktops) {
                ShowStatus("Application pins also require virtual desktop capture and restoration.", InfoBarSeverity.Warning);
                return;
            }
            settings.Save(App.WorkspaceDirectory);
            _settings = settings;
            ShowStatus("Recovery settings saved. Startup recovery applies on the next app launch.", InfoBarSeverity.Success);
        } catch (Exception ex) { ShowStatus(ex.Message, InfoBarSeverity.Error); }
    }

    private void Timer_Tick(DispatcherQueueTimer sender, object args) {
        if (_busy || !_settings.AutoCapture || !_automaticCaptureReady) { return; }
        try {
            // Locked sessions and shutdown can expose an incomplete set of windows.
            DesktopSessionInfo session = new DesktopSessionService().GetCurrentSession();
            if (session.IsLocked != false) { return; }
            if (_service.TrySaveAutomatic(_snapshotPath, new WorkspaceCaptureOptions {
                IncludeVirtualDesktops = _settings.IncludeVirtualDesktops,
                IncludeExplorerFolders = _settings.IncludeExplorerFolders
            })) { RefreshSnapshotStatus(); }
        } catch (Exception ex) { ShowStatus("Periodic capture preserved the previous snapshot. " + ex.Message, InfoBarSeverity.Warning); }
    }

    private WorkspaceCaptureOptions CaptureOptions() => new() {
        IncludeVirtualDesktops = VirtualDesktopsCheck.IsChecked == true,
        IncludeExplorerFolders = ExplorerFoldersCheck.IsChecked == true
    };

    private void RefreshSnapshotStatus() {
        if (!File.Exists(_snapshotPath)) { SnapshotStatusText.Text = "No snapshot yet. Capture a workspace to begin."; return; }
        WindowLayout snapshot = WorkspaceRecoveryService.Read(_snapshotPath);
        SnapshotStatusText.Text = $"Saved {snapshot.CapturedAt?.ToLocalTime():g} · {snapshot.Windows.Count} windows · {snapshot.Desktops.Count} virtual desktops";
    }

    private void SetBusy(bool value) {
        _busy = value;
        CaptureButton.IsEnabled = PreviewButton.IsEnabled = RestoreButton.IsEnabled = InspectDesktopsButton.IsEnabled = !value;
        SaveSettingsButton.IsEnabled = !value && !_settingsFailed;
    }

    private void ShowStatus(string message, InfoBarSeverity severity) {
        StatusInfo.Message = message; StatusInfo.Severity = severity; StatusInfo.IsOpen = true;
    }

    /// <summary>Stops only this panel's timer and cancels pending startup retries.</summary>
    public void Dispose() {
        _timer?.Stop();
        if (_timer != null) { _timer.Tick -= Timer_Tick; }
        _lifetime.Cancel();
        _lifetime.Dispose();
    }
}
