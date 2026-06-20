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
            MonitorIndex = exactPlacement ? null : ResolveMonitorIndex(action),
            Placement = ParsePlacement(action),
            ExactLeft = exactPlacement ? action.ExactLeft : null,
            ExactTop = exactPlacement ? action.ExactTop : null,
            ExactWidth = exactPlacement ? action.ExactWidth : null,
            ExactHeight = exactPlacement ? action.ExactHeight : null,
            VerifyAfterAction = action.VerifyAfterAction
        };
    }

    private static global::DesktopManager.WindowMonitorTargetKind ParseMonitorTarget(string monitor) {
        if (string.Equals(monitor, MonitorTargets.TopLeft, StringComparison.OrdinalIgnoreCase)) {
            return global::DesktopManager.WindowMonitorTargetKind.TopLeft;
        }

        if (string.Equals(monitor, MonitorTargets.TopRight, StringComparison.OrdinalIgnoreCase)) {
            return global::DesktopManager.WindowMonitorTargetKind.TopRight;
        }

        if (string.Equals(monitor, MonitorTargets.BottomLeft, StringComparison.OrdinalIgnoreCase)) {
            return global::DesktopManager.WindowMonitorTargetKind.BottomLeft;
        }

        if (string.Equals(monitor, MonitorTargets.BottomRight, StringComparison.OrdinalIgnoreCase)) {
            return global::DesktopManager.WindowMonitorTargetKind.BottomRight;
        }

        if (string.Equals(monitor, MonitorTargets.Current, StringComparison.OrdinalIgnoreCase)) {
            return global::DesktopManager.WindowMonitorTargetKind.Current;
        }

        throw new InvalidOperationException($"Unsupported monitor target '{monitor}'.");
    }

    private static int? ResolveMonitorIndex(WindowHotkeyActionDefinition action) {
        if (string.IsNullOrWhiteSpace(action.MonitorStableKey)) {
            return action.MonitorIndex;
        }

        try {
            global::DesktopManager.MonitorTopologySnapshot topology = new global::DesktopManager.Monitors().GetMonitorTopology(refresh: true);
            global::DesktopManager.MonitorTopologyItem? item = topology.Items.FirstOrDefault(item =>
                string.Equals(item.Identity.StableKey, action.MonitorStableKey, StringComparison.OrdinalIgnoreCase));
            return item?.Monitor.Index ?? action.MonitorIndex;
        } catch {
            return action.MonitorIndex;
        }
    }

    private static global::DesktopManager.WindowPlacementKind ParsePlacement(WindowHotkeyActionDefinition action) {
        if (string.Equals(action.Placement, WindowPlacements.Restore, StringComparison.OrdinalIgnoreCase)) {
            return global::DesktopManager.WindowPlacementKind.Restore;
        }

        if (string.Equals(action.Placement, WindowPlacements.LeftHalf, StringComparison.OrdinalIgnoreCase)) {
            return global::DesktopManager.WindowPlacementKind.LeftHalf;
        }

        if (string.Equals(action.Placement, WindowPlacements.RightHalf, StringComparison.OrdinalIgnoreCase)) {
            return global::DesktopManager.WindowPlacementKind.RightHalf;
        }

        if (string.Equals(action.Placement, WindowPlacements.Maximize, StringComparison.OrdinalIgnoreCase)) {
            return global::DesktopManager.WindowPlacementKind.Maximize;
        }

        if (string.Equals(action.Placement, WindowPlacements.ExactRectangle, StringComparison.OrdinalIgnoreCase)) {
            return global::DesktopManager.WindowPlacementKind.ExactRectangle;
        }

        throw new InvalidOperationException($"Unsupported window placement '{action.Placement}'.");
    }
}
