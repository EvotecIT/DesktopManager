using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;

namespace DesktopManager;

/// <summary>
/// Helper methods for persisting and retrieving a list of recently used
/// wallpaper paths. The history is stored as JSON in the user's
/// ApplicationData folder.
/// </summary>
public static class WallpaperHistory
{
    private static readonly object _lock = new();
    private const int MutexWaitTimeoutMilliseconds = 5000;

    private static string DefaultHistoryFilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DesktopManager",
            "wallpaper-history.json");

    private static string HistoryFilePath =>
        GetHistoryFilePath();

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

    /// <summary>Maximum number of entries persisted in history.</summary>
    private const int MaxEntries = 50;

    /// <summary>
    /// Reads the wallpaper history from the persistent storage.
    /// </summary>
    /// <returns>A list of wallpaper paths with the most recent entry first.</returns>
    /// <example>
    ///   <code>Get-WallpaperHistory</code>
    /// </example>
    public static List<string> GetHistory()
    {
        return ExecuteWithHistoryFileLock(ReadHistoryCore);
    }

    /// <summary>
    /// Replaces the current history with the specified collection of paths.
    /// </summary>
    /// <param name="paths">Collection of wallpaper file paths to persist.</param>
    public static void SetHistory(IEnumerable<string> paths)
    {
        ExecuteWithHistoryFileLock(historyFilePath => {
            WriteHistoryCore(historyFilePath, paths);
            return 0;
        });
    }

    /// <summary>
    /// Adds the specified path to the history placing it at the top.
    /// If the path already exists it is moved to the first position.
    /// The history is truncated to <see cref="MaxEntries"/> items.
    /// </summary>
    /// <param name="path">The wallpaper file path to record.</param>
    /// <example>
    ///   <code>Add-WallpaperHistory -Path "C:\\wallpaper.jpg"</code>
    /// </example>
    public static void AddEntry(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        ExecuteWithHistoryFileLock(historyFilePath => {
            var history = ReadHistoryCore(historyFilePath);
            history.Remove(path);
            history.Insert(0, path);
            if (history.Count > MaxEntries)
            {
                history.RemoveRange(MaxEntries, history.Count - MaxEntries);
            }

            WriteHistoryCore(historyFilePath, history);
            return 0;
        });
    }

    private static List<string> ReadHistoryCore(string historyFilePath)
    {
        if (!File.Exists(historyFilePath))
        {
            return new List<string>();
        }

        try
        {
            using FileStream stream = new FileStream(historyFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var history = JsonSerializer.Deserialize<List<string>>(stream);
            return history ?? new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }

    private static void WriteHistoryCore(string historyFilePath, IEnumerable<string> paths) {
        string json = JsonSerializer.Serialize(paths);
        AtomicFileWriter.WriteAllText(historyFilePath, json);
    }

    private static T ExecuteWithHistoryFileLock<T>(Func<string, T> action)
    {
        string historyFilePath = HistoryFilePath;
        lock (_lock)
        {
            using Mutex mutex = OpenHistoryFileMutex(historyFilePath);
            try
            {
                if (!mutex.WaitOne(MutexWaitTimeoutMilliseconds))
                {
                    throw new IOException($"Timed out waiting for wallpaper history access to '{historyFilePath}'.");
                }
            }
            catch (AbandonedMutexException)
            {
                // Continue; ownership is granted to the current process.
            }

            try
            {
                return action(historyFilePath);
            }
            finally
            {
                try
                {
                    mutex.ReleaseMutex();
                }
                catch (ApplicationException)
                {
                }
            }
        }
    }

    private static Mutex OpenHistoryFileMutex(string historyFilePath)
    {
        using MD5 md5 = MD5.Create();
        byte[] hashBytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(historyFilePath));
        string hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty);
        return new Mutex(false, @"Global\DesktopManager.WallpaperHistory." + hash);
    }
}
