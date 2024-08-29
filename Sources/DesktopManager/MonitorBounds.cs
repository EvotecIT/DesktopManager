namespace DesktopManager;

public class MonitorBounds {
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public MonitorBounds(RECT rect) {
        Left = rect.Left;
        Top = rect.Top;
        Right = rect.Right;
        Bottom = rect.Bottom;
    }
}