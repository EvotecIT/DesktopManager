using System.Runtime.Versioning;
using System.Security.Principal;

namespace DesktopManager;

/// <summary>
/// Helper for checking if the current process is running with administrative privileges.
/// </summary>
[SupportedOSPlatform("windows")]
public static class PrivilegeChecker {
    /// <summary>
    /// Returns true if the current process is elevated.
    /// </summary>
    public static bool IsElevated {
        get {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> if the current process is not elevated.
    /// </summary>
    public static void EnsureElevated() {
        if (!IsElevated) {
            throw new InvalidOperationException("Operation requires administrative privileges.");
        }
    }
}
