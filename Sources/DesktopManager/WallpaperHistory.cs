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

    private static string? _historyFilePath;

    private static string DefaultHistoryFilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DesktopManager",
            "wallpaper-history.json");

    private static string HistoryFilePath =>
        _historyFilePath ??= GetHistoryFilePath();

    private static string GetHistoryFilePath()
    {
        string? envPath = Environment.GetEnvironmentVariable("DESKTOPMANAGER_HISTORY_PATH");
        if (!string.IsNullOrEmpty(envPath))
        {
            try
            {
                string? dir = Path.GetDirectoryName(envPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    string testFile = Path.Combine(dir, Path.GetRandomFileName());
                    using (FileStream _ = File.Create(testFile, 1, FileOptions.DeleteOnClose))
                    {
                    }

                    return envPath;
                }
            }
            catch
            {
                // Ignore and fall back to default path
            }
        }

        return DefaultHistoryFilePath;
    }

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
            SetHistory(history);
        }
    }
}
