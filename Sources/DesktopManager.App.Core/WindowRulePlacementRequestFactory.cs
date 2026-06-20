namespace DesktopManager.App.Core;

/// <summary>
/// Converts saved layout rule actions into reusable DesktopManager placement requests.
/// </summary>
public static class WindowRulePlacementRequestFactory {
    /// <summary>
    /// Creates a placement request for a rule action and observed window.
    /// </summary>
    /// <param name="action">Rule placement action.</param>
    /// <param name="targetWindowHandle">Window handle to place.</param>
    /// <returns>A placement request for the DesktopManager placement engine.</returns>
    public static global::DesktopManager.WindowPlacementRequest Create(WindowHotkeyActionDefinition action, IntPtr targetWindowHandle) {
        return WindowHotkeyPlacementRequestFactory.Create(action, targetWindowHandle);
    }
}
