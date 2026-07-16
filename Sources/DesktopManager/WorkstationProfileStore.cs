using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DesktopManager;

/// <summary>
/// Stores named workstation profiles in the current user's DesktopManager state directory.
/// </summary>
public static class WorkstationProfileStore {
    private static readonly JsonSerializerOptions SerializerOptions = new() {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    /// <summary>Saves or replaces a named workstation profile.</summary>
    /// <param name="name">The profile name.</param>
    /// <param name="profile">The profile to store.</param>
    public static void Save(string name, WorkstationProfile profile) {
        if (profile == null) {
            throw new ArgumentNullException(nameof(profile));
        }

        WorkstationProfileValidator.Validate(profile);
        string json = JsonSerializer.Serialize(profile, SerializerOptions);
        AtomicFileWriter.WriteAllText(DesktopStateStore.GetWorkstationProfilePath(name), json);
    }

    /// <summary>Loads a named workstation profile.</summary>
    /// <param name="name">The profile name.</param>
    /// <returns>The stored profile.</returns>
    public static WorkstationProfile Load(string name) {
        string path = DesktopStateStore.GetWorkstationProfilePath(name);
        if (!File.Exists(path)) {
            throw new FileNotFoundException("Workstation profile not found.", path);
        }

        WorkstationProfile? profile = JsonSerializer.Deserialize<WorkstationProfile>(File.ReadAllText(path), SerializerOptions);
        if (profile == null) {
            throw new InvalidOperationException("Unable to deserialize the workstation profile.");
        }

        WorkstationProfileValidator.Validate(profile);
        return profile;
    }

    /// <summary>Lists stored workstation profile names.</summary>
    /// <returns>The stored profile names.</returns>
    public static IReadOnlyList<string> List() {
        return DesktopStateStore.ListNames("workstation-profiles");
    }

    /// <summary>Deletes a named workstation profile.</summary>
    /// <param name="name">The profile name.</param>
    /// <returns><c>true</c> when a profile was deleted.</returns>
    public static bool Delete(string name) {
        string path = DesktopStateStore.GetWorkstationProfilePath(name);
        if (!File.Exists(path)) {
            return false;
        }
        File.Delete(path);
        return true;
    }
}
