using System.Runtime.InteropServices;

namespace DesktopManager.AotSmoke;

/// <summary>Reads the shared audio/media APIs without changing volume, mute, playback or device configuration.</summary>
internal static class AudioMediaChecks {
    internal static void Run() {
        var unavailable = new List<string>();
        ReadEndpoint("Application sessions", () => {
            using var sessions = new AudioSessionService();
            var applications = sessions.GetApplications();
            if (applications.Any(app => app.VolumePercent < 0 || app.VolumePercent > 100 || app.PeakValue < 0 || app.PeakValue > 1)) {
                throw new InvalidOperationException("Application audio values must remain within their documented ranges.");
            }
            Console.WriteLine($"Read {applications.Count} audio application channels and {sessions.GetPeaks().Count} cached peak levels; no writes.");
        }, unavailable);
        var audio = new AudioService();
        foreach (var direction in new[] { AudioDataFlow.Render, AudioDataFlow.Capture }) {
            ReadEndpoint($"Default {direction}", () => {
                using var endpoint = audio.OpenDefaultEndpoint(direction, direction == AudioDataFlow.Render ? AudioRole.Multimedia : AudioRole.Communications);
                if (!endpoint.IsAvailable) {
                    unavailable.Add($"Default {direction} endpoint is inactive.");
                    return;
                }
                var level = endpoint.GetState();
                if (level.VolumePercent < 0 || level.VolumePercent > 100 || level.PeakValue < 0 || level.PeakValue > 1) {
                    throw new InvalidOperationException("Endpoint audio values must remain within their documented ranges.");
                }
                Console.WriteLine($"Read default {direction} volume/mute/peak state; no writes.");
                if (direction == AudioDataFlow.Render) {
                    IntPtr client = endpoint.ActivateInterface(new Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2"));
                    Marshal.Release(client);
                    Console.WriteLine("Activated and released IAudioClient; no stream initialization or capture.");
                }
            }, unavailable);
        }
        using var media = new MediaSessionService();
        var current = media.GetCurrentAsync().GetAwaiter().GetResult();
        if (CheckResult(current, "Media query", unavailable)) {
            Console.WriteLine($"Media query succeeded; active session={current.Session != null}, encoded artwork bytes={current.Session?.ArtworkData?.Length ?? 0}. No transport calls.");
        }
        var usage = new DeviceUsageService();
        foreach (var result in new[] { usage.GetMicrophoneUsers(), usage.GetCameraUsers() }) {
            if (CheckResult(result, "Device usage query", unavailable)) {
                Console.WriteLine($"Read {result.Applications.Count} active device-use records; identities omitted.");
            }
        }
        if (unavailable.Count > 0) { throw new UnavailableException(string.Join(" ", unavailable)); }
    }

    private static bool CheckResult(DesktopOperationResult result, string name, List<string> unavailable) {
        if (result.Succeeded) { return true; }
        string message = $"{name}: {result.Status}: {result.Message}";
        if (result.Status != DesktopOperationStatus.Unsupported && result.Status != DesktopOperationStatus.Unavailable) {
            throw new InvalidOperationException(message);
        }
        unavailable.Add(message);
        return false;
    }

    private static void ReadEndpoint(string name, Action read, List<string> unavailable) {
        try {
            read();
        } catch (COMException ex) when (ex.HResult == unchecked((int)0x80070490)) {
            // E_NOTFOUND from GetDefaultAudioEndpoint means missing hardware, not failed generated interop.
            unavailable.Add($"{name}: no default audio endpoint exists.");
        }
    }
}
