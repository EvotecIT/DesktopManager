using System;

namespace DesktopManager.Cli;

internal static class MonitorValueParser {
    public static DisplayOrientation? ParseOptionalDisplayOrientation(string? value, string label) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        if (TryParseDisplayOrientation(value, out DisplayOrientation orientation)) {
            return orientation;
        }

        throw new CommandLineException($"{label} expects one of: default, degrees90, degrees180, degrees270.");
    }

    public static TaskbarPosition? ParseOptionalTaskbarPosition(string? value, string label) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        if (TryParseTaskbarPosition(value, out TaskbarPosition position)) {
            return position;
        }

        throw new CommandLineException($"{label} expects one of: left, top, right, bottom.");
    }

    private static bool TryParseDisplayOrientation(string value, out DisplayOrientation orientation) {
        switch (value.Trim().ToLowerInvariant()) {
            case "default":
            case "0":
                orientation = DisplayOrientation.Default;
                return true;
            case "degrees90":
            case "90":
                orientation = DisplayOrientation.Degrees90;
                return true;
            case "degrees180":
            case "180":
                orientation = DisplayOrientation.Degrees180;
                return true;
            case "degrees270":
            case "270":
                orientation = DisplayOrientation.Degrees270;
                return true;
            default:
                orientation = default;
                return false;
        }
    }

    private static bool TryParseTaskbarPosition(string value, out TaskbarPosition position) {
        switch (value.Trim().ToLowerInvariant()) {
            case "left":
                position = TaskbarPosition.Left;
                return true;
            case "top":
                position = TaskbarPosition.Top;
                return true;
            case "right":
                position = TaskbarPosition.Right;
                return true;
            case "bottom":
                position = TaskbarPosition.Bottom;
                return true;
            default:
                position = default;
                return false;
        }
    }
}
