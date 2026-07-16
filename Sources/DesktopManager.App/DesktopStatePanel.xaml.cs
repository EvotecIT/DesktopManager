using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace DesktopManager.App;

/// <summary>
/// Provides cohesive quick controls over DesktopManager's reusable desktop-state services.
/// </summary>
public sealed partial class DesktopStatePanel : UserControl {
    private readonly AudioService _audioService = new();
    private readonly SystemPowerService _powerService = new();
    private readonly DesktopSessionService _sessionService = new();
    private readonly TaskbarService _taskbarService = new();
    private readonly WorkstationProfileService _workstationProfileService = new();
    private readonly PersonalizationService _personalizationService = new();
    private readonly RadioService _radioService = new();
    private readonly ObservableCollection<AudioEndpointInfo> _audioEndpoints = new();
    private readonly ObservableCollection<DesktopRadioInfo> _radios = new();
    private readonly ObservableCollection<string> _workstationProfiles = new();
    private readonly ObservableCollection<string> _personalizationSnapshots = new();
    private KeepAwakeLease? _keepAwakeLease;
    private bool _loading;

    /// <summary>Initializes the desktop-state panel.</summary>
    public DesktopStatePanel() {
        InitializeComponent();
        AudioEndpointsComboBox.ItemsSource = _audioEndpoints;
        RadiosComboBox.ItemsSource = _radios;
        WorkstationProfilesComboBox.ItemsSource = _workstationProfiles;
        PersonalizationSnapshotsComboBox.ItemsSource = _personalizationSnapshots;
        RadioStateComboBox.ItemsSource = new[] { DesktopRadioState.On, DesktopRadioState.Off };
        Loaded += DesktopStatePanel_Loaded;
    }

    private async void DesktopStatePanel_Loaded(object sender, RoutedEventArgs e) {
        await RefreshAllAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) {
        await RefreshAllAsync();
    }

    private async Task RefreshAllAsync() {
        if (_loading) {
            return;
        }

        _loading = true;
        try {
            SystemPowerStatus power = _powerService.GetStatus();
            DesktopSessionInfo session = _sessionService.GetCurrentSession();
            string battery = power.HasBattery ? $", battery {power.BatteryPercent?.ToString() ?? "unknown"}%" : ", no battery";
            string lockState = session.IsLocked.HasValue ? (session.IsLocked.Value ? "locked" : "unlocked") : "lock unknown";
            SystemStateText.Text = $"{power.PowerLineState}{battery}. Session {session.SessionId}: {session.DomainName}\\{session.UserName}, {session.Protocol}, {lockState}, idle {session.IdleTime:g}.";

            ReplaceItems(_audioEndpoints, _audioService.GetEndpoints(AudioDataFlow.All, AudioEndpointState.Active));
            if (_audioEndpoints.Count > 0 && AudioEndpointsComboBox.SelectedItem == null) {
                AudioEndpointsComboBox.SelectedIndex = 0;
            }

            ReplaceItems(_radios, await _radioService.GetRadiosAsync());
            if (_radios.Count > 0 && RadiosComboBox.SelectedItem == null) {
                RadiosComboBox.SelectedIndex = 0;
            }

            ReplaceItems(_workstationProfiles, WorkstationProfileStore.List());
            ReplaceItems(_personalizationSnapshots, PersonalizationStateStore.ListSnapshots());
            TaskbarAutoHideSwitch.IsOn = _taskbarService.GetTaskbarAutoHide();
            ShowStatus("Desktop state refreshed.", InfoBarSeverity.Success);
        } catch (Exception ex) {
            ShowStatus(ex.Message, InfoBarSeverity.Error);
        } finally {
            _loading = false;
        }
    }

    private void AudioEndpointsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
        if (AudioEndpointsComboBox.SelectedItem is not AudioEndpointInfo endpoint) {
            return;
        }

        AudioVolumeSlider.Value = endpoint.VolumePercent ?? 0;
        AudioMutedSwitch.IsOn = endpoint.IsMuted == true;
        ConsoleRoleCheckBox.IsChecked = endpoint.DefaultRoles.Contains(AudioRole.Console);
        MultimediaRoleCheckBox.IsChecked = endpoint.DefaultRoles.Contains(AudioRole.Multimedia);
        CommunicationsRoleCheckBox.IsChecked = endpoint.DefaultRoles.Contains(AudioRole.Communications);
    }

    private async void ApplyAudioButton_Click(object sender, RoutedEventArgs e) {
        if (AudioEndpointsComboBox.SelectedItem is not AudioEndpointInfo endpoint) {
            ShowStatus("Select an audio endpoint first.", InfoBarSeverity.Warning);
            return;
        }

        try {
            _audioService.SetEndpointVolume(endpoint.Id, (float)AudioVolumeSlider.Value);
            _audioService.SetEndpointMute(endpoint.Id, AudioMutedSwitch.IsOn);
            AudioRole[] roles = GetSelectedAudioRoles();
            if (roles.Length > 0) {
                _audioService.SetDefaultAudioDevice(endpoint.Id, roles);
            }
            ShowStatus($"Updated {endpoint.Name}.", InfoBarSeverity.Success);
            await RefreshAllAsync();
        } catch (Exception ex) {
            ShowStatus(ex.Message, InfoBarSeverity.Error);
        }
    }

    private void KeepAwakeSwitch_Toggled(object sender, RoutedEventArgs e) {
        if (_loading) {
            return;
        }

        try {
            _keepAwakeLease?.Dispose();
            _keepAwakeLease = KeepAwakeSwitch.IsOn
                ? _powerService.CreateKeepAwakeLease(KeepAwakeOptions.System | KeepAwakeOptions.Display)
                : null;
            ShowStatus(KeepAwakeSwitch.IsOn ? "Keep-awake request started." : "Keep-awake request stopped.", InfoBarSeverity.Success);
        } catch (Exception ex) {
            KeepAwakeSwitch.IsOn = false;
            ShowStatus(ex.Message, InfoBarSeverity.Error);
        }
    }

    private async void LockSessionButton_Click(object sender, RoutedEventArgs e) {
        try {
            if (await ConfirmAsync("Lock this session?", "DesktopManager will lock the current Windows session immediately.")) {
                _powerService.LockWorkstation();
            }
        } catch (Exception ex) {
            ShowStatus(ex.Message, InfoBarSeverity.Error);
        }
    }

    private async void SaveWorkstationProfileButton_Click(object sender, RoutedEventArgs e) {
        string name = WorkstationProfileNameBox.Text.Trim();
        await RunOperationAsync(async () => {
            WorkstationProfile profile = _workstationProfileService.CaptureProfile();
            WorkstationProfileStore.Save(name, profile);
            await RefreshAllAsync();
        }, $"Saved workstation profile '{name}'.");
    }

    private async void ApplyWorkstationProfileButton_Click(object sender, RoutedEventArgs e) {
        string? name = WorkstationProfilesComboBox.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(name)) {
            ShowStatus("Select a workstation profile first.", InfoBarSeverity.Warning);
            return;
        }

        if (!await ConfirmAsync("Apply workstation profile?", $"Apply monitor, audio, taskbar, and personalization state from '{name}'?")) {
            return;
        }

        await RunOperationAsync(() => Task.Run(() => _workstationProfileService.ApplyProfile(name)), $"Applied workstation profile '{name}'.");
    }

    private async void DeleteWorkstationProfileButton_Click(object sender, RoutedEventArgs e) {
        string? name = WorkstationProfilesComboBox.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(name) || !await ConfirmAsync("Delete workstation profile?", $"Delete '{name}'?")) {
            return;
        }

        await RunOperationAsync(async () => {
            WorkstationProfileStore.Delete(name);
            await RefreshAllAsync();
        }, $"Deleted workstation profile '{name}'.");
    }

    private void ApplyTaskbarButton_Click(object sender, RoutedEventArgs e) {
        try {
            _taskbarService.SetTaskbarAutoHide(TaskbarAutoHideSwitch.IsOn);
            ShowStatus("Updated taskbar auto-hide.", InfoBarSeverity.Success);
        } catch (Exception ex) {
            ShowStatus(ex.Message, InfoBarSeverity.Error);
        }
    }

    private async void SavePersonalizationButton_Click(object sender, RoutedEventArgs e) {
        string name = PersonalizationNameBox.Text.Trim();
        await RunOperationAsync(async () => {
            PersonalizationStateStore.SaveSnapshot(name, _personalizationService.CaptureSnapshot());
            await RefreshAllAsync();
        }, $"Saved personalization snapshot '{name}'.");
    }

    private async void RestorePersonalizationButton_Click(object sender, RoutedEventArgs e) {
        string? name = PersonalizationSnapshotsComboBox.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(name) || !await ConfirmAsync("Restore personalization?", $"Restore the settings in '{name}'?")) {
            return;
        }

        await RunOperationAsync(
            () => Task.Run(() => _personalizationService.Restore(
                PersonalizationStateStore.LoadSnapshot(name),
                restoreMachinePolicies: false)),
            $"Restored personalization snapshot '{name}'.");
    }

    private async void DeletePersonalizationButton_Click(object sender, RoutedEventArgs e) {
        string? name = PersonalizationSnapshotsComboBox.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(name) || !await ConfirmAsync("Delete personalization snapshot?", $"Delete '{name}'?")) {
            return;
        }

        await RunOperationAsync(async () => {
            PersonalizationStateStore.DeleteSnapshot(name);
            await RefreshAllAsync();
        }, $"Deleted personalization snapshot '{name}'.");
    }

    private void RadiosComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
        if (RadiosComboBox.SelectedItem is DesktopRadioInfo radio) {
            RadioStateComboBox.SelectedItem = radio.State == DesktopRadioState.On ? DesktopRadioState.On : DesktopRadioState.Off;
        }
    }

    private async void ApplyRadioButton_Click(object sender, RoutedEventArgs e) {
        if (RadiosComboBox.SelectedItem is not DesktopRadioInfo radio || RadioStateComboBox.SelectedItem is not DesktopRadioState state) {
            ShowStatus("Select a radio and explicit state first.", InfoBarSeverity.Warning);
            return;
        }

        await RunOperationAsync(async () => {
            await _radioService.SetRadioStateAsync(radio.Kind, state, radio.Name);
            await RefreshAllAsync();
        }, $"Requested {state} for {radio.Name}.");
    }

    private void GetAirplaneModeButton_Click(object sender, RoutedEventArgs e) {
        RunExperimentalAirplaneOperation(service => service.GetState());
    }

    private void EnableAirplaneModeButton_Click(object sender, RoutedEventArgs e) {
        RunExperimentalAirplaneOperation(service => service.SetState(AirplaneModeState.Enabled));
    }

    private void DisableAirplaneModeButton_Click(object sender, RoutedEventArgs e) {
        RunExperimentalAirplaneOperation(service => service.SetState(AirplaneModeState.Disabled));
    }

    private void RunExperimentalAirplaneOperation(Func<ExperimentalAirplaneModeService, AirplaneModeState> operation) {
        if (ExperimentalAirplaneAcknowledgement.IsChecked != true) {
            ShowStatus("Acknowledge the experimental API before using global airplane mode.", InfoBarSeverity.Warning);
            return;
        }

        try {
            AirplaneModeState state = operation(new ExperimentalAirplaneModeService());
            AirplaneModeStateText.Text = $"Global airplane mode: {state}";
        } catch (Exception ex) {
            ShowStatus(ex.Message, InfoBarSeverity.Error);
        }
    }

    private void InspectVirtualDesktopButton_Click(object sender, RoutedEventArgs e) {
        try {
            IntPtr handle = ParseHandle(VirtualDesktopWindowHandleBox.Text);
            using var service = new VirtualDesktopService();
            Guid desktopId = service.GetWindowDesktopId(handle);
            VirtualDesktopIdBox.Text = desktopId.ToString();
            ShowStatus(service.IsWindowOnCurrentDesktop(handle) ? "Window is on the current desktop." : "Window is on another desktop.", InfoBarSeverity.Success);
        } catch (Exception ex) {
            ShowStatus(ex.Message, InfoBarSeverity.Error);
        }
    }

    private async void MoveVirtualDesktopButton_Click(object sender, RoutedEventArgs e) {
        try {
            IntPtr handle = ParseHandle(VirtualDesktopWindowHandleBox.Text);
            if (!Guid.TryParse(VirtualDesktopIdBox.Text, out Guid desktopId)) {
                throw new ArgumentException("Enter a valid desktop ID.");
            }
            if (!await ConfirmAsync("Move window?", $"Move window {VirtualDesktopWindowHandleBox.Text} to desktop {desktopId}?")) {
                return;
            }
            using var service = new VirtualDesktopService();
            service.MoveWindowToDesktop(handle, desktopId);
            ShowStatus("Window moved to the requested virtual desktop.", InfoBarSeverity.Success);
        } catch (Exception ex) {
            ShowStatus(ex.Message, InfoBarSeverity.Error);
        }
    }

    private AudioRole[] GetSelectedAudioRoles() {
        var roles = new List<AudioRole>(3);
        if (ConsoleRoleCheckBox.IsChecked == true) roles.Add(AudioRole.Console);
        if (MultimediaRoleCheckBox.IsChecked == true) roles.Add(AudioRole.Multimedia);
        if (CommunicationsRoleCheckBox.IsChecked == true) roles.Add(AudioRole.Communications);
        return roles.ToArray();
    }

    private async Task RunOperationAsync(Func<Task> operation, string successMessage) {
        try {
            await operation();
            ShowStatus(successMessage, InfoBarSeverity.Success);
        } catch (Exception ex) {
            ShowStatus(ex.Message, InfoBarSeverity.Error);
        }
    }

    private async Task<bool> ConfirmAsync(string title, string message) {
        var dialog = new ContentDialog {
            XamlRoot = XamlRoot,
            Title = title,
            Content = message,
            PrimaryButtonText = "Continue",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close
        };
        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    private void ShowStatus(string message, InfoBarSeverity severity) {
        StatusInfo.Message = message;
        StatusInfo.Severity = severity;
        StatusInfo.IsOpen = true;
    }

    private static IntPtr ParseHandle(string value) {
        string normalized = value.Trim();
        long parsed = normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? Convert.ToInt64(normalized.Substring(2), 16)
            : Convert.ToInt64(normalized, 10);
        if (parsed == 0) {
            throw new ArgumentException("Window handle cannot be zero.");
        }
        return new IntPtr(parsed);
    }

    private static void ReplaceItems<T>(ObservableCollection<T> destination, IEnumerable<T> source) {
        destination.Clear();
        foreach (T item in source) {
            destination.Add(item);
        }
    }
}
