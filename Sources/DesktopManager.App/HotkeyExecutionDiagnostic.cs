namespace DesktopManager.App;

internal sealed class HotkeyExecutionDiagnostic {
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
    public string FunctionName { get; set; } = string.Empty;
    public string Hotkey { get; set; } = string.Empty;
    public string Placement { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Monitor { get; set; } = string.Empty;
    public int? MonitorIndex { get; set; }
    public string RequestedHandle { get; set; } = string.Empty;
    public string ResolvedHandle { get; set; } = string.Empty;
    public int Attempt { get; set; }
    public bool Verified { get; set; }
    public string? Error { get; set; }
    public List<HotkeyWindowSnapshot> Snapshots { get; } = new();

    public void AddSnapshot(string stage, global::DesktopManager.WindowInfo window) {
        Snapshots.Add(HotkeyWindowSnapshot.FromWindow(stage, window));
    }

    public void AddSnapshot(global::DesktopManager.WindowPlacementSnapshot snapshot) {
        Snapshots.Add(HotkeyWindowSnapshot.FromPlacementSnapshot(snapshot));
    }
}

internal sealed class HotkeyRuntimeDiagnostic {
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
    public string EventName { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public string Hotkey { get; set; } = string.Empty;
    public string Placement { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Monitor { get; set; } = string.Empty;
    public int? MonitorIndex { get; set; }
    public string? Message { get; set; }
    public object? Details { get; set; }
}

internal sealed class HotkeyWindowSnapshot {
    public string Stage { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Handle { get; set; } = string.Empty;
    public uint ProcessId { get; set; }
    public string State { get; set; } = string.Empty;
    public int Left { get; set; }
    public int Top { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int MonitorIndex { get; set; }
    public string MonitorDeviceName { get; set; } = string.Empty;
    public bool IsVisible { get; set; }
    public bool IsTopMost { get; set; }

    public static HotkeyWindowSnapshot FromWindow(string stage, global::DesktopManager.WindowInfo window) {
        return new HotkeyWindowSnapshot {
            Stage = stage,
            Title = window.Title,
            Handle = $"0x{window.Handle.ToInt64():X}",
            ProcessId = window.ProcessId,
            State = window.State?.ToString() ?? string.Empty,
            Left = window.Left,
            Top = window.Top,
            Width = window.Width,
            Height = window.Height,
            MonitorIndex = window.MonitorIndex,
            MonitorDeviceName = window.MonitorDeviceName,
            IsVisible = window.IsVisible,
            IsTopMost = window.IsTopMost
        };
    }

    public static HotkeyWindowSnapshot FromPlacementSnapshot(global::DesktopManager.WindowPlacementSnapshot snapshot) {
        return new HotkeyWindowSnapshot {
            Stage = snapshot.Stage,
            Title = snapshot.Title,
            Handle = $"0x{snapshot.Handle.ToInt64():X}",
            ProcessId = snapshot.ProcessId,
            State = snapshot.State?.ToString() ?? string.Empty,
            Left = snapshot.Left,
            Top = snapshot.Top,
            Width = snapshot.Width,
            Height = snapshot.Height,
            MonitorIndex = snapshot.MonitorIndex,
            MonitorDeviceName = snapshot.MonitorDeviceName,
            IsVisible = snapshot.IsVisible,
            IsTopMost = snapshot.IsTopMost
        };
    }
}
