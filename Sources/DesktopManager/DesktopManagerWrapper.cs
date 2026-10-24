namespace DesktopManager;

/// <summary>
/// Activates the native desktop manager through its interface, including an existing RCW.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal static class DesktopManagerWrapper {
    internal static IDesktopManager Create() {
        var type = Type.GetTypeFromCLSID(new Guid("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD"));
        return (IDesktopManager)Activator.CreateInstance(type!)!;
    }
}
