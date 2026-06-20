using DesktopManager.App.Core;

namespace DesktopManager.App;

internal static class HotkeyDiagnosticsReader {
    public static HotkeyDiagnosticSummary ReadLatest(HotkeyFunctionDefinition function) {
        if (function == null) {
            throw new ArgumentNullException(nameof(function));
        }

        string directory = GetDiagnosticDirectory();
        if (!Directory.Exists(directory)) {
            return NotFound($"No diagnostics directory found at {directory}.");
        }

        foreach (FileInfo file in new DirectoryInfo(directory)
                     .EnumerateFiles("hotkeys-*.jsonl")
                     .OrderByDescending(item => item.LastWriteTimeUtc)) {
            foreach (string line in ReadLinesReverse(file.FullName)) {
                if (HotkeyDiagnosticLineParser.TryParse(line, function.Hotkey, function.Name, out HotkeyDiagnosticSummary summary)) {
                    summary.Path = file.FullName;
                    return summary;
                }
            }
        }

        return NotFound($"No diagnostics found for {function.Name} ({function.Hotkey}).");
    }

    private static HotkeyDiagnosticSummary NotFound(string message) {
        return new HotkeyDiagnosticSummary {
            Found = false,
            Summary = message
        };
    }

    private static IEnumerable<string> ReadLinesReverse(string path) {
        string[] lines = File.ReadAllLines(path);
        for (int i = lines.Length - 1; i >= 0; i--) {
            yield return lines[i];
        }
    }

    private static string GetDiagnosticDirectory() {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "Evotec", "DesktopManager", "Diagnostics");
    }
}
