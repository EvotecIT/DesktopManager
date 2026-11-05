using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using Microsoft.Win32;

namespace DesktopManager;

/// <summary>Reads the current user's Windows microphone and camera privacy-indicator records without changing permissions.</summary>
/// <remarks>
/// Consent-store timestamps describe Windows' last recorded usage and may lag actual device activity. A record
/// that started before Windows last booted is ignored: an app cannot hold a device across a restart, so a missing
/// stop time there means Windows went down mid-use, such as a crash during a call.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class DeviceUsageService {
    private readonly IReader _reader;
    private readonly Func<long> _bootFileTime;

    // Five minutes in FILETIME units (100 ns).
    internal const long BootClockAllowance = 5L * 60 * 10_000_000;

    /// <summary>Creates a reader for the current user's Windows capability consent store.</summary>
    public DeviceUsageService() : this(new RegistryReader(), BootFileTime) {
    }

    internal DeviceUsageService(IReader reader, Func<long> bootFileTime) {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _bootFileTime = bootFileTime ?? throw new ArgumentNullException(nameof(bootFileTime));
    }

    /// <summary>When Windows last started, as a UTC file time like the consent store's timestamps.</summary>
    private static long BootFileTime() => DateTime.UtcNow.AddMilliseconds(-(double)GetTickCount64()).ToFileTimeUtc();

    [DllImport("kernel32.dll")]
    private static extern ulong GetTickCount64();

    /// <summary>Gets applications Windows currently records as using the microphone.</summary>
    /// <returns>A usage snapshot, or an explicit unsupported or unavailable result.</returns>
    public DeviceUsageResult GetMicrophoneUsers() => Read("microphone");

    /// <summary>Gets applications Windows currently records as using the camera.</summary>
    /// <returns>A usage snapshot, or an explicit unsupported or unavailable result.</returns>
    public DeviceUsageResult GetCameraUsers() => Read("webcam");

    private DeviceUsageResult Read(string capability) {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            return Empty(DesktopOperationStatus.Unsupported, "Device usage discovery requires Windows.");
        }

        try {
            var applications = new List<DeviceUsageInfo>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            long boot = _bootFileTime();
            foreach (Entry entry in _reader.Read(capability)) {
                // The boot time is derived from the wall clock, so a correction after boot shifts it; allow a few minutes.
                if (entry.Start <= 0 || entry.Stop >= entry.Start || entry.Start < boot - BootClockAllowance) {
                    continue;
                }

                string name = FriendlyName(entry.Key);
                if (names.Add(name)) {
                    applications.Add(new DeviceUsageInfo(entry.Key, name));
                }
            }

            return new DeviceUsageResult(DesktopOperationStatus.Success, string.Empty, applications.AsReadOnly());
        } catch (Exception exception) when (exception is SecurityException || exception is UnauthorizedAccessException || exception is IOException) {
            return Empty(DesktopOperationStatus.Unavailable, "Windows device usage records could not be read: " + exception.Message);
        }
    }

    private static DeviceUsageResult Empty(DesktopOperationStatus status, string message) =>
        new DeviceUsageResult(status, message, Array.Empty<DeviceUsageInfo>());

    private static string FriendlyName(string key) {
        if (key.IndexOf('#') >= 0) {
            string path = key.Replace('#', '\\');
            try {
                if (File.Exists(path)) {
                    string? description = FileVersionInfo.GetVersionInfo(path).FileDescription;
                    if (!string.IsNullOrEmpty(description)) {
                        return description!;
                    }
                }
            } catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is SecurityException) {
                // Executable metadata is optional; the registry identity still supplies a name.
            }

            return Path.GetFileNameWithoutExtension(path);
        }

        int separator = key.IndexOf('_');
        string package = separator < 0 ? key : key.Substring(0, separator);
        return package.Substring(package.LastIndexOf('.') + 1);
    }

    /// <summary>Reads raw usage timestamps at the Windows registry boundary.</summary>
    internal interface IReader {
        IEnumerable<Entry> Read(string capability);
    }

    internal readonly struct Entry {
        internal Entry(string key, long start, long stop) {
            Key = key;
            Start = start;
            Stop = stop;
        }

        internal string Key { get; }
        internal long Start { get; }
        internal long Stop { get; }
    }

    private sealed class RegistryReader : IReader {
        private const string Store = @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\";

        public IEnumerable<Entry> Read(string capability) {
            using RegistryKey? root = Registry.CurrentUser.OpenSubKey(Store + capability, writable: false);
            if (root == null) {
                yield break;
            }

            foreach (Entry entry in Collect(root)) {
                yield return entry;
            }

            using RegistryKey? desktop = root.OpenSubKey("NonPackaged", writable: false);
            if (desktop != null) {
                foreach (Entry entry in Collect(desktop)) {
                    yield return entry;
                }
            }
        }

        private static IEnumerable<Entry> Collect(RegistryKey parent) {
            foreach (string name in parent.GetSubKeyNames()) {
                if (string.Equals(name, "NonPackaged", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                using RegistryKey? application = parent.OpenSubKey(name, writable: false);
                if (application?.GetValue("LastUsedTimeStart") is long start) {
                    long stop = application.GetValue("LastUsedTimeStop") is long value ? value : 0;
                    yield return new Entry(name, start, stop);
                }
            }
        }
    }
}
