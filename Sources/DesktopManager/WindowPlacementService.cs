using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DesktopManager;

/// <summary>
/// Executes reusable reliable window placement operations for apps, command-line tools, and PowerShell surfaces.
/// </summary>
public sealed class WindowPlacementService {
    private readonly WindowManager _windowManager;
    private readonly Monitors _monitors;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowPlacementService"/> class.
    /// </summary>
    public WindowPlacementService()
        : this(new Monitors()) {
    }

    private WindowPlacementService(Monitors monitors)
        : this(new WindowManager(monitors), monitors) {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowPlacementService"/> class.
    /// </summary>
    /// <param name="windowManager">Window manager used to query and mutate windows.</param>
    /// <param name="monitors">Monitor provider used to resolve target monitors.</param>
    public WindowPlacementService(WindowManager windowManager, Monitors monitors) {
        _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
        _monitors = monitors ?? throw new ArgumentNullException(nameof(monitors));
    }

    /// <summary>
    /// Applies the requested placement to a window and returns observed snapshots and verification status.
    /// </summary>
    /// <param name="request">Placement request.</param>
    /// <returns>The observed placement result.</returns>
    public WindowPlacementResult Apply(WindowPlacementRequest request) {
        if (request == null) {
            throw new ArgumentNullException(nameof(request));
        }

        ValidateRequest(request);

        var snapshots = new List<WindowPlacementSnapshot>();
        WindowInfo window = ResolveTargetWindow(request.TargetWindowHandle);
        IntPtr resolvedHandle = window.Handle;
        AddSnapshot(snapshots, "resolved", window);

        WindowPlacementResult? lastResult = null;
        bool verified = true;
        int maxAttempts = Math.Max(1, request.MaxAttempts);
        for (int attempt = 1; attempt <= maxAttempts; attempt++) {
            window = RefreshWindow(resolvedHandle);
            AddSnapshot(snapshots, $"attempt-{attempt}-before", window);
            Monitor? targetMonitor = RequiresTargetMonitor(request.Placement)
                ? ResolveTargetMonitor(request, window)
                : null;

            if (request.Placement == WindowPlacementKind.ExactRectangle) {
                ApplyExactRectangle(window, request, snapshots, attempt);
            } else {
                ApplyPlacement(window, targetMonitor, request, snapshots, attempt);
            }

            verified = !request.VerifyAfterAction || VerifyPlacement(resolvedHandle, request, targetMonitor);
            WindowInfo observed = RefreshWindow(resolvedHandle);
            AddSnapshot(snapshots, $"attempt-{attempt}-observed", observed);

            lastResult = new WindowPlacementResult(
                request.TargetWindowHandle,
                resolvedHandle,
                observed,
                verified,
                attempt,
                snapshots.ToArray());

            if (verified) {
                return lastResult;
            }
        }

        return lastResult ?? throw new InvalidOperationException("Window placement was not executed.");
    }

    private static void ValidateRequest(WindowPlacementRequest request) {
        if (request.Placement == WindowPlacementKind.ExactRectangle && !request.HasExactRectangle) {
            throw new ArgumentException("Exact rectangle placement requires left, top, width, and height.", nameof(request));
        }

        if (request.Placement == WindowPlacementKind.ExactRectangle &&
            (request.ExactWidth.GetValueOrDefault() <= 0 || request.ExactHeight.GetValueOrDefault() <= 0)) {
            throw new ArgumentOutOfRangeException(nameof(request), "Exact rectangle width and height must be greater than zero.");
        }

        if (request.VerificationIntervalMilliseconds <= 0) {
            throw new ArgumentOutOfRangeException(nameof(request), "Verification interval must be greater than zero.");
        }

        if (request.VerificationTimeoutMilliseconds < 0) {
            throw new ArgumentOutOfRangeException(nameof(request), "Verification timeout cannot be negative.");
        }
    }

    internal static bool RequiresTargetMonitor(WindowPlacementKind placement) {
        return placement != WindowPlacementKind.ExactRectangle &&
            placement != WindowPlacementKind.Restore;
    }

    private WindowInfo ResolveTargetWindow(IntPtr targetWindowHandle) {
        if (targetWindowHandle != IntPtr.Zero) {
            IntPtr rootHandle = WindowManager.GetRootWindowHandle(targetWindowHandle);
            WindowInfo? capturedWindow = _windowManager.GetWindow(
                rootHandle,
                includeHidden: true,
                includeCloaked: true,
                includeOwned: true,
                includeEmptyTitles: true);

            if (capturedWindow != null) {
                return capturedWindow;
            }

            throw new InvalidOperationException($"Window 0x{rootHandle.ToInt64():X} could not be resolved.");
        }

        return _windowManager.GetActiveWindow(
            includeHidden: true,
            includeCloaked: true,
            includeOwned: true,
            includeEmptyTitles: true) ?? throw new InvalidOperationException("Active window could not be resolved.");
    }

    private WindowInfo RefreshWindow(IntPtr handle) {
        return _windowManager.GetWindow(
            handle,
            includeHidden: true,
            includeCloaked: true,
            includeOwned: true,
            includeEmptyTitles: true) ?? throw new InvalidOperationException($"Window 0x{handle.ToInt64():X} could not be resolved.");
    }

    private void ApplyExactRectangle(WindowInfo window, WindowPlacementRequest request, List<WindowPlacementSnapshot> snapshots, int attempt) {
        _windowManager.RestoreWindow(window);
        WaitForRestoredWindow(window.Handle, request);
        AddSnapshot(snapshots, $"attempt-{attempt}-after-restore", RefreshWindow(window.Handle));

        WindowInfo restored = RefreshWindow(window.Handle);
        _windowManager.SetWindowRectangle(
            restored,
            request.ExactLeft!.Value,
            request.ExactTop!.Value,
            request.ExactWidth!.Value,
            request.ExactHeight!.Value);
        AddSnapshot(snapshots, $"attempt-{attempt}-after-move", RefreshWindow(window.Handle));
    }

    private Monitor? ResolveTargetMonitor(WindowPlacementRequest request, WindowInfo window) {
        List<Monitor> connectedMonitors = _monitors.GetMonitors(connectedOnly: true, refresh: true);
        Monitor? explicitMonitor = request.MonitorIndex.HasValue
            ? connectedMonitors.FirstOrDefault(monitor => monitor.Index == request.MonitorIndex.Value)
            : null;

        if (explicitMonitor != null) {
            return explicitMonitor;
        }

        if (request.MonitorIndex.HasValue) {
            throw new InvalidOperationException($"Monitor index {request.MonitorIndex.Value} was not found.");
        }

        if (request.MonitorTarget == WindowMonitorTargetKind.Current) {
            return ResolveCurrentMonitor(window);
        }

        List<Monitor> monitors = connectedMonitors
            .OrderBy(monitor => monitor.PositionTop)
            .ThenBy(monitor => monitor.PositionLeft)
            .ToList();

        if (monitors.Count == 0) {
            throw new InvalidOperationException("No connected monitors were found.");
        }

        return request.MonitorTarget switch {
            WindowMonitorTargetKind.TopLeft or
                WindowMonitorTargetKind.TopRight or
                WindowMonitorTargetKind.BottomLeft or
                WindowMonitorTargetKind.BottomRight => ResolveCornerMonitor(monitors, request.MonitorTarget),
            _ => ResolveCurrentMonitor(window)
        };
    }

    internal static Monitor ResolveCornerMonitor(IReadOnlyList<Monitor> monitors, WindowMonitorTargetKind target) {
        if (monitors.Count == 0) {
            throw new InvalidOperationException("No connected monitors were found.");
        }

        return target switch {
            WindowMonitorTargetKind.TopLeft => monitors
                .OrderBy(monitor => monitor.PositionTop)
                .ThenBy(monitor => monitor.PositionLeft)
                .First(),
            WindowMonitorTargetKind.TopRight => monitors
                .OrderBy(monitor => monitor.PositionTop)
                .ThenByDescending(monitor => monitor.PositionRight)
                .First(),
            WindowMonitorTargetKind.BottomLeft => monitors
                .OrderByDescending(monitor => monitor.PositionBottom)
                .ThenBy(monitor => monitor.PositionLeft)
                .First(),
            WindowMonitorTargetKind.BottomRight => monitors
                .OrderByDescending(monitor => monitor.PositionBottom)
                .ThenByDescending(monitor => monitor.PositionRight)
                .First(),
            _ => throw new ArgumentOutOfRangeException(nameof(target), target, "Unsupported corner monitor target.")
        };
    }

    internal static IReadOnlyList<IReadOnlyList<Monitor>> GroupMonitorRows(IReadOnlyList<Monitor> monitors) {
        return MonitorTopologySnapshot.GroupRows(monitors);
    }

    internal static RECT GetPlacementBounds(Monitor monitor) {
        if (monitor == null) {
            throw new ArgumentNullException(nameof(monitor));
        }

        return monitor.GetMonitorWorkArea();
    }

    private Monitor ResolveCurrentMonitor(WindowInfo window) {
        List<Monitor> monitors = _monitors.GetMonitors(connectedOnly: true, refresh: true);
        Monitor? monitor = monitors.FirstOrDefault(candidate => candidate.Index == window.MonitorIndex) ??
            monitors.FirstOrDefault(candidate =>
                window.Left >= candidate.PositionLeft &&
                window.Left < candidate.PositionRight &&
                window.Top >= candidate.PositionTop &&
                window.Top < candidate.PositionBottom);

        return monitor ?? monitors.FirstOrDefault() ?? throw new InvalidOperationException("No connected monitors were found.");
    }

    private void ApplyPlacement(WindowInfo window, Monitor? monitor, WindowPlacementRequest request, List<WindowPlacementSnapshot> snapshots, int attempt) {
        WindowPlacementKind placement = request.Placement;
        if (placement == WindowPlacementKind.Restore) {
            _windowManager.RestoreWindow(window);
            WaitForRestoredWindow(window.Handle, request);
            AddSnapshot(snapshots, $"attempt-{attempt}-after-restore", RefreshWindow(window.Handle));
            return;
        }

        if (placement == WindowPlacementKind.Maximize) {
            _windowManager.RestoreWindow(window);
            WaitForRestoredWindow(window.Handle, request);
            AddSnapshot(snapshots, $"attempt-{attempt}-after-restore", RefreshWindow(window.Handle));

            if (monitor != null) {
                WindowInfo restoredWindow = RefreshWindow(window.Handle);
                RECT placementBounds = GetPlacementBounds(monitor);
                _windowManager.SetWindowPosition(
                    restoredWindow,
                    placementBounds.Left,
                    placementBounds.Top,
                    placementBounds.Right - placementBounds.Left,
                    placementBounds.Bottom - placementBounds.Top);
                AddSnapshot(snapshots, $"attempt-{attempt}-after-move", RefreshWindow(window.Handle));
            }

            WindowInfo moved = RefreshWindow(window.Handle);
            _windowManager.MaximizeWindow(moved);
            AddSnapshot(snapshots, $"attempt-{attempt}-after-maximize", RefreshWindow(window.Handle));
            return;
        }

        Monitor targetMonitor = monitor ?? ResolveCurrentMonitor(window);
        RECT targetBounds = GetPlacementBounds(targetMonitor);
        int width = targetBounds.Right - targetBounds.Left;
        int height = targetBounds.Bottom - targetBounds.Top;
        int left = targetBounds.Left;

        if (placement == WindowPlacementKind.RightHalf) {
            left += width / 2;
        } else if (placement != WindowPlacementKind.LeftHalf) {
            throw new InvalidOperationException($"Unsupported window placement '{placement}'.");
        }

        _windowManager.RestoreWindow(window);
        WaitForRestoredWindow(window.Handle, request);
        AddSnapshot(snapshots, $"attempt-{attempt}-after-restore", RefreshWindow(window.Handle));

        WindowInfo restored = RefreshWindow(window.Handle);
        _windowManager.SetWindowPosition(restored, left, targetBounds.Top, width / 2, height);
        AddSnapshot(snapshots, $"attempt-{attempt}-after-move", RefreshWindow(window.Handle));
    }

    private bool VerifyPlacement(IntPtr handle, WindowPlacementRequest request, Monitor? monitor) {
        return WaitForWindow(handle, current => IsExpectedPlacement(current, request, monitor), request);
    }

    internal static bool IsExpectedPlacement(WindowInfo window, WindowPlacementRequest request, Monitor? monitor) {
        if (request.Placement == WindowPlacementKind.ExactRectangle) {
            return IsNear(window.Left, request.ExactLeft!.Value, request.GeometryTolerancePixels) &&
                IsNear(window.Top, request.ExactTop!.Value, request.GeometryTolerancePixels) &&
                IsNear(window.Width, request.ExactWidth!.Value, request.GeometryTolerancePixels) &&
                IsNear(window.Height, request.ExactHeight!.Value, request.GeometryTolerancePixels);
        }

        if (request.Placement == WindowPlacementKind.Maximize) {
            bool expectedMonitor = monitor == null || window.MonitorIndex == monitor.Index;
            return expectedMonitor && window.State == WindowState.Maximize;
        }

        if (request.Placement == WindowPlacementKind.Restore) {
            return window.State == WindowState.Normal;
        }

        if (monitor == null) {
            return true;
        }

        RECT expectedBounds = GetPlacementBounds(monitor);
        int width = expectedBounds.Right - expectedBounds.Left;
        int expectedLeft = expectedBounds.Left;
        if (request.Placement == WindowPlacementKind.RightHalf) {
            expectedLeft += width / 2;
        }

        return window.MonitorIndex == monitor.Index &&
            IsNear(window.Left, expectedLeft, request.GeometryTolerancePixels) &&
            IsNear(window.Top, expectedBounds.Top, request.GeometryTolerancePixels) &&
            IsNear(window.Width, width / 2, request.GeometryTolerancePixels) &&
            IsNear(window.Height, expectedBounds.Bottom - expectedBounds.Top, request.GeometryTolerancePixels);
    }

    private bool WaitForWindow(IntPtr handle, Func<WindowInfo, bool> predicate, WindowPlacementRequest request) {
        DateTime deadline = DateTime.UtcNow.AddMilliseconds(request.VerificationTimeoutMilliseconds);
        do {
            WindowInfo current = RefreshWindow(handle);
            if (predicate(current)) {
                return true;
            }

            Thread.Sleep(request.VerificationIntervalMilliseconds);
        } while (DateTime.UtcNow < deadline);

        return predicate(RefreshWindow(handle));
    }

    private bool WaitForRestoredWindow(IntPtr handle, WindowPlacementRequest? request) {
        WindowPlacementRequest waitRequest = request ?? new WindowPlacementRequest {
            VerifyAfterAction = false
        };

        return WaitForWindow(
            handle,
            current => current.State == WindowState.Normal &&
                current.Width > 200 &&
                current.Height > 100,
            waitRequest);
    }

    private static bool IsNear(int actual, int expected, int tolerance) {
        return Math.Abs(actual - expected) <= tolerance;
    }

    private static void AddSnapshot(List<WindowPlacementSnapshot> snapshots, string stage, WindowInfo window) {
        snapshots.Add(WindowPlacementSnapshot.FromWindow(stage, window));
    }
}
