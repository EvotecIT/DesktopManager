using System;
using System.Threading;

namespace DesktopManager.Cli;

internal static class SystemCommands {
    public static int Run(string action, CommandLineArguments arguments) {
        return action switch {
            "power" => Power(),
            "session" => Session(),
            "lock" => Lock(),
            "keep-awake" => KeepAwake(arguments),
            "suspend" => Suspend(arguments),
            "sign-out" => SignOut(arguments),
            _ => throw new CommandLineException($"Unknown system command '{action}'.")
        };
    }

    private static int Power() {
        OutputFormatter.WriteJson(new SystemPowerService().GetStatus());
        return 0;
    }

    private static int Session() {
        OutputFormatter.WriteJson(new DesktopSessionService().GetCurrentSession());
        return 0;
    }

    private static int Lock() {
        new SystemPowerService().LockWorkstation();
        return 0;
    }

    private static int KeepAwake(CommandLineArguments arguments) {
        int seconds = arguments.GetRequiredIntOption("seconds");
        if (seconds <= 0 || seconds > 86400) {
            throw new CommandLineException("Option '--seconds' must be between 1 and 86400.");
        }
        KeepAwakeOptions options = KeepAwakeOptions.System;
        if (arguments.GetBoolFlag("display")) {
            options |= KeepAwakeOptions.Display;
        }
        if (arguments.GetBoolFlag("away-mode")) {
            options |= KeepAwakeOptions.AwayMode;
        }

        using (new SystemPowerService().CreateKeepAwakeLease(options)) {
            Thread.Sleep(TimeSpan.FromSeconds(seconds));
        }
        return 0;
    }

    private static int Suspend(CommandLineArguments arguments) {
        RequireConfirmation(arguments, "suspend");
        new SystemPowerService().Suspend(arguments.GetBoolFlag("hibernate"), arguments.GetBoolFlag("force"));
        return 0;
    }

    private static int SignOut(CommandLineArguments arguments) {
        RequireConfirmation(arguments, "sign out");
        new SystemPowerService().SignOut(arguments.GetBoolFlag("force"));
        return 0;
    }

    private static void RequireConfirmation(CommandLineArguments arguments, string operation) {
        if (!arguments.GetBoolFlag("confirm")) {
            throw new CommandLineException($"The {operation} operation requires '--confirm'.");
        }
    }
}
