namespace DesktopManager;

internal static class PortableDesktopBackendFactory {
    internal static IPortableDesktopBackend Create() {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            return new WindowsDesktopBackend();
        }
        var runner = new DesktopCommandRunner();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            return new MacDesktopBackend(runner, Environment.GetEnvironmentVariable);
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            return new LinuxDesktopBackend(runner, Environment.GetEnvironmentVariable);
        }
        return new UnsupportedDesktopBackend();
    }
}

internal sealed class UnsupportedDesktopBackend : IPortableDesktopBackend {
    private const string Message = "This operating system does not have a supported desktop adapter. Image generation remains available.";
    public DesktopCapabilities GetCapabilities() => new DesktopCapabilities("unsupported", false, false, false, false, false, Message);
    public IReadOnlyList<DesktopDisplayInfo> GetDisplays() => Array.Empty<DesktopDisplayInfo>();
    public IReadOnlyList<DesktopWallpaperTarget> GetWallpaperTargets() => Array.Empty<DesktopWallpaperTarget>();
    public DesktopUserSession GetSession() => new DesktopUserSession { UserName = Environment.UserName, AccountName = Environment.UserName };
    public DesktopWallpaperState GetWallpaper(string? displayId) => new DesktopWallpaperState(DesktopOperationStatus.Unsupported, Message);
    public DesktopOperationResult ApplyWallpaper(DesktopWallpaperRequest request) => new DesktopOperationResult(DesktopOperationStatus.Unsupported, Message);
}
