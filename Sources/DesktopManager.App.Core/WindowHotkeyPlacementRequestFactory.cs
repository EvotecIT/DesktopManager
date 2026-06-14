namespace DesktopManager.App.Core;

/// <summary>
/// Converts profile window actions into reusable DesktopManager placement requests.
/// </summary>
public static class WindowHotkeyPlacementRequestFactory {
    /// <summary>
    /// Creates a placement request for a profile window action.
    /// </summary>
    /// <param name="action">Profile window action.</param>
    /// <param name="targetWindowHandle">Window handle captured by the hotkey backend.</param>
    /// <returns>A placement request that can be executed by <see cref="global::DesktopManager.WindowPlacementService"/>.</returns>
    public static global::DesktopManager.WindowPlacementRequest Create(WindowHotkeyActionDefinition action, IntPtr targetWindowHandle) {
        if (action == null) {
            throw new ArgumentNullException(nameof(action));
        }

        bool exactPlacement = string.Equals(action.Placement, WindowPlacements.ExactRectangle, StringComparison.OrdinalIgnoreCase);
        return new global::DesktopManager.WindowPlacementRequest {
            TargetWindowHandle = targetWindowHandle,
            MonitorTarget = ParseMonitorTarget(action.Monitor),
            MonitorIndex = exactPlacement ? null : action.MonitorIndex,
            Placement = ParsePlacement(action),
            ExactLeft = exactPlacement ? action.ExactLeft : null,
            ExactTop = exactPlacement ? action.ExactTop : null,
            ExactWidth = exactPlacement ? action.ExactWidth : null,
            ExactHeight = exactPlacement ? action.ExactHeight : null,
            VerifyAfterAction = action.VerifyAfterAction
        };
    }

    private static global::DesktopManager.WindowMonitorTargetKind ParseMonitorTarget(string monitor) {
        return monitor switch {
            MonitorTargets.TopLeft => global::DesktopManager.WindowMonitorTargetKind.TopLeft,
            MonitorTargets.TopRight => global::DesktopManager.WindowMonitorTargetKind.TopRight,
            MonitorTargets.BottomLeft => global::DesktopManager.WindowMonitorTargetKind.BottomLeft,
            MonitorTargets.BottomRight => global::DesktopManager.WindowMonitorTargetKind.BottomRight,
            MonitorTargets.Current => global::DesktopManager.WindowMonitorTargetKind.Current,
            _ => global::DesktopManager.WindowMonitorTargetKind.Current
        };
    }

    private static global::DesktopManager.WindowPlacementKind ParsePlacement(WindowHotkeyActionDefinition action) {
        return action.Placement switch {
            WindowPlacements.Restore => global::DesktopManager.WindowPlacementKind.Restore,
            WindowPlacements.LeftHalf => global::DesktopManager.WindowPlacementKind.LeftHalf,
            WindowPlacements.RightHalf => global::DesktopManager.WindowPlacementKind.RightHalf,
            WindowPlacements.Maximize => global::DesktopManager.WindowPlacementKind.Maximize,
            WindowPlacements.ExactRectangle => global::DesktopManager.WindowPlacementKind.ExactRectangle,
            _ => throw new InvalidOperationException($"Unsupported window placement '{action.Placement}'.")
        };
    }
}
