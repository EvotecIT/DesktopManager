using System;
using System.IO;

namespace DesktopManager;

public partial class MonitorService {
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
