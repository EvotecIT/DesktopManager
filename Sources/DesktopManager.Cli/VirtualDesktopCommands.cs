using System;

namespace DesktopManager.Cli;

internal static class VirtualDesktopCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        IntPtr handle = DesktopHandleParser.Parse(arguments.GetRequiredOption("handle"));
        using var service = new VirtualDesktopService();
        return action switch {
            "current" => Write(new { handle, onCurrentDesktop = service.IsWindowOnCurrentDesktop(handle) }),
            "id" => Write(new { handle, desktopId = service.GetWindowDesktopId(handle) }),
            "move" => Move(service, handle, arguments),
            _ => throw new CommandLineException($"Unknown virtual-desktop command '{action}'.")
        };
    }

    private static int Move(VirtualDesktopService service, IntPtr handle, CommandLineArguments arguments) {
        if (!Guid.TryParse(arguments.GetRequiredOption("desktop-id"), out Guid desktopId)) {
            throw new CommandLineException("Option '--desktop-id' must be a valid GUID.");
        }
        service.MoveWindowToDesktop(handle, desktopId);
        return Write(new { handle, desktopId, moved = true });
    }

    private static int Write(object value) {
        OutputFormatter.WriteJson(value);
        return 0;
    }
}
