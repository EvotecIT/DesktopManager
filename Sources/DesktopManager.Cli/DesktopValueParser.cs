using System;
using System.Globalization;

namespace DesktopManager.Cli;

internal static class DesktopValueParser {
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

    public static DesktopWallpaperPosition? ParseOptionalWallpaperPosition(string? value, string label) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        if (TryParseWallpaperPosition(value, out DesktopWallpaperPosition position)) {
            return position;
        }

        throw new CommandLineException($"{label} expects one of: center, tile, stretch, fit, fill, span.");
    }

    public static DesktopSlideshowDirection? ParseOptionalSlideshowDirection(string? value, string label) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        if (TryParseSlideshowDirection(value, out DesktopSlideshowDirection direction)) {
            return direction;
        }

        throw new CommandLineException($"{label} expects one of: forward, backward.");
    }

    public static uint ParseRequiredColor(string? value, string label) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new CommandLineException($"{label} is required.");
        }

        string trimmed = value.Trim();
        if (trimmed.StartsWith("#", StringComparison.Ordinal)) {
            string hexValue = trimmed.Substring(1);
            if (uint.TryParse(hexValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint parsedHexValue)) {
                return parsedHexValue;
            }
        } else if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) {
            string hexValue = trimmed.Substring(2);
            if (uint.TryParse(hexValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint parsedHexValue)) {
                return parsedHexValue;
            }
        } else if (uint.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint numericValue)) {
            return numericValue;
        }

        throw new CommandLineException($"{label} expects a decimal or hexadecimal RGB value.");
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

    private static bool TryParseWallpaperPosition(string value, out DesktopWallpaperPosition position) {
        switch (value.Trim().ToLowerInvariant()) {
            case "center":
                position = DesktopWallpaperPosition.Center;
                return true;
            case "tile":
                position = DesktopWallpaperPosition.Tile;
                return true;
            case "stretch":
                position = DesktopWallpaperPosition.Stretch;
                return true;
            case "fit":
                position = DesktopWallpaperPosition.Fit;
                return true;
            case "fill":
                position = DesktopWallpaperPosition.Fill;
                return true;
            case "span":
                position = DesktopWallpaperPosition.Span;
                return true;
            default:
                position = default;
                return false;
        }
    }

    private static bool TryParseSlideshowDirection(string value, out DesktopSlideshowDirection direction) {
        switch (value.Trim().ToLowerInvariant()) {
            case "forward":
                direction = DesktopSlideshowDirection.Forward;
                return true;
            case "backward":
                direction = DesktopSlideshowDirection.Backward;
                return true;
            default:
                direction = default;
                return false;
        }
    }
}
