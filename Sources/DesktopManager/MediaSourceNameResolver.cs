using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace DesktopManager;

// SMTC reports the source as an AppUserModelID ("MSEdge", "Vendor.App_hash!App") or an executable name/path.
// The Start menu's AppsFolder knows the display name for registered IDs; executables fall back to their file description.
internal static class MediaSourceNameResolver {
    private static readonly Guid ShellItemInterfaceId = new("43826D1E-E718-42EE-BC55-A1E261C37BFE");
    private static readonly ConcurrentDictionary<string, string> Cache = new(StringComparer.OrdinalIgnoreCase);

    internal static string Resolve(string appId) {
        if (string.IsNullOrWhiteSpace(appId)) {
            return FallbackName(appId ?? string.Empty);
        }
        // Executable basenames can refer to a different process on the next read, and files can be replaced.
        if (appId.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            || appId.IndexOf('\\') >= 0 || appId.IndexOf('/') >= 0) {
            return ResolveUncached(appId) ?? FallbackName(appId);
        }
        if (Cache.TryGetValue(appId, out string? cached)) {
            return cached;
        }
        string? resolved = ResolveUncached(appId);
        if (resolved == null) {
            // A source may have exited or not registered yet. Let the next read try again.
            return FallbackName(appId);
        }
        return Cache.GetOrAdd(appId, resolved);
    }

    private static string? ResolveUncached(string appId) {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            try {
                string? name = Path.IsPathRooted(appId) ? null : GetAppsFolderName(appId);
                name ??= GetExecutableDescription(appId);
                if (!string.IsNullOrWhiteSpace(name)) {
                    return name!.Trim();
                }
            } catch (Exception ex) {
                DesktopManagerDiagnostics.Report($"Media source name could not be resolved: {ex.Message}");
            }
        }
        return null;
    }

    internal static string? GetAppsFolderName(string appId) {
        Guid interfaceId = ShellItemInterfaceId;
        int result = MonitorNativeMethods.SHCreateItemFromParsingName(@"shell:AppsFolder\" + appId, IntPtr.Zero, ref interfaceId, out IntPtr pointer);
        if (result != 0 || pointer == IntPtr.Zero) {
            return null;
        }
        try {
            var item = NativeComActivation.FromPointer<MonitorNativeMethods.IShellItem>(pointer);
            try {
                if (item.GetDisplayName(MonitorNativeMethods.SIGDN.NormalDisplay, out IntPtr namePointer) != 0 || namePointer == IntPtr.Zero) {
                    return null;
                }
                try {
                    string? name = Marshal.PtrToStringUni(namePointer);
                    // An unregistered ID can parse to an item that merely echoes the ID back.
                    return string.IsNullOrWhiteSpace(name) || string.Equals(name, appId, StringComparison.OrdinalIgnoreCase) ? null : name;
                } finally {
                    MonitorNativeMethods.CoTaskMemFree(namePointer);
                }
            } finally {
                NativeComActivation.Release(item);
            }
        } finally {
            Marshal.Release(pointer);
        }
    }

    private static string? GetExecutableDescription(string appId) {
        string? path = Path.IsPathRooted(appId) ? appId : FindRunningExecutable(appId);
        // Metadata is optional. Avoid UNC paths and mapped network drives while media operations are serialized.
        if (path == null || path.StartsWith(@"\\", StringComparison.Ordinal)
            || new DriveInfo(Path.GetPathRoot(path)!).DriveType != DriveType.Fixed || !File.Exists(path)) {
            return null;
        }
        try {
            string? description = FileVersionInfo.GetVersionInfo(path).FileDescription;
            return string.IsNullOrWhiteSpace(description) ? null : description;
        } catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException) {
            return null;
        }
    }

    private static string? FindRunningExecutable(string appId) {
        if (!appId.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || appId.IndexOfAny(new[] { '\\', '/', '!' }) >= 0) {
            return null;
        }
        Process[] processes = Process.GetProcessesByName(appId.Substring(0, appId.Length - 4));
        try {
            string? candidate = null;
            foreach (Process process in processes) {
                string? path = WindowManager.TryGetProcessPath((uint)process.Id);
                if (string.IsNullOrEmpty(path)
                    || (candidate != null && !string.Equals(candidate, path, StringComparison.OrdinalIgnoreCase))) {
                    return null;
                }
                candidate = path;
            }
            return candidate;
        } finally {
            foreach (Process process in processes) {
                process.Dispose();
            }
        }
    }

    internal static string FallbackName(string appId) {
        string[] parts = appId.Split('!', '\\');
        string name = parts[parts.Length - 1];
        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) {
            name = name.Substring(0, name.Length - 4);
        }
        int dot = name.LastIndexOf('.');
        if (dot >= 0 && dot < name.Length - 1 && name.IndexOf(' ') < 0) {
            name = name.Substring(dot + 1);
        }
        return name.Length == 0 ? "Media" : char.ToUpperInvariant(name[0]) + name.Substring(1);
    }
}
