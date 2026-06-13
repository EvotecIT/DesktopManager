namespace DesktopManager.App.Core;

/// <summary>
/// Provides the first-run DesktopManager hotkey profile.
/// </summary>
public static class HotkeyProfileDefaults {
    /// <summary>
    /// Creates a default profile for common four-monitor window movement.
    /// </summary>
    /// <returns>A new default profile instance.</returns>
    public static HotkeyProfile CreateDefaultProfile() {
        return new HotkeyProfile {
            SchemaVersion = 1,
            Enabled = true,
            ProfileName = "EVOMAGIC 4 monitors",
            Functions = {
                CreateMonitorMove("move-top-left-maximize", "Move Window to Top Left Monitor", "Ctrl+Alt+Shift+5", MonitorTargets.TopLeft, 1),
                CreateMonitorMove("move-top-right-maximize", "Move Window to Top Right Monitor", "Ctrl+Alt+Shift+6", MonitorTargets.TopRight, 0),
                CreateMonitorMove("move-bottom-left-maximize", "Move Window to Bottom Left Monitor", "Ctrl+Alt+Shift+7", MonitorTargets.BottomLeft, 3),
                CreateMonitorMove("move-bottom-right-maximize", "Move Window to Bottom Right Monitor", "Ctrl+Alt+Shift+8", MonitorTargets.BottomRight, 2),
                CreateMonitorPlacement("move-top-left-left-half", "Move Window to Top Left Monitor Left Half", "Ctrl+Alt+Shift+1", MonitorTargets.TopLeft, 1, WindowPlacements.LeftHalf, -3840, 19, 1920, 2088),
                CreateMonitorPlacement("move-top-left-right-half", "Move Window to Top Left Monitor Right Half", "Ctrl+Alt+Shift+2", MonitorTargets.TopLeft, 1, WindowPlacements.RightHalf, -1920, 19, 1920, 2088),
                CreateMonitorPlacement("move-top-right-left-half", "Move Window to Top Right Monitor Left Half", "Ctrl+Alt+Shift+3", MonitorTargets.TopRight, 0, WindowPlacements.LeftHalf, 0, 0, 1920, 2088),
                CreateMonitorPlacement("move-top-right-right-half", "Move Window To Top Right Monitor Right Half", "Ctrl+Alt+Shift+4", MonitorTargets.TopRight, 0, WindowPlacements.RightHalf, 1920, 0, 1920, 2088),
                CreateWindowManagement("maximize-active-window", "Maximize Active Window", "Ctrl+Alt+Shift+9", WindowPlacements.Maximize)
            }
        };
    }

    private static HotkeyFunctionDefinition CreateMonitorMove(string id, string name, string hotkey, string monitor, int monitorIndex) {
        return CreateMonitorPlacement(id, name, hotkey, monitor, monitorIndex, WindowPlacements.Maximize);
    }

    private static HotkeyFunctionDefinition CreateMonitorPlacement(
        string id,
        string name,
        string hotkey,
        string monitor,
        int monitorIndex,
        string placement,
        int? exactLeft = null,
        int? exactTop = null,
        int? exactWidth = null,
        int? exactHeight = null) {
        return new HotkeyFunctionDefinition {
            Id = id,
            Name = name,
            Category = "Custom Functions",
            Hotkey = hotkey,
            WindowAction = new WindowHotkeyActionDefinition {
                Target = WindowTargets.ActiveWindow,
                Monitor = monitor,
                MonitorIndex = monitorIndex,
                Placement = placement,
                ExactLeft = exactLeft,
                ExactTop = exactTop,
                ExactWidth = exactWidth,
                ExactHeight = exactHeight,
                VerifyAfterAction = true
            }
        };
    }

    private static HotkeyFunctionDefinition CreateWindowManagement(string id, string name, string hotkey, string placement) {
        return new HotkeyFunctionDefinition {
            Id = id,
            Name = name,
            Category = "Window Management",
            Hotkey = hotkey,
            WindowAction = new WindowHotkeyActionDefinition {
                Target = WindowTargets.ActiveWindow,
                Monitor = MonitorTargets.Current,
                Placement = placement,
                VerifyAfterAction = true
            }
        };
    }
}
