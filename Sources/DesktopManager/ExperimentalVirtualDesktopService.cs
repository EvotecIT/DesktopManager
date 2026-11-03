using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace DesktopManager;

/// <summary>
/// Opt-in Windows 11 shell operations for workspace recovery. Uses an undocumented,
/// version-gated contract; unsupported builds fail without falling back to registry writes.
/// Instances must be used and disposed on the creating thread.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240.0")]
public sealed class ExperimentalVirtualDesktopService : IDisposable, IWorkspaceDesktopApi {
    private readonly IWorkspaceShellServices _shell;
    private readonly IWorkspaceDesktopManager _manager;
    private readonly IWorkspaceApplicationViews _views;
    private readonly IWorkspacePinnedApps _pins;
    private readonly int _threadId = Environment.CurrentManagedThreadId;
    private bool _disposed;

    /// <summary>Connects to the gated Windows 11 shell interface family (builds 26100, 26200 and 26300).</summary>
    public ExperimentalVirtualDesktopService() {
        using RegistryKey? version = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        if (!int.TryParse(version?.GetValue("CurrentBuildNumber") as string, out int build) ||
                (build != 26100 && build != 26200 && build != 26300)) {
            throw new PlatformNotSupportedException("Experimental virtual desktop recovery requires Windows 11 build 26100, 26200 or 26300 and the supported shell interface IDs.");
        }
        object shell = NativeComActivation.CreateInstance(ExperimentalVirtualDesktopNative.ShellClass);
        try { _shell = (IWorkspaceShellServices)shell; }
        catch { ExperimentalVirtualDesktopNative.Release(shell); throw; }
        object? manager = null, views = null, pins = null;
        // Concrete casts retain generated interface metadata even when a consumer only enumerates desktops.
        try {
            manager = Query(ExperimentalVirtualDesktopNative.ManagerService, new Guid("53F5CA0B-158F-4124-900C-057158060B27"));
            _manager = (IWorkspaceDesktopManager)manager;
            views = Query(new Guid("1841C6D7-4F9D-42C0-AF41-8747538F10E5"), new Guid("1841C6D7-4F9D-42C0-AF41-8747538F10E5"));
            _views = (IWorkspaceApplicationViews)views;
            pins = Query(ExperimentalVirtualDesktopNative.PinsService, new Guid("4CE81583-1E4C-4632-A621-07A53543148F"));
            _pins = (IWorkspacePinnedApps)pins;
        } catch {
            ExperimentalVirtualDesktopNative.Release(pins);
            ExperimentalVirtualDesktopNative.Release(views);
            ExperimentalVirtualDesktopNative.Release(manager);
            ExperimentalVirtualDesktopNative.Release(_shell);
            throw;
        }
    }

    /// <summary>Reads desktop IDs, names and wallpaper paths from the live shell without changing state.</summary>
    public IReadOnlyList<VirtualDesktopInfo> GetDesktops() {
        EnsureUsable();
        _manager.GetDesktops(out IntPtr pointer);
        IWorkspaceObjectArray array = ExperimentalVirtualDesktopNative.WrapOwned<IWorkspaceObjectArray>(pointer);
        try {
            array.GetCount(out int count);
            var result = new List<VirtualDesktopInfo>();
            Guid iid = new("3F07F4BE-B107-441A-AF0F-39D82529072C");
            for (int index = 0; index < count; index++) {
                array.GetAt(index, ref iid, out IntPtr itemPointer);
                IWorkspaceDesktop desktop = ExperimentalVirtualDesktopNative.WrapOwned<IWorkspaceDesktop>(itemPointer);
                try {
                    desktop.GetName(out IntPtr name);
                    string desktopName = ExperimentalVirtualDesktopNative.ReadString(name);
                    desktop.GetWallpaperPath(out IntPtr wallpaper);
                    string wallpaperPath = ExperimentalVirtualDesktopNative.ReadString(wallpaper);
                    result.Add(new VirtualDesktopInfo {
                        Id = desktop.GetId(), Index = index + 1, Name = desktopName,
                        WallpaperPath = wallpaperPath
                    });
                } finally { ExperimentalVirtualDesktopNative.Release(desktop); }
            }
            return result;
        } finally { ExperimentalVirtualDesktopNative.Release(array); }
    }

    /// <summary>Captures a window's desktop and pin identity in the supplied saved position.</summary>
    public void CaptureWindow(WindowPosition window) {
        if (window == null) { throw new ArgumentNullException(nameof(window)); }
        WithView(window.Handle, view => {
            string appId = ReadApplicationId(view);
            window.AppUserModelId = appId;
            window.PinMode = !string.IsNullOrEmpty(appId) && _pins.IsAppIdPinned(appId)
                ? VirtualDesktopPinMode.Application : _pins.IsViewPinned(view) ? VirtualDesktopPinMode.Window : VirtualDesktopPinMode.None;
            using var manager = new VirtualDesktopService();
            Guid desktopId = manager.GetWindowDesktopId(window.Handle);
            window.VirtualDesktopId = window.PinMode == VirtualDesktopPinMode.None ? desktopId : null;
        });
    }

    /// <summary>Restores captured desktop and pin state, verifying the result. Application-wide pin changes require separate opt-in.</summary>
    public void RestoreWindow(WindowPosition saved, IntPtr handle, bool restoreApplicationPins = false) {
        if (saved == null) { throw new ArgumentNullException(nameof(saved)); }
        EnsureUsable();
        if (saved.PinMode.HasValue && !Enum.IsDefined(typeof(VirtualDesktopPinMode), saved.PinMode.Value)) {
            throw new ArgumentException("The saved pin mode is invalid.", nameof(saved));
        }
        IWorkspaceDesktop? target = null;
        Guid id = saved.VirtualDesktopId ?? Guid.Empty;
        if (saved.PinMode != VirtualDesktopPinMode.Application && saved.PinMode != VirtualDesktopPinMode.Window) {
            if (id == Guid.Empty) { throw new InvalidOperationException("The saved window has no target desktop identity."); }
            // Resolve the exact GUID before making even application-wide pin changes.
            target = FindDesktop(ref id);
        }
        try { WithView(handle, view => {
            string appId = ReadApplicationId(view);
            bool applicationPinned = !string.IsNullOrEmpty(appId) && _pins.IsAppIdPinned(appId);
            if (saved.PinMode == VirtualDesktopPinMode.Application) {
                if (!restoreApplicationPins) { throw new InvalidOperationException("Restoring an application pin requires explicit application-pin permission."); }
                if (string.IsNullOrEmpty(saved.AppUserModelId) || !string.Equals(appId, saved.AppUserModelId, StringComparison.Ordinal)) {
                    throw new InvalidOperationException("The live application identity does not match the saved application pin.");
                }
                _pins.PinAppId(appId);
                if (!_pins.IsAppIdPinned(appId)) { throw new InvalidOperationException("Windows did not apply the application pin."); }
                return;
            }
            if (applicationPinned) {
                if (!restoreApplicationPins) { throw new InvalidOperationException("The application is pinned; changing all its windows requires explicit application-pin permission."); }
                if (!string.IsNullOrEmpty(saved.AppUserModelId) && !string.Equals(saved.AppUserModelId, appId, StringComparison.Ordinal)) {
                    throw new InvalidOperationException("The live application identity does not match the saved pin state.");
                }
                _pins.UnpinAppId(appId);
                if (_pins.IsAppIdPinned(appId)) { throw new InvalidOperationException("Windows did not remove the application pin."); }
            }
            if (saved.PinMode == VirtualDesktopPinMode.Window) {
                _pins.PinView(view);
                if (!_pins.IsViewPinned(view)) { throw new InvalidOperationException("Windows did not apply the window pin."); }
                return;
            }
            if (_pins.IsViewPinned(view)) { _pins.UnpinView(view); }
            _manager.MoveViewToDesktop(view, target!);
            using var manager = new VirtualDesktopService();
            if (_pins.IsViewPinned(view) || manager.GetWindowDesktopId(handle) != id) {
                throw new InvalidOperationException("Windows did not apply the requested desktop assignment.");
            }
        }); } finally { ExperimentalVirtualDesktopNative.Release(target); }
    }

    /// <summary>Explicitly restores a desktop wallpaper through the live shell. Does not change slideshow configuration.</summary>
    public void SetWallpaper(Guid desktopId, string wallpaperPath) {
        EnsureUsable();
        if (desktopId == Guid.Empty) { throw new ArgumentException("A desktop ID is required.", nameof(desktopId)); }
        if (string.IsNullOrWhiteSpace(wallpaperPath) || !Path.IsPathRooted(wallpaperPath) || !File.Exists(wallpaperPath)) {
            throw new FileNotFoundException("The saved wallpaper must be an existing absolute file path.", wallpaperPath);
        }
        IWorkspaceDesktop desktop = FindDesktop(ref desktopId);
        try {
            Marshal.ThrowExceptionForHR(ExperimentalVirtualDesktopNative.WindowsCreateString(wallpaperPath, wallpaperPath.Length, out IntPtr value));
            try { _manager.SetDesktopWallpaper(desktop, value); }
            finally { ExperimentalVirtualDesktopNative.WindowsDeleteString(value); }
            desktop.GetWallpaperPath(out IntPtr actual);
            if (!string.Equals(ExperimentalVirtualDesktopNative.ReadString(actual), wallpaperPath, StringComparison.OrdinalIgnoreCase)) {
                throw new InvalidOperationException("Windows did not report the requested wallpaper after restoration.");
            }
        } finally { ExperimentalVirtualDesktopNative.Release(desktop); }
    }

    private object Query(Guid service, Guid iid) {
        _shell.QueryService(ref service, ref iid, out IntPtr pointer);
        return ExperimentalVirtualDesktopNative.WrapOwned<object>(pointer);
    }

    private IWorkspaceDesktop FindDesktop(ref Guid id) {
        _manager.FindDesktop(ref id, out IntPtr pointer);
        return ExperimentalVirtualDesktopNative.WrapOwned<IWorkspaceDesktop>(pointer);
    }

    private void WithView(IntPtr handle, Action<IntPtr> action) {
        EnsureUsable();
        if (handle == IntPtr.Zero) { throw new ArgumentException("A non-zero window handle is required.", nameof(handle)); }
        _views.GetViewForHwnd(handle, out IntPtr view);
        try { action(view); }
        finally { if (view != IntPtr.Zero) { Marshal.Release(view); } }
    }

    private static string ReadApplicationId(IntPtr view) {
        object identity = NativeComActivation.FromPointer(view);
        try {
            ((IWorkspaceApplicationIdentity)identity).GetAppUserModelId(out string appId);
            return appId;
        } finally { ExperimentalVirtualDesktopNative.Release(identity); }
    }

    private void EnsureUsable() {
        if (_disposed) { throw new ObjectDisposedException(nameof(ExperimentalVirtualDesktopService)); }
        if (_threadId != Environment.CurrentManagedThreadId) { throw new InvalidOperationException("Use the virtual desktop service on its creating thread."); }
    }

    /// <inheritdoc/>
    public void Dispose() {
        if (_disposed) { return; }
        EnsureUsable();
        ExperimentalVirtualDesktopNative.Release(_pins);
        ExperimentalVirtualDesktopNative.Release(_views);
        ExperimentalVirtualDesktopNative.Release(_manager);
        ExperimentalVirtualDesktopNative.Release(_shell);
        _disposed = true;
    }
}

internal interface IWorkspaceDesktopApi : IDisposable {
    IReadOnlyList<VirtualDesktopInfo> GetDesktops();
    void CaptureWindow(WindowPosition window);
    void RestoreWindow(WindowPosition saved, IntPtr handle, bool restoreApplicationPins);
    void SetWallpaper(Guid desktopId, string wallpaperPath);
}
