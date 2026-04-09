namespace DesktopManager.WinUiHarness;

public sealed class WinUiHarnessOptions {
    public string Title { get; init; } = "DesktopManager WinUI Harness";

    public string InitialText { get; init; } = "seed";

    public string? StatusFilePath { get; init; }

    public static WinUiHarnessOptions Parse(IReadOnlyList<string> args) {
        var options = new WinUiHarnessOptions();
        if (args == null || args.Count == 0) {
            return options;
        }

        string title = options.Title;
        string initialText = options.InitialText;
        string? statusFilePath = options.StatusFilePath;

        for (int index = 0; index < args.Count; index++) {
            string argument = args[index];
            if (string.Equals(argument, "--title", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Count) {
                title = args[++index];
                continue;
            }

            if (string.Equals(argument, "--text", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Count) {
                initialText = args[++index];
                continue;
            }

            if (string.Equals(argument, "--status-file", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Count) {
                statusFilePath = args[++index];
            }
        }

        return new WinUiHarnessOptions {
            Title = string.IsNullOrWhiteSpace(title) ? options.Title : title,
            InitialText = initialText ?? options.InitialText,
            StatusFilePath = string.IsNullOrWhiteSpace(statusFilePath) ? null : statusFilePath
        };
    }
}
