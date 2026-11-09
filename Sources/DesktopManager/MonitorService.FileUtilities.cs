using System;
using System.IO;

namespace DesktopManager;

public partial class MonitorService {
    private static void SetTemporaryWallpaper(Stream stream, Action<string> operation) {
        string temp = WriteStreamToTempFile(stream);
        // Transfer the input to the request: cancellation retires it before execution,
        // while an executing request retains it even if its caller's wait throws.
        WallpaperComApartment.Invoke(() => {
            operation(temp);
            return true;
        }, operationCleanup: () => DeleteTempFile(temp));
    }

    private static string WriteStreamToTempFile(Stream stream) {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        long? originalPosition = null;
        if (stream.CanSeek) {
            originalPosition = stream.Position;
            stream.Position = 0;
        }
        try {
            using FileStream fs = File.Create(path);
            stream.CopyTo(fs);
            return path;
        } catch {
            DeleteTempFile(path);
            throw;
        } finally {
            if (originalPosition.HasValue) {
                stream.Position = originalPosition.Value;
            }
        }
    }

    private static void DeleteTempFile(string path) {
        try {
            File.Delete(path);
        } catch (Exception ex) {
            DesktopManagerDiagnostics.Report($"DeleteTempFile failed: {ex.Message}");
        }
    }
}
