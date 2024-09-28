namespace DesktopManager;

public class MonitorPosition {
    public int Left { get; set; }
    public int Top { get; set; }
    public int Right { get; set; }
    public int Bottom { get; set; }

    public MonitorPosition(int left, int top, int right, int bottom) {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }
}