using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DesktopManager;

public static class WallpaperHistory
{
    private static readonly object _lock = new();

    private static string HistoryFilePath =>
        Environment.GetEnvironmentVariable("DESKTOPMANAGER_HISTORY_PATH") ??
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DesktopManager", "wallpaper-history.json");

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
