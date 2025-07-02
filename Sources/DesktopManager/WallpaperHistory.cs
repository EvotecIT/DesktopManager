using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DesktopManager;

/// <summary>
/// Helper methods for persisting and retrieving a list of recently used
/// wallpaper paths. The history is stored as JSON in the user's
/// ApplicationData folder.
/// </summary>
public static class WallpaperHistory
{
    private static readonly object _lock = new();

    private static string HistoryFilePath =>
        Environment.GetEnvironmentVariable("DESKTOPMANAGER_HISTORY_PATH") ??
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DesktopManager", "wallpaper-history.json");

    /// <summary>Maximum number of entries persisted in history.</summary>
    private const int MaxEntries = 50;

    /// <summary>
    /// Reads the wallpaper history from the persistent storage.
    /// </summary>
    /// <returns>A list of wallpaper paths with the most recent entry first.</returns>
    public static List<string> GetHistory()
    {
        lock (_lock)
        {
            if (!File.Exists(HistoryFilePath))
            {
                return new List<string>();
            }
            try
            {
                string json = File.ReadAllText(HistoryFilePath);
                var history = JsonSerializer.Deserialize<List<string>>(json);
                return history ?? new List<string>();
            }
            catch (JsonException)
            {
                return new List<string>();
            }
        }
    }

    /// <summary>
    /// Replaces the current history with the specified collection of paths.
    /// </summary>
    /// <param name="paths">Collection of wallpaper file paths to persist.</param>
    public static void SetHistory(IEnumerable<string> paths)
    {
        lock (_lock)
        {
            string? dir = Path.GetDirectoryName(HistoryFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            string json = JsonSerializer.Serialize(paths);
            File.WriteAllText(HistoryFilePath, json);
        }
    }

    /// <summary>
    /// Adds the specified path to the history placing it at the top.
    /// If the path already exists it is moved to the first position.
    /// The history is truncated to <see cref="MaxEntries"/> items.
    /// </summary>
    /// <param name="path">The wallpaper file path to record.</param>
    public static void AddEntry(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }
        lock (_lock)
        {
            var history = GetHistory();
            history.Remove(path);
            history.Insert(0, path);
            if (history.Count > MaxEntries)
            {
                history.RemoveRange(MaxEntries, history.Count - MaxEntries);
            }
            SetHistory(history);
        }
    }
}
