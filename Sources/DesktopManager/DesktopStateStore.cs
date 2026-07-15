using System;
using System.IO;
using System.Linq;

namespace DesktopManager;

/// <summary>
/// Provides naming and storage conventions for DesktopManager state files and captures.
/// </summary>
public static class DesktopStateStore {
    private static readonly HashSet<string> ReservedDeviceNames = new(StringComparer.OrdinalIgnoreCase) {
        "CON",
        "PRN",
        "AUX",
        "NUL",
        "COM1",
        "COM2",
        "COM3",
        "COM4",
        "COM5",
        "COM6",
        "COM7",
        "COM8",
        "COM9",
        "LPT1",
        "LPT2",
        "LPT3",
        "LPT4",
        "LPT5",
        "LPT6",
        "LPT7",
        "LPT8",
        "LPT9"
    };

    /// <summary>
    /// Gets the captures directory.
    /// </summary>
    /// <returns>The captures directory path.</returns>
    public static string GetCapturesDirectory() {
        string directory = GetCategoryDirectory("captures");
        Directory.CreateDirectory(directory);
        return directory;
    }

    /// <summary>
    /// Gets the path for a named layout.
    /// </summary>
    /// <param name="name">Layout name.</param>
    /// <returns>The full layout path.</returns>
    public static string GetLayoutPath(string name) {
        return GetNamedPath("layouts", name);
    }

    /// <summary>
    /// Gets the path for a named snapshot.
    /// </summary>
    /// <param name="name">Snapshot name.</param>
    /// <returns>The full snapshot path.</returns>
    public static string GetSnapshotPath(string name) {
        return GetNamedPath("snapshots", name);
    }

    /// <summary>
    /// Gets the path for a named window target.
    /// </summary>
    /// <param name="name">Target name.</param>
    /// <returns>The full target path.</returns>
    public static string GetTargetPath(string name) {
        return GetNamedPath("targets", name);
    }

    /// <summary>
    /// Gets the path for a named control target.
    /// </summary>
    /// <param name="name">Control target name.</param>
    /// <returns>The full target path.</returns>
    public static string GetControlTargetPath(string name) {
        return GetNamedPath("control-targets", name);
    }

    /// <summary>
    /// Gets the path for a named visual baseline metadata file.
    /// </summary>
    /// <param name="name">Visual baseline name.</param>
    /// <returns>The full metadata path.</returns>
    public static string GetVisualBaselinePath(string name) {
        return GetNamedPath("visual-baselines", name);
    }

    /// <summary>
    /// Gets the PNG image path for a named visual baseline.
    /// </summary>
    /// <param name="name">Visual baseline name.</param>
    /// <returns>The full image path.</returns>
    public static string GetVisualBaselineImagePath(string name) {
        return GetNamedImagePath("visual-baselines", name);
    }

    /// <summary>
    /// Lists stored names for a given category.
    /// </summary>
    /// <param name="category">Storage category.</param>
    /// <returns>The stored names.</returns>
    public static IReadOnlyList<string> ListNames(string category) {
        return ListNames(category, "*.json");
    }

    /// <summary>
    /// Lists stored names for a given category and file pattern.
    /// </summary>
    /// <param name="category">Storage category.</param>
    /// <param name="searchPattern">Search pattern such as <c>*.json</c> or <c>*.png</c>.</param>
    /// <returns>The stored names.</returns>
    public static IReadOnlyList<string> ListNames(string category, string searchPattern) {
        string directory = GetCategoryDirectory(category);
        if (!Directory.Exists(directory)) {
            return Array.Empty<string>();
        }

        return Directory.GetFiles(directory, searchPattern)
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Resolves a PNG output path for a screenshot capture.
    /// </summary>
    /// <param name="prefix">Default file name prefix.</param>
    /// <param name="outputPath">Optional caller-provided path.</param>
    /// <returns>The full output path.</returns>
    public static string ResolveCapturePath(string prefix, string? outputPath) {
        string path = string.IsNullOrWhiteSpace(outputPath)
            ? Path.Combine(GetCapturesDirectory(), $"{ValidateName(prefix)}-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}.png")
            : outputPath!;

        string extension = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(extension)) {
            path += ".png";
        } else if (!extension.Equals(".png", StringComparison.OrdinalIgnoreCase)) {
            path = Path.ChangeExtension(path, ".png");
        }

        string fullPath = Path.GetFullPath(path);
        string? directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
        }

        return fullPath;
    }

    private static string GetNamedPath(string category, string name) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException("A name is required.", nameof(name));
        }

        string validated = ValidateName(name);
        string directory = GetCategoryDirectory(category);
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, validated + ".json");
    }

    private static string GetNamedImagePath(string category, string name) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException("A name is required.", nameof(name));
        }

        string validated = ValidateName(name);
        string directory = GetCategoryDirectory(category);
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, validated + ".png");
    }

    private static string GetCategoryDirectory(string category) {
        string root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DesktopManager");
        return Path.Combine(root, ValidateName(category));
    }

    internal static string ValidateName(string name) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException("A name is required.", nameof(name));
        }

        if (!string.Equals(name, name.Trim(), StringComparison.Ordinal) || name.EndsWith(".", StringComparison.Ordinal)) {
            throw new ArgumentException("Names cannot start or end with whitespace or end with a period.", nameof(name));
        }

        if (name == "." || name == "..") {
            throw new ArgumentException("Relative path segments are not valid names.", nameof(name));
        }

        const string invalidCharacters = "<>:\"/\\|?*";
        foreach (char character in name) {
            if (character < 32 || invalidCharacters.IndexOf(character) >= 0) {
                throw new ArgumentException($"The name '{name}' contains an invalid file-name character.", nameof(name));
            }
        }

        int extensionSeparator = name.IndexOf('.');
        string deviceStem = extensionSeparator < 0 ? name : name.Substring(0, extensionSeparator);
        if (ReservedDeviceNames.Contains(deviceStem)) {
            throw new ArgumentException($"The name '{name}' uses a reserved Windows device name.", nameof(name));
        }

        return name;
    }
}
