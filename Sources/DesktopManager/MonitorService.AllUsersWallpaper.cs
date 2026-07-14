using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace DesktopManager;

/// <summary>
/// Provides methods for applying wallpaper settings to multiple user profiles.
/// </summary>
public partial class MonitorService {
    private const string ProfileListPath = @"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\ProfileList";
    private const string DesktopKeyPath = @"Control Panel\\Desktop";
    private const string WallpaperValueName = "Wallpaper";
    private const string WallpaperStyleValueName = "WallpaperStyle";
    private const string TileWallpaperValueName = "TileWallpaper";
    private static readonly UIntPtr HKeyUsers = new(0x80000003u);

    /// <summary>
    /// Sets the wallpaper for all user profiles on the machine.
    /// </summary>
    /// <param name="wallpaperPath">Path to the wallpaper image.</param>
    /// <param name="position">Wallpaper position to store for users.</param>
    /// <param name="includeDefaultProfile">Whether to update the default user profile.</param>
    public void SetWallpaperForAllUsers(string wallpaperPath, DesktopWallpaperPosition position, bool includeDefaultProfile = true) {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            throw new PlatformNotSupportedException("Wallpaper updates are supported only on Windows.");
        }
        if (string.IsNullOrWhiteSpace(wallpaperPath)) {
            throw new ArgumentNullException(nameof(wallpaperPath));
        }
        if (!File.Exists(wallpaperPath)) {
            throw new FileNotFoundException("The wallpaper file path does not exist.", wallpaperPath);
        }

        PrivilegeChecker.EnsureElevated();

        var (style, tile) = GetWallpaperStyle(position);

        foreach (var profile in EnumerateUserProfiles()) {
            try {
                ApplyWallpaperToProfile(profile, wallpaperPath, style, tile);
            } catch (Exception ex) {
                DesktopManagerDiagnostics.Report($"SetWallpaperForAllUsers failed for {profile.Sid}: {ex.Message}");
            }
        }

        if (includeDefaultProfile) {
            ApplyWallpaperToDefaultProfile(wallpaperPath, style, tile);
        }
    }

    private static void ApplyWallpaperToProfile(UserProfileHive profile, string wallpaperPath, string wallpaperStyle, string tileWallpaper) {
        using RegistryKey? hive = Registry.Users.OpenSubKey(profile.Sid, writable: true);
        if (hive != null) {
            ApplyWallpaperToUserHive(hive, wallpaperPath, wallpaperStyle, tileWallpaper);
            return;
        }

        if (!TryLoadUserHive(profile.Sid, profile.HivePath)) {
            return;
        }

        try {
            using RegistryKey? loaded = Registry.Users.OpenSubKey(profile.Sid, writable: true);
            if (loaded != null) {
                ApplyWallpaperToUserHive(loaded, wallpaperPath, wallpaperStyle, tileWallpaper);
            }
        } finally {
            TryUnloadUserHive(profile.Sid);
        }
    }

    private static void ApplyWallpaperToDefaultProfile(string wallpaperPath, string wallpaperStyle, string tileWallpaper) {
        var hivePath = GetDefaultUserHivePath();
        if (string.IsNullOrWhiteSpace(hivePath) || !File.Exists(hivePath)) {
            return;
        }

        var tempKey = "PowerBGInfo_Default_" + Guid.NewGuid().ToString("N");
        if (!TryLoadUserHive(tempKey, hivePath)) {
            return;
        }

        try {
            using RegistryKey? loaded = Registry.Users.OpenSubKey(tempKey, writable: true);
            if (loaded != null) {
                ApplyWallpaperToUserHive(loaded, wallpaperPath, wallpaperStyle, tileWallpaper);
            }
        } finally {
            TryUnloadUserHive(tempKey);
        }
    }

    private static void ApplyWallpaperToUserHive(RegistryKey userRoot, string wallpaperPath, string wallpaperStyle, string tileWallpaper) {
        using RegistryKey? desktopKey = userRoot.CreateSubKey(DesktopKeyPath);
        if (desktopKey == null) {
            return;
        }

        desktopKey.SetValue(WallpaperValueName, wallpaperPath, RegistryValueKind.String);
        desktopKey.SetValue(WallpaperStyleValueName, wallpaperStyle, RegistryValueKind.String);
        desktopKey.SetValue(TileWallpaperValueName, tileWallpaper, RegistryValueKind.String);
    }

    private static IEnumerable<UserProfileHive> EnumerateUserProfiles() {
        using RegistryKey? profileList = Registry.LocalMachine.OpenSubKey(ProfileListPath);
        if (profileList == null) {
            yield break;
        }

        foreach (var sid in profileList.GetSubKeyNames()) {
            using RegistryKey? profileKey = profileList.OpenSubKey(sid);
            if (profileKey == null) {
                continue;
            }

            var profilePath = profileKey.GetValue("ProfileImagePath") as string;
            if (string.IsNullOrWhiteSpace(profilePath)) {
                continue;
            }

            var expandedPath = Environment.ExpandEnvironmentVariables(profilePath);
            var hivePath = Path.Combine(expandedPath, "NTUSER.DAT");
            if (!File.Exists(hivePath)) {
                continue;
            }

            yield return new UserProfileHive(sid, hivePath);
        }
    }

    private static string GetDefaultUserHivePath() {
        var systemDrive = Environment.GetEnvironmentVariable("SystemDrive") ?? "C:";
        return Path.Combine(systemDrive, "Users", "Default", "NTUSER.DAT");
    }

    private static (string Style, string Tile) GetWallpaperStyle(DesktopWallpaperPosition position) {
        switch (position) {
            case DesktopWallpaperPosition.Tile:
                return ("0", "1");
            case DesktopWallpaperPosition.Center:
                return ("0", "0");
            case DesktopWallpaperPosition.Stretch:
                return ("2", "0");
            case DesktopWallpaperPosition.Fit:
                return ("6", "0");
            case DesktopWallpaperPosition.Fill:
                return ("10", "0");
            case DesktopWallpaperPosition.Span:
                return ("22", "0");
            default:
                return ("0", "0");
        }
    }

    private static bool TryLoadUserHive(string subKey, string hivePath) {
        int result = RegLoadKey(HKeyUsers, subKey, hivePath);
        if (result != 0) {
            return false;
        }
        return true;
    }

    private static void TryUnloadUserHive(string subKey) {
        try {
            RegUnLoadKey(HKeyUsers, subKey);
        } catch {
        }
    }

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int RegLoadKey(UIntPtr hKey, string lpSubKey, string lpFile);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int RegUnLoadKey(UIntPtr hKey, string lpSubKey);

    private readonly struct UserProfileHive {
        public UserProfileHive(string sid, string hivePath) {
            Sid = sid;
            HivePath = hivePath;
        }

        public string Sid { get; }
        public string HivePath { get; }
    }
}
