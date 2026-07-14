using System;
using System.IO;
using System.Text;

namespace DesktopManager;

/// <summary>
/// Writes files through a flushed temporary file and atomically replaces the destination.
/// </summary>
internal static class AtomicFileWriter {
    /// <summary>
    /// Writes UTF-8 text without exposing a partially written destination file.
    /// </summary>
    internal static void WriteAllText(string path, string contents) {
        Write(path, stream => {
            using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 4096, leaveOpen: true);
            writer.Write(contents);
            writer.Flush();
        });
    }

    /// <summary>
    /// Writes a file without exposing a partially written destination file.
    /// </summary>
    internal static void Write(string path, Action<Stream> write) {
        if (string.IsNullOrWhiteSpace(path)) {
            throw new ArgumentException("A destination path is required.", nameof(path));
        }
        if (write == null) {
            throw new ArgumentNullException(nameof(write));
        }

        string fullPath = Path.GetFullPath(path);
        string? directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory)) {
            Directory.CreateDirectory(directory);
        }

        string tempPath = Path.Combine(
            directory ?? Path.GetTempPath(),
            $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        try {
            using (FileStream stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
                write(stream);
                stream.Flush(flushToDisk: true);
            }

            ReplaceDestination(tempPath, fullPath);
        } finally {
            if (File.Exists(tempPath)) {
                File.Delete(tempPath);
            }
        }
    }

    private static void ReplaceDestination(string tempPath, string destinationPath) {
        if (File.Exists(destinationPath)) {
            File.Replace(tempPath, destinationPath, null);
            return;
        }

        try {
            File.Move(tempPath, destinationPath);
        } catch (IOException) when (File.Exists(destinationPath)) {
            File.Replace(tempPath, destinationPath, null);
        }
    }
}
