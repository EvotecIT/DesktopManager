namespace DesktopManager.App;

internal sealed class RuntimeLogEntry {
    public RuntimeLogEntry(DateTimeOffset timestamp, string message) {
        Timestamp = timestamp;
        Message = message;
    }

    public DateTimeOffset Timestamp { get; }

    public string TimeText => Timestamp.ToString("HH:mm:ss");

    public string Message { get; }
}
