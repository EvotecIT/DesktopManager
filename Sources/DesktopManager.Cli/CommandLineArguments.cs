using System;
using System.Collections.Generic;
using System.Globalization;

namespace DesktopManager.Cli;

internal sealed class CommandLineArguments {
    private readonly List<string> _commandParts;
    private readonly Dictionary<string, List<string>> _options;

    private CommandLineArguments(List<string> commandParts, Dictionary<string, List<string>> options) {
        _commandParts = commandParts;
        _options = options;
    }

    public bool IsEmpty => _commandParts.Count == 0 && _options.Count == 0;

    public static CommandLineArguments Parse(string[] args) {
        var commandParts = new List<string>();
        var options = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < args.Length; index++) {
            string current = args[index];
            if (!current.StartsWith("--", StringComparison.Ordinal)) {
                commandParts.Add(current);
                continue;
            }

            if (current.Length <= 2) {
                throw new CommandLineException("Encountered an empty option name.");
            }

            string optionName = current.Substring(2);
            string? value = null;
            if (index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal)) {
                value = args[index + 1];
                index++;
            }

            if (!options.TryGetValue(optionName, out List<string>? values)) {
                values = new List<string>();
                options[optionName] = values;
            }

            values.Add(value ?? "true");
        }

        return new CommandLineArguments(commandParts, options);
    }

    public string? GetCommandPart(int index) {
        if (index < 0 || index >= _commandParts.Count) {
            return null;
        }

        return _commandParts[index];
    }

    public bool HasFlag(string name) {
        return _options.ContainsKey(name);
    }

    public string? GetOption(string name) {
        if (!_options.TryGetValue(name, out List<string>? values) || values.Count == 0) {
            return null;
        }

        return values[values.Count - 1];
    }

    public IReadOnlyList<string> GetOptions(string name) {
        if (!_options.TryGetValue(name, out List<string>? values) || values.Count == 0) {
            return Array.Empty<string>();
        }

        return values.ToArray();
    }

    public string GetRequiredOption(string name) {
        return GetOption(name) ?? throw new CommandLineException($"Missing required option '--{name}'.");
    }

    public string GetRequiredCommandPart(int index, string description) {
        return GetCommandPart(index) ?? throw new CommandLineException($"Missing required {description}.");
    }

    public int? GetIntOption(string name) {
        string? value = GetOption(name);
        if (string.IsNullOrEmpty(value)) {
            return null;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)) {
            return parsed;
        }

        throw new CommandLineException($"Option '--{name}' expects an integer value.");
    }

    public int GetRequiredIntOption(string name) {
        return GetIntOption(name) ?? throw new CommandLineException($"Missing required option '--{name}'.");
    }

    public uint? GetUIntOption(string name) {
        string? value = GetOption(name);
        if (string.IsNullOrEmpty(value)) {
            return null;
        }

        if (uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint parsed)) {
            return parsed;
        }

        throw new CommandLineException($"Option '--{name}' expects an unsigned integer value.");
    }

    public double? GetDoubleOption(string name) {
        string? value = GetOption(name);
        if (string.IsNullOrEmpty(value)) {
            return null;
        }

        if (double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double parsed)) {
            return parsed;
        }

        throw new CommandLineException($"Option '--{name}' expects a numeric value.");
    }

    public bool GetBoolFlag(string name) {
        return HasFlag(name);
    }
}
