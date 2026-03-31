using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DesktopManager.Cli;

internal static class MonitorCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "list" => List(arguments),
            "brightness" => Brightness(arguments),
            "wallpaper" => Wallpaper(arguments),
            "set-wallpaper" => SetWallpaper(arguments),
            "set-brightness" => SetBrightness(arguments),
            "set-position" => SetPosition(arguments),
            "set-resolution" => SetResolution(arguments),
            "set-dpi-scaling" => SetDpiScaling(arguments),
            "set-taskbar" => SetTaskbar(arguments),
            _ => throw new CommandLineException($"Unknown monitor command '{action}'.")
        };
    }

    private static int List(CommandLineArguments arguments) {
        IReadOnlyList<MonitorResult> results = DesktopOperations.ListMonitors(
            connectedOnly: GetConnectedOnly(arguments),
            primaryOnly: GetPrimaryOnly(arguments),
            index: arguments.GetIntOption("index"),
            deviceId: arguments.GetOption("device-id"),
            deviceName: arguments.GetOption("device-name"));

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(results);
            return 0;
        }

        return WriteMonitorResults(results, Console.Out);
    }

    private static int Brightness(CommandLineArguments arguments) {
        IReadOnlyList<MonitorBrightnessResult> results = DesktopOperations.GetMonitorBrightness(
            connectedOnly: GetConnectedOnly(arguments),
            primaryOnly: GetPrimaryOnly(arguments),
            index: arguments.GetIntOption("index"),
            deviceId: arguments.GetOption("device-id"),
            deviceName: arguments.GetOption("device-name"));

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(results);
            return 0;
        }

        return WriteMonitorBrightnessResults(results, Console.Out);
    }

    private static int Wallpaper(CommandLineArguments arguments) {
        IReadOnlyList<MonitorWallpaperResult> results = DesktopOperations.GetMonitorWallpaper(
            connectedOnly: GetConnectedOnly(arguments),
            primaryOnly: GetPrimaryOnly(arguments),
            index: arguments.GetIntOption("index"),
            deviceId: arguments.GetOption("device-id"),
            deviceName: arguments.GetOption("device-name"));

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(results);
            return 0;
        }

        return WriteMonitorWallpaperResults(results, Console.Out);
    }

    private static int SetBrightness(CommandLineArguments arguments) {
        IReadOnlyList<MonitorBrightnessResult> results = DesktopOperations.SetMonitorBrightness(
            arguments.GetRequiredIntOption("brightness"),
            connectedOnly: GetConnectedOnly(arguments),
            primaryOnly: GetPrimaryOnly(arguments),
            index: arguments.GetIntOption("index"),
            deviceId: arguments.GetOption("device-id"),
            deviceName: arguments.GetOption("device-name"));

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(results);
            return 0;
        }

        return WriteMonitorBrightnessResults(results, Console.Out);
    }

    private static int SetPosition(CommandLineArguments arguments) {
        IReadOnlyList<MonitorResult> results = DesktopOperations.SetMonitorPosition(
            arguments.GetRequiredIntOption("left"),
            arguments.GetRequiredIntOption("top"),
            arguments.GetRequiredIntOption("right"),
            arguments.GetRequiredIntOption("bottom"),
            connectedOnly: GetConnectedOnly(arguments),
            primaryOnly: GetPrimaryOnly(arguments),
            index: arguments.GetIntOption("index"),
            deviceId: arguments.GetOption("device-id"),
            deviceName: arguments.GetOption("device-name"));

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(results);
            return 0;
        }

        return WriteMonitorResults(results, Console.Out);
    }

    private static int SetResolution(CommandLineArguments arguments) {
        IReadOnlyList<MonitorResult> results = DesktopOperations.SetMonitorResolution(
            arguments.GetRequiredIntOption("width"),
            arguments.GetRequiredIntOption("height"),
            DesktopValueParser.ParseOptionalDisplayOrientation(arguments.GetOption("orientation"), "Option '--orientation'"),
            connectedOnly: GetConnectedOnly(arguments),
            primaryOnly: GetPrimaryOnly(arguments),
            index: arguments.GetIntOption("index"),
            deviceId: arguments.GetOption("device-id"),
            deviceName: arguments.GetOption("device-name"));

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(results);
            return 0;
        }

        return WriteMonitorResults(results, Console.Out);
    }

    private static int SetDpiScaling(CommandLineArguments arguments) {
        IReadOnlyList<MonitorResult> results = DesktopOperations.SetMonitorDpiScaling(
            arguments.GetRequiredIntOption("scaling-percent"),
            connectedOnly: GetConnectedOnly(arguments),
            primaryOnly: GetPrimaryOnly(arguments),
            index: arguments.GetIntOption("index"),
            deviceId: arguments.GetOption("device-id"),
            deviceName: arguments.GetOption("device-name"));

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(results);
            return 0;
        }

        return WriteMonitorResults(results, Console.Out);
    }

    private static int SetTaskbar(CommandLineArguments arguments) {
        bool show = arguments.GetBoolFlag("show");
        bool hide = arguments.GetBoolFlag("hide");
        if (show && hide) {
            throw new CommandLineException("Cannot combine '--show' with '--hide'.");
        }

        bool? visible = show ? true : hide ? false : null;
        TaskbarPosition? position = DesktopValueParser.ParseOptionalTaskbarPosition(arguments.GetOption("position"), "Option '--position'");
        IReadOnlyList<MonitorResult> results = DesktopOperations.SetTaskbarPosition(
            position,
            visible,
            connectedOnly: GetConnectedOnly(arguments),
            primaryOnly: GetPrimaryOnly(arguments),
            index: arguments.GetIntOption("index"),
            deviceId: arguments.GetOption("device-id"),
            deviceName: arguments.GetOption("device-name"));

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(results);
            return 0;
        }

        return WriteMonitorResults(results, Console.Out);
    }

    private static int SetWallpaper(CommandLineArguments arguments) {
        IReadOnlyList<MonitorWallpaperResult> results = DesktopOperations.SetMonitorWallpaper(
            arguments.GetOption("wallpaper-path"),
            arguments.GetOption("url"),
            DesktopValueParser.ParseOptionalWallpaperPosition(arguments.GetOption("position"), "Option '--position'"),
            connectedOnly: GetConnectedOnly(arguments),
            primaryOnly: GetPrimaryOnly(arguments),
            index: arguments.GetIntOption("index"),
            deviceId: arguments.GetOption("device-id"),
            deviceName: arguments.GetOption("device-name"));

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(results);
            return 0;
        }

        return WriteMonitorWallpaperResults(results, Console.Out);
    }

    internal static int WriteMonitorBrightnessResults(IReadOnlyList<MonitorBrightnessResult> results, TextWriter writer) {
        IReadOnlyList<IReadOnlyList<string>> rows = results
            .Select(monitor => (IReadOnlyList<string>)new[] {
                monitor.Index.ToString(),
                monitor.IsPrimary ? "Yes" : "No",
                monitor.Brightness.ToString(),
                monitor.DeviceName,
                monitor.DeviceId
            })
            .ToArray();

        OutputFormatter.WriteTable(writer, new[] { "Idx", "Primary", "Brightness", "DeviceName", "DeviceId" }, rows);
        return 0;
    }

    internal static int WriteMonitorWallpaperResults(IReadOnlyList<MonitorWallpaperResult> results, TextWriter writer) {
        IReadOnlyList<IReadOnlyList<string>> rows = results
            .Select(monitor => (IReadOnlyList<string>)new[] {
                monitor.Index.ToString(),
                monitor.IsPrimary ? "Yes" : "No",
                monitor.DeviceName,
                monitor.DeviceId,
                monitor.Wallpaper
            })
            .ToArray();

        OutputFormatter.WriteTable(writer, new[] { "Idx", "Primary", "DeviceName", "DeviceId", "Wallpaper" }, rows);
        return 0;
    }

    internal static int WriteMonitorResults(IReadOnlyList<MonitorResult> results, TextWriter writer) {
        IReadOnlyList<IReadOnlyList<string>> rows = results
            .Select(monitor => (IReadOnlyList<string>)new[] {
                monitor.Index.ToString(),
                monitor.IsPrimary ? "Yes" : "No",
                monitor.IsConnected ? "Yes" : "No",
                $"{monitor.Left},{monitor.Top},{monitor.Right},{monitor.Bottom}",
                monitor.DeviceName,
                monitor.DeviceString
            })
            .ToArray();

        OutputFormatter.WriteTable(writer, new[] { "Idx", "Primary", "Connected", "Bounds", "DeviceName", "Device" }, rows);
        return 0;
    }

    private static bool? GetConnectedOnly(CommandLineArguments arguments) {
        return arguments.GetBoolFlag("connected") ? true : null;
    }

    private static bool? GetPrimaryOnly(CommandLineArguments arguments) {
        return arguments.GetBoolFlag("primary") ? true : null;
    }
}
