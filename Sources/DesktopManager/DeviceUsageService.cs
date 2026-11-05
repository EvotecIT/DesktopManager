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
/// that started before both the current sign-in and estimated boot is ignored. Timestamps between those
/// boundaries are ambiguous after Fast Startup or a clock correction and produce an unavailable result.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class DeviceUsageService {
    private readonly IReader _reader;
    private readonly Func<long> _bootFileTime;
    private readonly Func<long> _sessionFileTime;

    /// <summary>Creates a reader for the current user's Windows capability consent store.</summary>
    public DeviceUsageService() : this(new RegistryReader(), BootFileTime, DesktopSessionService.GetLogonFileTime) {
    }

    internal DeviceUsageService(IReader reader, Func<long> bootFileTime, Func<long>? sessionFileTime = null) {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _bootFileTime = bootFileTime ?? throw new ArgumentNullException(nameof(bootFileTime));
        _sessionFileTime = sessionFileTime ?? bootFileTime;
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
            long signIn = _sessionFileTime();
            if (boot <= 0 || signIn <= 0) {
                return Empty(DesktopOperationStatus.Unavailable, "Windows could not establish the current sign-in time for device usage records.");
            }
            foreach (Entry entry in _reader.Read(capability)) {
                if (entry.Start <= 0 || entry.Stop >= entry.Start) {
                    continue;
                }
                // Sign-in is a recorded timestamp, so a forward clock correction cannot shift it past an active call.
                if (entry.Start < signIn) {
                    if (entry.Start >= boot) {
                        // Kernel uptime survives Fast Startup, while sign-in does not. A backwards clock change can
                        // produce the same ordering for a current call. Never turn that uncertainty into "not in use".
                        return Empty(DesktopOperationStatus.Unavailable, "Windows device usage timestamps overlap an earlier sign-in or a clock correction.");
                    }
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
