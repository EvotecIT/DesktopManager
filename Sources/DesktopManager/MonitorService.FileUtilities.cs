using System;
using System.IO;

namespace DesktopManager;

public partial class MonitorService {
    private static string WriteStreamToTempFile(Stream stream) {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try {
            using FileStream fs = File.Create(path);
            stream.CopyTo(fs);
            return path;
        } catch {
            DeleteTempFile(path);
            throw;
        }
    }

    private static void DeleteTempFile(string path) {
        try {
            File.Delete(path);
        } catch (Exception ex) {
            Console.WriteLine($"DeleteTempFile failed: {ex.Message}");
        }
    }
}
