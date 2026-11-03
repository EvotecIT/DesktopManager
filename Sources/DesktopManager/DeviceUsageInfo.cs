namespace DesktopManager;

/// <summary>Identifies an application reported as using a microphone or camera by Windows.</summary>
public sealed class DeviceUsageInfo {
    internal DeviceUsageInfo(string key, string name) {
        Key = key;
        Name = name;
    }

    /// <summary>Gets the original package family name or encoded executable path in the consent store.</summary>
    public string Key { get; }

    /// <summary>Gets the executable description, executable file name, or shortened package name.</summary>
    public string Name { get; }
}
