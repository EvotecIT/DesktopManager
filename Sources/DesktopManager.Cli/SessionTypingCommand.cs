using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace DesktopManager.Cli;

/// <summary>Runs <c>window type-session</c> and reads typed text from the command line, standard input or an environment variable.</summary>
internal static class SessionTypingCommand {
    public static int Run(CommandLineArguments arguments) {
        SessionTypingOptions options = CreateOptions(arguments);
        char[] text = ReadText(arguments, Console.In, Environment.GetEnvironmentVariable);
        WindowInfo target;
        SessionTypingResult result;
        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, eventArgs) => {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };
        Console.CancelKeyPress += cancel;
        try {
            target = ResolveSingleWindow(arguments);
            if (options.ForegroundTimeout > TimeSpan.Zero && !options.ActivateWindow) {
                Console.Error.WriteLine($"Click into '{target.Title}' within {options.ForegroundTimeout.TotalSeconds:0} seconds; typing starts when it is the foreground window.");
            }

            result = SessionTypingService.Type(target, text, options, cancellation.Token);
        } finally {
            Console.CancelKeyPress -= cancel;
            Array.Clear(text, 0, text.Length);
        }

        if (arguments.GetBoolFlag("json")) {
            OutputFormatter.WriteJson(new {
                window = target.Title,
                handle = target.Handle,
                profile = result.Profile.ToString(),
                keyboardLayout = result.KeyboardLayout,
                charactersTotal = result.CharactersTotal,
                charactersSent = result.CharactersSent,
                completed = result.Completed,
                abortReason = result.AbortReason.ToString()
            });
        } else if (result.Completed) {
            Console.Out.WriteLine($"Typed {result.CharactersSent} characters into '{target.Title}'.");
        } else {
            Console.Out.WriteLine($"Typing into '{target.Title}' stopped after {result.CharactersSent} of {result.CharactersTotal} characters: {result.AbortReason}.");
        }

        return result.Completed ? 0 : 1;
    }

    internal static SessionTypingOptions CreateOptions(CommandLineArguments arguments) {
        return new SessionTypingOptions {
            Profile = ParseEnum(arguments.GetOption("profile"), SessionTypingProfile.Rdp, "profile"),
            KeyboardLayout = arguments.GetOption("layout"),
            LineBreak = ParseEnum(arguments.GetOption("line-break")?.Replace("-", string.Empty), SessionLineBreakMode.Enter, "line-break"),
            StripLeadingIndentation = arguments.GetBoolFlag("strip-indent"),
            UnmappedCharacters = ParseEnum(arguments.GetOption("unmapped"), UnmappedCharacterPolicy.Fail, "unmapped"),
            CapsLock = ParseEnum(arguments.GetOption("caps-lock")?.Replace("-", string.Empty), CapsLockPolicy.Fail, "caps-lock"),
            ForegroundTimeout = TimeSpan.FromMilliseconds(arguments.GetIntOption("foreground-timeout-ms") ?? 15000),
            ActivateWindow = arguments.GetBoolFlag("activate"),
            StopWhenFocusChanges = !arguments.GetBoolFlag("allow-focus-change"),
            StopOnUserInput = !arguments.GetBoolFlag("ignore-user-input"),
            KeyDelayMilliseconds = arguments.GetIntOption("delay-ms"),
            ModifierHoldMilliseconds = arguments.GetIntOption("modifier-hold-ms"),
            LineDelayMilliseconds = arguments.GetIntOption("line-delay-ms"),
            ChunkSize = arguments.GetIntOption("chunk-size"),
            ChunkPauseMilliseconds = arguments.GetIntOption("chunk-pause-ms")
        };
    }

    /// <summary>
    /// Reads the text from exactly one of <c>--text</c>, <c>--text-stdin</c> or <c>--text-env NAME</c>. Standard input
    /// loses one trailing line break, so a piped password does not end with Enter.
    /// </summary>
    internal static char[] ReadText(CommandLineArguments arguments, TextReader standardInput, Func<string, string?> readEnvironment) {
        string? text = arguments.GetOption("text");
        bool fromStandardInput = arguments.GetBoolFlag("text-stdin");
        string? variable = arguments.GetOption("text-env");
        int sources = (text != null ? 1 : 0) + (fromStandardInput ? 1 : 0) + (variable != null ? 1 : 0);
        if (sources != 1) {
            throw new CommandLineException("Use exactly one of '--text', '--text-stdin' or '--text-env <name>'. Prefer the last two for secrets so they stay off the command line.");
        }

        if (text != null) {
            return text.ToCharArray();
        }

        if (variable != null) {
            if (string.IsNullOrWhiteSpace(variable) || variable == "true") {
                throw new CommandLineException("'--text-env' needs the name of an environment variable.");
            }

            string? value = readEnvironment(variable);
            if (value == null) {
                throw new CommandLineException($"Environment variable '{variable}' is not set.");
            }

            return value.ToCharArray();
        }

        return ReadAll(standardInput);
    }

    private static char[] ReadAll(TextReader reader) {
        char[] buffer = new char[256];
        int length = 0;
        while (true) {
            if (length == buffer.Length) {
                char[] larger = new char[buffer.Length * 2];
                Array.Copy(buffer, larger, length);
                Array.Clear(buffer, 0, buffer.Length);
                buffer = larger;
            }

            int read = reader.Read(buffer, length, buffer.Length - length);
            if (read <= 0) {
                break;
            }

            length += read;
        }

        if (length > 0 && buffer[length - 1] == '\n') {
            length--;
            if (length > 0 && buffer[length - 1] == '\r') {
                length--;
            }
        }

        char[] text = new char[length];
        Array.Copy(buffer, text, length);
        Array.Clear(buffer, 0, buffer.Length);
        return text;
    }

    private static WindowInfo ResolveSingleWindow(CommandLineArguments arguments) {
        WindowSelectionCriteria criteria = WindowCommands.CreateCriteria(arguments, includeEmptyDefault: false);
        criteria.IncludeCloaked = false;
        IReadOnlyList<WindowInfo> windows = new DesktopAutomationService().GetWindows(DesktopOperations.CreateWindowQuery(criteria));
        if (windows.Count != 1) {
            throw new CommandLineException(windows.Count == 0
                ? "No visible window matches the selector."
                : $"{windows.Count} windows match the selector. Text is typed into exactly one window; narrow it with --title, --process or --handle.");
        }

        return windows[0];
    }

    private static T ParseEnum<T>(string? value, T fallback, string option) where T : struct {
        if (string.IsNullOrWhiteSpace(value)) {
            return fallback;
        }

        if (Enum.TryParse(value, ignoreCase: true, out T parsed) && Enum.IsDefined(typeof(T), parsed)) {
            return parsed;
        }

        throw new CommandLineException($"Unsupported value '{value}' for '--{option}'. Use one of: {string.Join(", ", Enum.GetNames(typeof(T)))}.");
    }
}
