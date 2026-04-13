using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace DesktopManager;

/// <summary>
/// Provides personalization orchestration for lock screen and desktop settings.
/// </summary>
public sealed class PersonalizationService {
    private const string PersonalizationPolicyPath = @"SOFTWARE\Policies\Microsoft\Windows\Personalization";
    private const string CloudContentPolicyPath = @"SOFTWARE\Policies\Microsoft\Windows\CloudContent";
    private readonly Monitors _monitors;

    /// <summary>
    /// Initializes a new instance of the <see cref="PersonalizationService"/> class.
    /// </summary>
    public PersonalizationService() {
        _monitors = new Monitors();
    }

    /// <summary>
    /// Captures personalization settings and policy values.
    /// </summary>
    /// <returns>The personalization snapshot.</returns>
    public PersonalizationSnapshot CaptureSnapshot() {
        var snapshot = new PersonalizationSnapshot {
            DesktopBackgroundColor = _monitors.GetBackgroundColor(),
            WallpaperPosition = _monitors.GetWallpaperPosition(),
            Policy = CapturePolicySnapshot()
        };

        foreach (var monitor in _monitors.GetMonitors(refresh: true)) {
            snapshot.Monitors.Add(new PersonalizationMonitorSnapshot {
                DeviceId = monitor.DeviceId,
                DeviceName = monitor.DeviceName,
                WallpaperPath = monitor.Wallpaper,
                IsPrimary = monitor.IsPrimary
            });
        }

        return snapshot;
    }

    /// <summary>
    /// Applies personalization settings.
    /// </summary>
    /// <param name="settings">Settings to apply.</param>
    public void Apply(PersonalizationSettings settings) {
        if (settings == null) {
            throw new ArgumentNullException(nameof(settings));
        }

        if (settings.DesktopWallpaperPosition.HasValue) {
            _monitors.SetWallpaperPosition(settings.DesktopWallpaperPosition.Value);
        }

        if (settings.DesktopWallpaperPath != null) {
            if (settings.ApplyWallpaperToAllUsers) {
                _monitors.SetWallpaperForAllUsers(settings.DesktopWallpaperPath,
                    settings.DesktopWallpaperPosition ?? _monitors.GetWallpaperPosition(),
                    settings.IncludeDefaultUserProfile);
            } else {
                _monitors.SetWallpaper(settings.DesktopWallpaperPath);
            }
        }

        if (settings.DesktopBackgroundColor.HasValue) {
            _monitors.SetBackgroundColor(settings.DesktopBackgroundColor.Value);
        }

        if (!string.IsNullOrWhiteSpace(settings.LockScreenImagePath)) {
            string lockScreenImagePath = settings.LockScreenImagePath!;
            ApplyLockScreenPolicyImage(lockScreenImagePath);
#if NET5_0_OR_GREATER
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240)) {
                ApplyLockScreenImage(lockScreenImagePath);
            }
#endif
        }

        if (settings.DisableLockScreenSlideshow.HasValue) {
            ApplyDwordPolicy(PersonalizationPolicyPath, "NoLockScreenSlideshow",
                settings.DisableLockScreenSlideshow.Value ? 1 : 0);
        }

        if (settings.DisableWindowsSpotlight.HasValue) {
            ApplyDwordPolicy(CloudContentPolicyPath, "DisableWindowsSpotlightFeatures",
                settings.DisableWindowsSpotlight.Value ? 1 : 0);
        }

        if (settings.DisableWindowsSpotlightOnLockScreen.HasValue) {
            ApplyDwordPolicy(CloudContentPolicyPath, "DisableWindowsSpotlightOnLockScreen",
                settings.DisableWindowsSpotlightOnLockScreen.Value ? 1 : 0);
        }
    }

    /// <summary>
    /// Restores personalization settings from a snapshot.
    /// </summary>
    /// <param name="snapshot">Snapshot to restore.</param>
    public void Restore(PersonalizationSnapshot snapshot) {
        if (snapshot == null) {
            throw new ArgumentNullException(nameof(snapshot));
        }

        ApplyPolicySnapshot(snapshot.Policy);

        if (snapshot.WallpaperPosition.HasValue) {
            _monitors.SetWallpaperPosition(snapshot.WallpaperPosition.Value);
        }

        if (snapshot.DesktopBackgroundColor.HasValue) {
            _monitors.SetBackgroundColor(snapshot.DesktopBackgroundColor.Value);
        }

        foreach (var monitor in snapshot.Monitors) {
            if (string.IsNullOrWhiteSpace(monitor.DeviceId) || string.IsNullOrWhiteSpace(monitor.WallpaperPath)) {
                continue;
            }
            _monitors.SetWallpaper(monitor.DeviceId, monitor.WallpaperPath);
        }
    }

    /// <summary>
    /// Applies a lock screen image using policy enforcement and user-scoped API.
    /// </summary>
    /// <param name="imagePath">Path to the lock screen image.</param>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public void ApplyLockScreenImage(string imagePath) {
        if (string.IsNullOrWhiteSpace(imagePath)) {
            throw new ArgumentNullException(nameof(imagePath));
        }

        if (!File.Exists(imagePath)) {
            throw new FileNotFoundException("The lock screen image was not found.", imagePath);
        }

        try {
            _monitors.SetLogonWallpaper(imagePath);
        } catch (Exception ex) {
            Console.WriteLine($"ApplyLockScreenImage fallback: {ex.Message}");
        }
    }

    private static void ApplyLockScreenPolicyImage(string imagePath) {
        PrivilegeChecker.EnsureElevated();
        ApplyStringPolicy(PersonalizationPolicyPath, "LockScreenImage", imagePath);
    }

    private static PersonalizationPolicySnapshot CapturePolicySnapshot() {
        return new PersonalizationPolicySnapshot {
            LockScreenImage = ReadStringPolicy(PersonalizationPolicyPath, "LockScreenImage"),
            NoLockScreenSlideshow = ReadDwordPolicy(PersonalizationPolicyPath, "NoLockScreenSlideshow"),
            DisableWindowsSpotlightFeatures = ReadDwordPolicy(CloudContentPolicyPath, "DisableWindowsSpotlightFeatures"),
            DisableWindowsSpotlightOnLockScreen = ReadDwordPolicy(CloudContentPolicyPath, "DisableWindowsSpotlightOnLockScreen")
        };
    }

    private static void ApplyPolicySnapshot(PersonalizationPolicySnapshot snapshot) {
        if (snapshot == null) {
            throw new ArgumentNullException(nameof(snapshot));
        }

        ApplyStringPolicy(PersonalizationPolicyPath, "LockScreenImage", snapshot.LockScreenImage);
        ApplyDwordPolicy(PersonalizationPolicyPath, "NoLockScreenSlideshow", snapshot.NoLockScreenSlideshow);
        ApplyDwordPolicy(CloudContentPolicyPath, "DisableWindowsSpotlightFeatures", snapshot.DisableWindowsSpotlightFeatures);
        ApplyDwordPolicy(CloudContentPolicyPath, "DisableWindowsSpotlightOnLockScreen", snapshot.DisableWindowsSpotlightOnLockScreen);
    }

    private static StringPolicyValue ReadStringPolicy(string keyPath, string valueName) {
        using RegistryKey? key = Registry.LocalMachine.OpenSubKey(keyPath, false);
        if (key == null) {
            return new StringPolicyValue();
        }

        object? value = key.GetValue(valueName);
        if (value == null) {
            return new StringPolicyValue();
        }

        return new StringPolicyValue {
            IsSet = true,
            Value = value.ToString()
        };
    }

    private static DwordPolicyValue ReadDwordPolicy(string keyPath, string valueName) {
        using RegistryKey? key = Registry.LocalMachine.OpenSubKey(keyPath, false);
        if (key == null) {
            return new DwordPolicyValue();
        }

        object? value = key.GetValue(valueName);
        if (value == null) {
            return new DwordPolicyValue();
        }

        if (value is int dword) {
            return new DwordPolicyValue {
                IsSet = true,
                Value = dword
            };
        }

        return new DwordPolicyValue {
            IsSet = true,
            Value = Convert.ToInt32(value)
        };
    }

    private static void ApplyStringPolicy(string keyPath, string valueName, string value) {
        PrivilegeChecker.EnsureElevated();
        using RegistryKey? key = Registry.LocalMachine.CreateSubKey(keyPath);
        key?.SetValue(valueName, value, RegistryValueKind.String);
    }

    private static void ApplyStringPolicy(string keyPath, string valueName, StringPolicyValue state) {
        if (state == null) {
            throw new ArgumentNullException(nameof(state));
        }

        if (state.IsSet) {
            ApplyStringPolicy(keyPath, valueName, state.Value ?? string.Empty);
            return;
        }

        using RegistryKey? key = Registry.LocalMachine.OpenSubKey(keyPath, true);
        key?.DeleteValue(valueName, false);
    }

    private static void ApplyDwordPolicy(string keyPath, string valueName, int value) {
        PrivilegeChecker.EnsureElevated();
        using RegistryKey? key = Registry.LocalMachine.CreateSubKey(keyPath);
        key?.SetValue(valueName, value, RegistryValueKind.DWord);
    }

    private static void ApplyDwordPolicy(string keyPath, string valueName, DwordPolicyValue state) {
        if (state == null) {
            throw new ArgumentNullException(nameof(state));
        }

        if (state.IsSet) {
            ApplyDwordPolicy(keyPath, valueName, state.Value);
            return;
        }

        using RegistryKey? key = Registry.LocalMachine.OpenSubKey(keyPath, true);
        key?.DeleteValue(valueName, false);
    }
}
