using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        string directory = Path.GetDirectoryName(DesktopStateStore.GetPersonalizationSnapshotPath("snapshot"))!;
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

        return DesktopStateStore.GetPersonalizationSnapshotPath(name);
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
        AtomicFileWriter.WriteAllText(path, json);
    }

    /// <summary>
    /// Loads a snapshot from disk.
    /// </summary>
    /// <param name="name">Snapshot name.</param>
    /// <returns>The loaded snapshot.</returns>
    public static PersonalizationSnapshot LoadSnapshot(string name) {
        string path = GetSnapshotPath(name);
        if (!File.Exists(path)) {
            string legacyPath = GetLegacySnapshotPath(name);
            if (!File.Exists(legacyPath)) {
                throw new FileNotFoundException("Personalization snapshot not found.", path);
            }
            path = legacyPath;
        }

        string json = File.ReadAllText(path);
        PersonalizationSnapshot? snapshot = JsonSerializer.Deserialize<PersonalizationSnapshot>(json, SerializerOptions);
        if (snapshot == null) {
            throw new InvalidOperationException("Unable to deserialize personalization snapshot.");
        }

        return snapshot;
    }

    /// <summary>Lists stored personalization snapshot names.</summary>
    /// <returns>The stored names.</returns>
    public static IReadOnlyList<string> ListSnapshots() {
        IEnumerable<string> currentNames = Directory.GetFiles(GetSnapshotsDirectory(), "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrWhiteSpace(name))!;
        IEnumerable<string> legacyNames = GetLegacySnapshotNames();
        return currentNames
            .Concat(legacyNames)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray()!;
    }

    /// <summary>Deletes a stored personalization snapshot.</summary>
    /// <param name="name">The snapshot name.</param>
    /// <returns><c>true</c> when a snapshot was deleted.</returns>
    public static bool DeleteSnapshot(string name) {
        string path = GetSnapshotPath(name);
        bool deleted = false;
        if (File.Exists(path)) {
            File.Delete(path);
            deleted = true;
        }
        string legacyPath = GetLegacySnapshotPath(name);
        if (File.Exists(legacyPath)) {
            File.Delete(legacyPath);
            deleted = true;
        }
        return deleted;
    }

    private static IEnumerable<string> GetLegacySnapshotNames() {
        string directory = GetLegacySnapshotsDirectory();
        if (!Directory.Exists(directory)) {
            return Array.Empty<string>();
        }

        try {
            return Directory.GetFiles(directory, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToArray()!;
        } catch (UnauthorizedAccessException) {
            return Array.Empty<string>();
        }
    }

    private static string GetLegacySnapshotPath(string name) {
        string validated = DesktopStateStore.ValidateName(name);
        return Path.Combine(GetLegacySnapshotsDirectory(), validated + ".json");
    }

    private static string GetLegacySnapshotsDirectory() {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "DesktopManager",
            "Personalization");
    }

}
