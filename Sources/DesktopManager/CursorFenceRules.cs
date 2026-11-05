using System;

namespace DesktopManager;

/// <summary>A position in physical virtual-desktop pixels.</summary>
internal readonly struct FencePoint : IEquatable<FencePoint> {
    public FencePoint(int x, int y) {
        X = x;
        Y = y;
    }

    public int X { get; }

    public int Y { get; }

    public bool Equals(FencePoint other) => X == other.X && Y == other.Y;

    public override bool Equals(object? obj) => obj is FencePoint other && Equals(other);

    public override int GetHashCode() => unchecked((X * 397) ^ Y);

    public override string ToString() => $"({X}, {Y})";
}

/// <summary>The fenced rectangle's edge a move pushes against.</summary>
internal enum FenceEdge {
    None,
    Left,
    Right,
    Top,
    Bottom
}

/// <summary>Pressure at one edge, using the mouse message's wrapping millisecond clock.</summary>
internal readonly struct FencePush : IEquatable<FencePush> {
    public FencePush(FenceEdge edge, double distance, uint started, uint last) {
        Edge = edge;
        Distance = distance;
        Started = started;
        Last = last;
    }

    public FenceEdge Edge { get; }

    public double Distance { get; }

    public uint Started { get; }

    public uint Last { get; }

    public bool Equals(FencePush other) =>
        Edge == other.Edge && Distance.Equals(other.Distance) && Started == other.Started && Last == other.Last;

    public override bool Equals(object? obj) => obj is FencePush other && Equals(other);

    public override int GetHashCode() => unchecked(((int)Edge * 397) ^ Distance.GetHashCode() ^ (int)Started ^ ((int)Last << 7));

    public override string ToString() => $"{Edge} {Distance} {Started}-{Last}";
}

/// <summary>Where the pointer may go for one move, and the pressure to carry into the next.</summary>
internal readonly struct FenceDecision {
    public FenceDecision(FencePoint point, FencePush push) {
        Point = point;
        Push = push;
    }

    public FencePoint Point { get; }

    public FencePush Push { get; }
}

/// <summary>
/// Pure entry decisions for <see cref="CursorFence"/>, independent of windows and UI frameworks.
/// Exiting is always allowed, including after touch, a display change or another application's cursor repositioning.
/// </summary>
internal static class CursorFenceRules {
    internal const int PushDistance = 120;
    internal const uint PushMilliseconds = 150;
    internal const uint ResetMilliseconds = 250;

    // MI_WP_SIGNATURE marks mouse messages Windows promotes from touch and pen, including their button messages.
    private const ulong PointerSignatureMask = 0xFFFFFF00UL;
    private const ulong PointerSignature = 0xFF515700UL;

    /// <summary>Decides where a proposed move may put the pointer.</summary>
    /// <param name="current">Pointer position before the move.</param>
    /// <param name="proposed">Position the move would reach.</param>
    /// <param name="bounds">Fenced rectangle; right and bottom are exclusive.</param>
    /// <param name="mode">Fence mode.</param>
    /// <param name="control">True while a Control key is held, which always lets the pointer through.</param>
    /// <param name="extraInfo">The mouse message's extra information.</param>
    /// <param name="time">The mouse message's millisecond timestamp.</param>
    /// <param name="push">Pressure carried from the previous move.</param>
    public static FenceDecision Decide(FencePoint current, FencePoint proposed, RECT bounds,
        CursorFenceMode mode, bool control, ulong extraInfo, uint time, FencePush push) {
        if ((extraInfo & PointerSignatureMask) == PointerSignature || control || mode == CursorFenceMode.Off ||
            bounds.Right <= bounds.Left || bounds.Bottom <= bounds.Top || Contains(bounds, current)) {
            return new FenceDecision(proposed, default);
        }

        if (!Entry(current, proposed, bounds, out FenceEdge edge, out FencePoint contact)) {
            return new FenceDecision(proposed, default);
        }

        if (mode == CursorFenceMode.Sticky) {
            if (push.Edge != edge || unchecked(time - push.Last) > ResetMilliseconds) {
                push = new FencePush(edge, 0, time, time);
            }

            double pressure = edge switch {
                FenceEdge.Left => (double)proposed.X - bounds.Left + 1,
                FenceEdge.Right => (double)bounds.Right - proposed.X,
                FenceEdge.Top => (double)proposed.Y - bounds.Top + 1,
                _ => (double)bounds.Bottom - proposed.Y,
            };
            push = new FencePush(edge, Math.Min(PushDistance, push.Distance + Math.Max(0, pressure)), push.Started, time);
            if (push.Distance >= PushDistance && unchecked(time - push.Started) >= PushMilliseconds) {
                return new FenceDecision(proposed, default);
            }
        } else {
            push = default;
        }

        return new FenceDecision(contact, push);
    }

    internal static bool Contains(RECT bounds, FencePoint point) =>
        point.X >= bounds.Left && point.X < bounds.Right && point.Y >= bounds.Top && point.Y < bounds.Bottom;

    /// <summary>Clips the whole motion segment, so a fast move cannot jump through a narrow rectangle.</summary>
    private static bool Entry(FencePoint from, FencePoint to, RECT bounds, out FenceEdge edge, out FencePoint contact) {
        double enter = 0.0;
        double leave = 1.0;
        edge = FenceEdge.None;
        contact = to;
        double dx = (double)to.X - from.X;
        double dy = (double)to.Y - from.Y;
        if (!Axis(from.X, dx, bounds.Left, bounds.Right - 1, FenceEdge.Left, FenceEdge.Right, ref enter, ref leave, ref edge) ||
            !Axis(from.Y, dy, bounds.Top, bounds.Bottom - 1, FenceEdge.Top, FenceEdge.Bottom, ref enter, ref leave, ref edge) ||
            edge == FenceEdge.None) {
            return false;
        }

        int x = (int)Math.Round(from.X + dx * enter);
        int y = (int)Math.Round(from.Y + dy * enter);
        contact = edge switch {
            FenceEdge.Left => new FencePoint(bounds.Left - 1, y),
            FenceEdge.Right => new FencePoint(bounds.Right, y),
            FenceEdge.Top => new FencePoint(x, bounds.Top - 1),
            _ => new FencePoint(x, bounds.Bottom),
        };
        return true;
    }

    private static bool Axis(int start, double delta, int low, int high, FenceEdge lowEdge, FenceEdge highEdge,
        ref double enter, ref double leave, ref FenceEdge edge) {
        if (delta == 0) {
            return start >= low && start <= high;
        }

        double first = (low - (double)start) / delta;
        double last = (high - (double)start) / delta;
        FenceEdge enteringEdge = lowEdge;
        if (first > last) {
            (first, last) = (last, first);
            enteringEdge = highEdge;
        }

        if (first > enter) {
            enter = first;
            edge = enteringEdge;
        }

        leave = Math.Min(leave, last);
        return enter <= leave;
    }
}
