using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace DesktopManager.App.Core;

/// <summary>
/// Reads and writes DesktopManager hotkey profile JSON documents.
/// </summary>
public static class HotkeyProfileStore {
    private const string OrganizationFolder = "Evotec";
    private const string ProductFolder = "DesktopManager";
    private const string HotkeysFolder = "Hotkeys";
    private const string ProfileFileName = "profile.json";
    private static readonly JsonSerializerOptions JsonOptions = new() {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
    private static readonly HotkeyProfileJsonContext JsonContext = new(JsonOptions);

    /// <summary>
    /// Gets the default per-user profile path.
    /// </summary>
    /// <returns>The default hotkey profile path under AppData.</returns>
    public static string GetDefaultProfilePath() {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, OrganizationFolder, ProductFolder, HotkeysFolder, ProfileFileName);
    }

    /// <summary>
    /// Loads an existing profile or creates and saves the default profile.
    /// </summary>
    /// <param name="path">Profile JSON path.</param>
    /// <returns>The loaded or newly created profile.</returns>
    public static HotkeyProfile LoadOrCreate(string path) {
        if (string.IsNullOrWhiteSpace(path)) {
            throw new ArgumentException("Profile path cannot be empty.", nameof(path));
        }

        if (!File.Exists(path)) {
            HotkeyProfile defaultProfile = HotkeyProfileDefaults.CreateDefaultProfile();
            Save(path, defaultProfile);
            return defaultProfile;
        }

        using FileStream stream = File.OpenRead(path);
        HotkeyProfile? profile = JsonSerializer.Deserialize(stream, JsonContext.HotkeyProfile);
        if (profile == null) {
            throw new InvalidDataException("DesktopManager hotkey profile JSON did not contain a profile document.");
        }

        return profile;
    }

    /// <summary>
    /// Saves a hotkey profile as source-generated JSON.
    /// </summary>
    /// <param name="path">Profile JSON path.</param>
    /// <param name="profile">Profile to save.</param>
    public static void Save(string path, HotkeyProfile profile) {
        if (string.IsNullOrWhiteSpace(path)) {
            throw new ArgumentException("Profile path cannot be empty.", nameof(path));
        }

        if (profile == null) {
            throw new ArgumentNullException(nameof(profile));
        }

        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) {
            Directory.CreateDirectory(directory);
        }

        using FileStream stream = File.Create(path);
        JsonSerializer.Serialize(stream, profile, JsonContext.HotkeyProfile);
    }
}
