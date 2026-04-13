using System;
using System.IO;
using System.Text.Json;

namespace DesktopManager;

/// <summary>
/// Provides storage for personalization snapshots.
/// </summary>
public static class PersonalizationStateStore {
    private static readonly JsonSerializerOptions SerializerOptions = new() {
        WriteIndented = true
    };

    /// <summary>
    /// Gets the base directory for personalization snapshots.
    /// </summary>
    /// <returns>The snapshot directory.</returns>
    public static string GetSnapshotsDirectory() {
        string directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "DesktopManager",
            "Personalization");
        Directory.CreateDirectory(directory);
        return directory;
    }

    /// <summary>
    /// Gets the snapshot file path for the given name.
    /// </summary>
    /// <param name="name">Snapshot name.</param>
    /// <returns>The full file path.</returns>
    public static string GetSnapshotPath(string name) {
        if (string.IsNullOrWhiteSpace(name)) {
            throw new ArgumentException("A name is required.", nameof(name));
        }

        string sanitized = SanitizeName(name);
        string directory = GetSnapshotsDirectory();
        return Path.Combine(directory, sanitized + ".json");
    }

    /// <summary>
    /// Saves the snapshot to disk.
    /// </summary>
    /// <param name="name">Snapshot name.</param>
    /// <param name="snapshot">Snapshot to store.</param>
    public static void SaveSnapshot(string name, PersonalizationSnapshot snapshot) {
        if (snapshot == null) {
            throw new ArgumentNullException(nameof(snapshot));
        }

        string path = GetSnapshotPath(name);
        string json = JsonSerializer.Serialize(snapshot, SerializerOptions);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Loads a snapshot from disk.
    /// </summary>
    /// <param name="name">Snapshot name.</param>
    /// <returns>The loaded snapshot.</returns>
    public static PersonalizationSnapshot LoadSnapshot(string name) {
        string path = GetSnapshotPath(name);
        if (!File.Exists(path)) {
            throw new FileNotFoundException("Personalization snapshot not found.", path);
        }

        string json = File.ReadAllText(path);
        PersonalizationSnapshot? snapshot = JsonSerializer.Deserialize<PersonalizationSnapshot>(json, SerializerOptions);
        if (snapshot == null) {
            throw new InvalidOperationException("Unable to deserialize personalization snapshot.");
        }

        return snapshot;
    }

    private static string SanitizeName(string name) {
        char[] invalid = Path.GetInvalidFileNameChars();
        var buffer = new System.Collections.Generic.List<char>(name.Length);
        foreach (char character in name.Trim()) {
            if (Array.IndexOf(invalid, character) >= 0) {
                continue;
            }

            buffer.Add(character);
        }

        string sanitized = new string(buffer.ToArray()).TrimEnd(' ', '.');
        if (string.IsNullOrWhiteSpace(sanitized)) {
            throw new ArgumentException($"The name '{name}' does not produce a valid file name.", nameof(name));
        }

        return sanitized;
    }
}
