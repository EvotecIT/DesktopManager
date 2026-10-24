using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace DesktopManager;

internal sealed partial class LinuxDesktopBackend {
    private DesktopDisplayInfo? TryReadQualifiedX11Display() {
        try {
            return ReadQualifiedX11Display();
        } catch {
            // Geometry is optional for target discovery. GetDisplays keeps native
            // query failures observable; other operations never invent dimensions.
            return null;
        }
    }

    private DesktopDisplayInfo? ReadQualifiedX11Display() {
        if (_backend == "unsupported" || IsWayland || !GetSession().HasGraphicalSession) { return null; }
        var executable = _runner.FindExecutable("xrandr");
        if (executable == null) { return null; }
        return ParseSingleX11Display(_runner.Run(executable, "--current", "--verbose"));
    }

    /// <summary>Qualifies a single origin-aligned native mode without rotation, panning or an XRandR transform.</summary>
    private static DesktopDisplayInfo? ParseSingleX11Display(string output) {
        var lines = output.Replace("\r", string.Empty).Split('\n');
        var connected = lines.Where(line => Regex.IsMatch(line, @"^\S+ connected(?:\s|$)")).ToArray();
        if (connected.Length != 1) { return null; }
        var header = Regex.Match(connected[0],
            @"^(?<name>\S+) connected (?<primary>primary )?(?<width>[1-9]\d*)x(?<height>[1-9]\d*)\+0\+0(?: \(0x[0-9a-fA-F]+\))? normal(?:\s+\(|$)");
        var screen = Regex.Match(output, @"\bcurrent ([1-9]\d*) x ([1-9]\d*)\b");
        if (!header.Success || !screen.Success ||
            !int.TryParse(header.Groups["width"].Value, out var width) ||
            !int.TryParse(header.Groups["height"].Value, out var height) ||
            screen.Groups[1].Value != header.Groups["width"].Value || screen.Groups[2].Value != header.Groups["height"].Value) {
            return null;
        }

        var connectedIndex = Array.IndexOf(lines, connected[0]);
        var nextOutput = Array.FindIndex(lines, connectedIndex + 1,
            line => Regex.IsMatch(line, @"^\S+ (?:connected|disconnected|unknown connection)(?:\s|$)"));
        lines = lines.Skip(connectedIndex).Take((nextOutput < 0 ? lines.Length : nextOutput) - connectedIndex).ToArray();
        if (lines.Any(line => Regex.IsMatch(line, @"^\s+non-desktop:\s+1\s*$"))) { return null; }

        var transform = Array.FindIndex(lines, line => line.TrimStart().StartsWith("Transform:", StringComparison.Ordinal));
        if (transform < 0 || transform + 2 >= lines.Length ||
            lines.Any(line => line.TrimStart().StartsWith("Panning", StringComparison.OrdinalIgnoreCase))) {
            return null;
        }
        var matrix = lines[transform].Substring(lines[transform].IndexOf(':') + 1) + " " + lines[transform + 1] + " " + lines[transform + 2];
        var coefficients = matrix.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (coefficients.Length != 9) { return null; }
        for (var index = 0; index < coefficients.Length; index++) {
            if (!double.TryParse(coefficients[index], NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                value != (index % 4 == 0 ? 1 : 0)) {
                return null;
            }
        }

        var current = lines.Select((line, index) => new { line, index }).Where(item => item.line.Contains("*current")).ToArray();
        if (current.Length != 1 || current[0].index + 2 >= lines.Length) { return null; }
        var nativeWidth = Regex.Match(lines[current[0].index + 1], @"^\s+h: width\s+([1-9]\d*)\b");
        var nativeHeight = Regex.Match(lines[current[0].index + 2], @"^\s+v: height\s+([1-9]\d*)\b");
        if (!nativeWidth.Success || !nativeHeight.Success || nativeWidth.Groups[1].Value != header.Groups["width"].Value ||
            nativeHeight.Groups[1].Value != header.Groups["height"].Value) {
            return null;
        }

        return new DesktopDisplayInfo {
            Id = "x11:" + header.Groups["name"].Value,
            DeviceName = header.Groups["name"].Value,
            IsPrimary = true,
            Width = width,
            Height = height
        };
    }
}
