using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopManager;

public partial class WindowManager
{
        private static void ValidateWindowInfo(WindowInfo windowInfo) {
            if (windowInfo == null) {
                throw new ArgumentNullException(nameof(windowInfo));
            }
            if (windowInfo.Handle == IntPtr.Zero) {
                throw new InvalidOperationException("Invalid window handle.");
            }
        }

        /// <summary>
        /// Minimizes the specified windows.
        /// </summary>
        /// <param name="windows">Windows to minimize.</param>
        /// <param name="ignoreErrors">Whether to ignore failures per window.</param>
        /// <returns>Number of windows minimized.</returns>
        public int MinimizeWindows(IEnumerable<WindowInfo> windows, bool ignoreErrors = true) {
            if (windows == null) {
                throw new ArgumentNullException(nameof(windows));
            }

            int count = 0;
            foreach (var window in windows) {
                if (window == null) {
                    continue;
                }

                try {
                    ValidateWindowInfo(window);
                    if (IsWindowMinimized(window.Handle)) {
                        continue;
                    }
                    MinimizeWindow(window);
                    count++;
                } catch when (ignoreErrors) {
                    // Ignore per-window failures
                }
            }

            return count;
        }

        /// <summary>
        /// Restores the specified windows.
        /// </summary>
        /// <param name="windows">Windows to restore.</param>
        /// <param name="ignoreErrors">Whether to ignore failures per window.</param>
        /// <returns>Number of windows restored.</returns>
        public int RestoreWindows(IEnumerable<WindowInfo> windows, bool ignoreErrors = true) {
            if (windows == null) {
                throw new ArgumentNullException(nameof(windows));
            }

            int count = 0;
            foreach (var window in windows) {
                if (window == null) {
                    continue;
                }

                try {
                    ValidateWindowInfo(window);
                    if (!IsWindowMinimized(window.Handle)) {
                        continue;
                    }
                    RestoreWindow(window);
                    count++;
                } catch when (ignoreErrors) {
                    // Ignore per-window failures
                }
            }

            return count;
        }

        /// <summary>
        /// Minimizes all windows that match the specified filters.
        /// </summary>
        /// <param name="includeHidden">Whether to include hidden windows.</param>
        /// <param name="includeOwned">Whether to include owned windows.</param>
        /// <param name="includeCloaked">Whether to include DWM-cloaked windows.</param>
        /// <param name="ignoreErrors">Whether to ignore failures per window.</param>
        /// <returns>Number of windows minimized.</returns>
        public int MinimizeAll(bool includeHidden = false, bool includeOwned = true, bool includeCloaked = true, bool ignoreErrors = true) {
            var windows = GetWindows(includeHidden: includeHidden, includeOwned: includeOwned, includeCloaked: includeCloaked);
            return MinimizeWindows(windows, ignoreErrors);
        }

        /// <summary>
        /// Restores all windows that match the specified filters.
        /// </summary>
        /// <param name="includeHidden">Whether to include hidden windows.</param>
        /// <param name="includeOwned">Whether to include owned windows.</param>
        /// <param name="includeCloaked">Whether to include DWM-cloaked windows.</param>
        /// <param name="ignoreErrors">Whether to ignore failures per window.</param>
        /// <returns>Number of windows restored.</returns>
        public int RestoreAll(bool includeHidden = false, bool includeOwned = true, bool includeCloaked = true, bool ignoreErrors = true) {
            var windows = GetWindows(includeHidden: includeHidden, includeOwned: includeOwned, includeCloaked: includeCloaked);
            return RestoreWindows(windows, ignoreErrors);
        }

        /// <summary>
        /// Ensures the specified window is positioned on a visible monitor.
        /// </summary>
        /// <param name="windowInfo">The window information.</param>
        /// <param name="padding">Padding in pixels from the monitor edges.</param>
        /// <returns>True if the window was moved; otherwise false.</returns>
        public bool EnsureWindowOnScreen(WindowInfo windowInfo, int padding = 10) {
            ValidateWindowInfo(windowInfo);
            if (!MonitorNativeMethods.GetWindowRect(windowInfo.Handle, out RECT rect)) {
                throw new InvalidOperationException("Failed to get window position");
            }

            var monitors = _monitors.GetMonitors();
            if (monitors.Count == 0) {
                throw new InvalidOperationException("No monitors found for window positioning");
            }

            if (IsRectOnAnyMonitor(rect, monitors)) {
                return false;
            }

            var target = monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors[0];
            var bounds = target.GetMonitorBounds();

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            if (width <= 0) {
                width = 200;
            }
            if (height <= 0) {
                height = 200;
            }

            int left = bounds.Left + padding;
            int top = bounds.Top + padding;

            if (!MonitorNativeMethods.SetWindowPos(windowInfo.Handle, IntPtr.Zero, left, top, width, height,
                MonitorNativeMethods.SWP_NOZORDER | MonitorNativeMethods.SWP_NOACTIVATE)) {
                throw new InvalidOperationException("Failed to move window on screen");
            }

            return true;
        }

        /// <summary>
        /// Sets the position of a window.
        /// </summary>
        /// <param name="windowInfo">The window information.</param>
        /// <param name="left">The left position.</param>
        /// <param name="top">The top position.</param>
        public void SetWindowPosition(WindowInfo windowInfo, int left, int top) {
            SetWindowPosition(windowInfo, left, top, -1, -1);
        }

        /// <summary>
        /// Sets the position and size of a window.
        /// </summary>
        /// <param name="windowInfo">The window information.</param>
        /// <param name="left">The left position.</param>
        /// <param name="top">The top position.</param>
        /// <param name="width">The width of the window. Use -1 to keep current width.</param>
        /// <param name="height">The height of the window. Use -1 to keep current height.</param>
        public void SetWindowPosition(WindowInfo windowInfo, int left, int top, int width = -1, int height = -1) {
            const int SWP_NOMOVE = 0x0002;

            ValidateWindowInfo(windowInfo);

            int flags = MonitorNativeMethods.SWP_NOZORDER;

            // If position is -1, don't move
            if (left == -1 && top == -1) {
                flags |= SWP_NOMOVE;
            }

            // If size is -1, don't resize
            if (width < 0 && height < 0) {
                flags |= MonitorNativeMethods.SWP_NOSIZE;
            }

            if (!MonitorNativeMethods.SetWindowPos(
                windowInfo.Handle,
                IntPtr.Zero,
                left == -1 ? windowInfo.Left : left,
                top == -1 ? windowInfo.Top : top,
                width < 0 ? windowInfo.Width : width,
                height < 0 ? windowInfo.Height : height,
                flags)) {
                throw new InvalidOperationException("Failed to set window position");
            }
        }

        /// <summary>
        /// Sets an exact window rectangle without treating negative coordinates as sentinel values.
        /// </summary>
        /// <param name="windowInfo">The window information.</param>
        /// <param name="left">The exact left position.</param>
        /// <param name="top">The exact top position.</param>
        /// <param name="width">The exact window width.</param>
        /// <param name="height">The exact window height.</param>
        public void SetWindowRectangle(WindowInfo windowInfo, int left, int top, int width, int height) {
            ValidateWindowInfo(windowInfo);

            if (width <= 0) {
                throw new ArgumentOutOfRangeException(nameof(width), "Window width must be greater than zero.");
            }

            if (height <= 0) {
                throw new ArgumentOutOfRangeException(nameof(height), "Window height must be greater than zero.");
            }

            if (!MonitorNativeMethods.SetWindowPos(
                windowInfo.Handle,
                IntPtr.Zero,
                left,
                top,
                width,
                height,
                MonitorNativeMethods.SWP_NOZORDER)) {
                throw new InvalidOperationException("Failed to set window rectangle");
            }
        }

        /// <summary>
        /// Snaps a window to a predefined region of its monitor.
        /// </summary>
        /// <param name="window">The window to snap.</param>
        /// <param name="position">The snap position.</param>
        public void SnapWindow(WindowInfo window, SnapPosition position) {      
            ValidateWindowInfo(window);

            var monitor = _monitors.GetMonitors(index: window.MonitorIndex).FirstOrDefault();
            if (monitor == null) {
                monitor = _monitors.GetMonitors(primaryOnly: true).FirstOrDefault();
                if (monitor == null) {
                    throw new InvalidOperationException("No monitor found for snapping");
                }
            }

            var bounds = monitor.GetMonitorBounds();
            int monitorWidth = bounds.Right - bounds.Left;
            int monitorHeight = bounds.Bottom - bounds.Top;

            int width = monitorWidth;
            int height = monitorHeight;
            int left = bounds.Left;
            int top = bounds.Top;

            switch (position) {
                case SnapPosition.Left:
                    width = monitorWidth / 2;
                    break;
                case SnapPosition.Right:
                    width = monitorWidth / 2;
                    left += monitorWidth / 2;
                    break;
                case SnapPosition.TopLeft:
                    width = monitorWidth / 2;
                    height = monitorHeight / 2;
                    break;
                case SnapPosition.TopRight:
                    width = monitorWidth / 2;
                    height = monitorHeight / 2;
                    left += monitorWidth / 2;
                    break;
                case SnapPosition.BottomLeft:
                    width = monitorWidth / 2;
                    height = monitorHeight / 2;
                    top += monitorHeight / 2;
                    break;
                case SnapPosition.BottomRight:
                    width = monitorWidth / 2;
                    height = monitorHeight / 2;
                    left += monitorWidth / 2;
                    top += monitorHeight / 2;
                    break;
            }

            SetWindowPosition(window, left, top, width, height);
        }

        /// <summary>
        /// Moves the specified window to the target monitor while preserving its relative position.
        /// </summary>
        /// <param name="windowInfo">The window to move.</param>
        /// <param name="targetMonitor">The monitor to move the window to.</param>
        /// <returns>
        /// True if the window was repositioned; false if the window was already on the target monitor
        /// at the same coordinates.
        /// </returns>
        public bool MoveWindowToMonitor(WindowInfo windowInfo, Monitor targetMonitor) {
            ValidateWindowInfo(windowInfo);
            if (targetMonitor == null) {
                throw new ArgumentNullException(nameof(targetMonitor));
            }

            RECT windowRect = new RECT();
            if (!MonitorNativeMethods.GetWindowRect(windowInfo.Handle, out windowRect)) {
                throw new InvalidOperationException("Failed to get window position");
            }

            var targetBounds = targetMonitor.GetMonitorBounds();

            var currentMonitor = _monitors.GetMonitors(index: windowInfo.MonitorIndex).FirstOrDefault();
            RECT currentBounds;
            if (currentMonitor != null) {
                currentBounds = currentMonitor.GetMonitorBounds();
            } else {
                currentBounds = new RECT { Left = windowRect.Left, Top = windowRect.Top };
            }

            int offsetX = windowRect.Left - currentBounds.Left;
            int offsetY = windowRect.Top - currentBounds.Top;

            int newLeft = targetBounds.Left + offsetX;
            int newTop = targetBounds.Top + offsetY;

            if (currentBounds.Left == targetBounds.Left &&
                currentBounds.Top == targetBounds.Top &&
                currentBounds.Right == targetBounds.Right &&
                currentBounds.Bottom == targetBounds.Bottom) {
                return false;
            }

            SetWindowPosition(windowInfo, newLeft, newTop);
            return true;
        }

        /// <summary>
        /// Closes a window.
        /// </summary>
        /// <param name="windowInfo">The window information.</param>
        public void CloseWindow(WindowInfo windowInfo) {
            ValidateWindowInfo(windowInfo);
            MonitorNativeMethods.SendMessage(
                windowInfo.Handle,
                (uint)WindowMessage.WM_SYSCOMMAND,
                (uint)WindowCommand.SC_CLOSE,
                0);
        }

        /// <summary>
        /// Minimizes a window.
        /// </summary>
        /// <param name="windowInfo">The window information.</param>
        public void MinimizeWindow(WindowInfo windowInfo) {
            ValidateWindowInfo(windowInfo);
            MonitorNativeMethods.SendMessage(
                windowInfo.Handle,
                (uint)WindowMessage.WM_SYSCOMMAND,
                (uint)WindowCommand.SC_MINIMIZE,
                0);
        }

        /// <summary>
        /// Maximizes a window.
        /// </summary>
        /// <param name="windowInfo">The window information.</param>
        public void MaximizeWindow(WindowInfo windowInfo) {
            ValidateWindowInfo(windowInfo);
            MonitorNativeMethods.SendMessage(
                windowInfo.Handle,
                (uint)WindowMessage.WM_SYSCOMMAND,
                (uint)WindowCommand.SC_MAXIMIZE,
                0);
        }

        /// <summary>
        /// Restores a window.
        /// </summary>
        /// <param name="windowInfo">The window information.</param>
        public void RestoreWindow(WindowInfo windowInfo) {
            ValidateWindowInfo(windowInfo);
            MonitorNativeMethods.ShowWindow(windowInfo.Handle, MonitorNativeMethods.SW_RESTORE);
            MonitorNativeMethods.SendMessage(
                windowInfo.Handle,
                (uint)WindowMessage.WM_SYSCOMMAND,
                (uint)WindowCommand.SC_RESTORE,
                0);
        }

        /// <summary>
        /// Sets whether a window is topmost.
        /// </summary>
        /// <param name="windowInfo">The window information.</param>
        /// <param name="topMost">True to make the window topmost; false to reset.</param>
        public void SetWindowTopMost(WindowInfo windowInfo, bool topMost) {     
            const int SWP_NOMOVE = 0x0002;
            const int SWP_NOSIZE = 0x0001;

            ValidateWindowInfo(windowInfo);

            var insertAfter = topMost ? MonitorNativeMethods.HWND_TOPMOST : MonitorNativeMethods.HWND_NOTOPMOST;
            if (!MonitorNativeMethods.SetWindowPos(windowInfo.Handle, insertAfter, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE)) {
                throw new InvalidOperationException("Failed to set window topmost state");
            }
        }

        /// <summary>
        /// Activates a window.
        /// </summary>
        /// <param name="windowInfo">The window information.</param>
        public void ActivateWindow(WindowInfo windowInfo) {
            ValidateWindowInfo(windowInfo);
            if (!WindowActivationService.TryActivateWindow(windowInfo.Handle)) {
                throw new InvalidOperationException("Failed to activate window");
            }
        }

        /// <summary>
        /// Sets the transparency level of a window.
        /// </summary>
        /// <param name="windowInfo">The window information.</param>
        /// <param name="alpha">Transparency alpha from 0 (transparent) to 255 (opaque).</param>
        public void SetWindowTransparency(WindowInfo windowInfo, byte alpha) {
            ValidateWindowInfo(windowInfo);
            IntPtr stylePtr = MonitorNativeMethods.GetWindowLongPtr(windowInfo.Handle, MonitorNativeMethods.GWL_EXSTYLE);
            int style = stylePtr.ToInt32();
            if ((style & MonitorNativeMethods.WS_EX_LAYERED) == 0) {
                MonitorNativeMethods.SetWindowLongPtr(windowInfo.Handle, MonitorNativeMethods.GWL_EXSTYLE, new IntPtr(style | MonitorNativeMethods.WS_EX_LAYERED));
            }

            if (!MonitorNativeMethods.SetLayeredWindowAttributes(windowInfo.Handle, 0, alpha, MonitorNativeMethods.LWA_ALPHA)) {
                throw new InvalidOperationException("Failed to set window transparency");
            }
        }

        private static bool IsWindowMinimized(IntPtr handle) {
            IntPtr stylePtr = MonitorNativeMethods.GetWindowLongPtr(handle, MonitorNativeMethods.GWL_STYLE);
            long style = stylePtr.ToInt64();
            return (style & MonitorNativeMethods.WS_MINIMIZE) != 0;
        }

        private static bool IsRectOnAnyMonitor(RECT rect, IEnumerable<Monitor> monitors) {
            foreach (var monitor in monitors) {
                var bounds = monitor.GetMonitorBounds();
                if (rect.Right <= bounds.Left || rect.Left >= bounds.Right ||
                    rect.Bottom <= bounds.Top || rect.Top >= bounds.Bottom) {
                    continue;
                }
                return true;
            }
            return false;
        }
}
