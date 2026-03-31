using System.Collections.Generic;
using System.IO;

namespace DesktopManager.Cli;

internal static class DesktopCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "background-color" => BackgroundColor(arguments),
            "set-background-color" => SetBackgroundColor(arguments),
            "wallpaper-position" => WallpaperPosition(arguments),
            "set-wallpaper-position" => SetWallpaperPosition(arguments),
            "start-slideshow" => StartSlideshow(arguments),
            "stop-slideshow" => StopSlideshow(arguments),
            "advance-slideshow" => AdvanceSlideshow(arguments),
            _ => throw new CommandLineException($"Unknown desktop command '{action}'.")
        };
    }

    private static int BackgroundColor(CommandLineArguments arguments) {
        DesktopColorResult result = DesktopOperations.GetDesktopBackgroundColor();
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        return WriteColorResult(result, Console.Out);
    }

    private static int SetBackgroundColor(CommandLineArguments arguments) {
        DesktopColorResult result = DesktopOperations.SetDesktopBackgroundColor(
            DesktopValueParser.ParseRequiredColor(arguments.GetOption("color"), "Option '--color'"));
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        return WriteColorResult(result, Console.Out);
    }

    private static int WallpaperPosition(CommandLineArguments arguments) {
        DesktopWallpaperPositionResult result = DesktopOperations.GetDesktopWallpaperPosition();
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        return WriteWallpaperPositionResult(result, Console.Out);
    }

    private static int SetWallpaperPosition(CommandLineArguments arguments) {
        DesktopWallpaperPosition position = DesktopValueParser.ParseOptionalWallpaperPosition(arguments.GetOption("position"), "Option '--position'")
            ?? throw new CommandLineException("Option '--position' is required.");
        DesktopWallpaperPositionResult result = DesktopOperations.SetDesktopWallpaperPosition(position);
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        return WriteWallpaperPositionResult(result, Console.Out);
    }

    private static int StartSlideshow(CommandLineArguments arguments) {
        IReadOnlyList<string> imagePaths = arguments.GetOptions("image");
        if (imagePaths.Count == 0) {
            string single = arguments.GetRequiredOption("image");
            imagePaths = new[] { single };
        }

        DesktopSlideshowResult result = DesktopOperations.StartDesktopSlideshow(imagePaths);
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        return WriteSlideshowResult(result, Console.Out);
    }

    private static int StopSlideshow(CommandLineArguments arguments) {
        DesktopSlideshowResult result = DesktopOperations.StopDesktopSlideshow();
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        return WriteSlideshowResult(result, Console.Out);
    }

    private static int AdvanceSlideshow(CommandLineArguments arguments) {
        DesktopSlideshowDirection direction = DesktopValueParser.ParseOptionalSlideshowDirection(arguments.GetOption("direction"), "Option '--direction'")
            ?? throw new CommandLineException("Option '--direction' is required.");
        DesktopSlideshowResult result = DesktopOperations.AdvanceDesktopSlideshow(direction);
        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(result);
            return 0;
        }

        return WriteSlideshowResult(result, Console.Out);
    }

    internal static int WriteColorResult(DesktopColorResult result, TextWriter writer) {
        writer.WriteLine($"BackgroundColor: {result.HexValue}");
        writer.WriteLine($"- Value: {result.Value}");
        return 0;
    }

    internal static int WriteWallpaperPositionResult(DesktopWallpaperPositionResult result, TextWriter writer) {
        writer.WriteLine($"WallpaperPosition: {result.Position}");
        return 0;
    }

    internal static int WriteSlideshowResult(DesktopSlideshowResult result, TextWriter writer) {
        writer.WriteLine($"{result.Action}: running={result.IsRunning}");
        if (!string.IsNullOrWhiteSpace(result.Direction)) {
            writer.WriteLine($"- Direction: {result.Direction}");
        }
        if (result.ImageCount.HasValue) {
            writer.WriteLine($"- ImageCount: {result.ImageCount.Value}");
        }

        return 0;
    }
}
