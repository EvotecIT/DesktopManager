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
        /// <param name="includeCloaked">
        /// Whether to include DWM-cloaked windows, such as windows on other virtual desktops and suspended app frames.
        /// Off by default, because changing the state of a cloaked frame can make it appear.
        /// </param>
        /// <param name="ignoreErrors">Whether to ignore failures per window.</param>
        /// <returns>Number of windows minimized.</returns>
        public int MinimizeAll(bool includeHidden = false, bool includeOwned = true, bool includeCloaked = false, bool ignoreErrors = true) {
            var windows = GetWindows(includeHidden: includeHidden, includeOwned: includeOwned, includeCloaked: includeCloaked);
            return MinimizeWindows(windows, ignoreErrors);
        }

        /// <summary>
        /// Restores all windows that match the specified filters.
        /// </summary>
        /// <param name="includeHidden">Whether to include hidden windows.</param>
        /// <param name="includeOwned">Whether to include owned windows.</param>
        /// <param name="includeCloaked">
        /// Whether to include DWM-cloaked windows, such as windows on other virtual desktops and suspended app frames.
        /// Off by default, because restoring a cloaked frame can make it appear.
        /// </param>
        /// <param name="ignoreErrors">Whether to ignore failures per window.</param>
        /// <returns>Number of windows restored.</returns>
        public int RestoreAll(bool includeHidden = false, bool includeOwned = true, bool includeCloaked = false, bool ignoreErrors = true) {
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
            return WindowDpiContext.Execute(() => {
                if (!MonitorNativeMethods.GetWindowRect(windowInfo.Handle, out RECT rect)) {
                    throw CreateWindowOperationException(windowInfo.Handle, "Read the window position");
                }

                var monitors = _monitors.GetMonitors(refresh: true);
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

                SetWindowPosOrThrow(windowInfo.Handle, IntPtr.Zero, bounds.Left + padding, bounds.Top + padding, width, height,
                    MonitorNativeMethods.SWP_NOZORDER | MonitorNativeMethods.SWP_NOACTIVATE, "Move the window on screen");
                return true;
            });
        }

        /// <summary>
        /// Sets the position of a window without activating it.
        /// </summary>
        /// <param name="windowInfo">The window information.</param>
        /// <param name="left">The left position.</param>
        /// <param name="top">The top position.</param>
        public void SetWindowPosition(WindowInfo windowInfo, int left, int top) {
            SetWindowPosition(windowInfo, left, top, -1, -1);
        }

        /// <summary>
        /// Sets the position and size of a window without activating it. For a minimised window this
        /// changes where it restores to and leaves it minimised.
        /// </summary>
        /// <param name="windowInfo">The window information.</param>
        /// <param name="left">The left position. Use -1 to keep the current left position.</param>
        /// <param name="top">The top position. Use -1 to keep the current top position.</param>
        /// <param name="width">The width of the window. Use -1 to keep current width.</param>
        /// <param name="height">The height of the window. Use -1 to keep current height.</param>
        /// <remarks>
        /// -1 means "keep" for the position. To move a window to exactly -1, for example on a monitor left of
        /// the primary one, use <see cref="SetWindowRectangle"/>.
        /// </remarks>
        public void SetWindowPosition(WindowInfo windowInfo, int left, int top, int width = -1, int height = -1) {
            SetWindowBounds(windowInfo,
                left == -1 ? null : left,
                top == -1 ? null : top,
                width < 0 ? null : width,
                height < 0 ? null : height);
        }

        /// <summary>
        /// Moves or resizes a window without activating it, keeping each value that is null. Negative coordinates
        /// are real positions on monitors left of or above the primary monitor.
        /// </summary>
        internal void SetWindowBounds(WindowInfo windowInfo, int? left, int? top, int? width, int? height) {
            ValidateWindowInfo(windowInfo);
            // A negative size has always meant "keep the current size".
            width = width < 0 ? null : width;
            height = height < 0 ? null : height;
            if (!left.HasValue && !top.HasValue && !width.HasValue && !height.HasValue) {
                return;
            }

            WindowDpiContext.Execute(() => {
                IntPtr handle = windowInfo.Handle;
                if (MonitorNativeMethods.IsIconic(handle)) {
                    RECT normal = WorkspaceWindowPlacement.GetNormalBounds(handle);
                    WorkspaceWindowPlacement.SetMinimizedNormalBounds(handle, MergeBounds(normal, left, top, width, height));
                    return;
                }

                if (!MonitorNativeMethods.GetWindowRect(handle, out RECT current)) {
                    throw CreateWindowOperationException(handle, "Read the window position");
                }

                int flags = MonitorNativeMethods.SWP_NOZORDER | MonitorNativeMethods.SWP_NOACTIVATE;
                if (!left.HasValue && !top.HasValue) {
                    flags |= SwpNoMove;
                }
                if (!width.HasValue && !height.HasValue) {
                    flags |= MonitorNativeMethods.SWP_NOSIZE;
                }

                RECT target = MergeBounds(current, left, top, width, height);
                SetWindowPosOrThrow(handle, IntPtr.Zero, target.Left, target.Top, target.Right - target.Left, target.Bottom - target.Top,
                    flags, "Set the window position");
            });
        }

        /// <summary>A window rectangle with each supplied value applied and the rest kept.</summary>
        internal static RECT MergeBounds(RECT current, int? left, int? top, int? width, int? height) {
            int newLeft = left ?? current.Left;
            int newTop = top ?? current.Top;
            int newWidth = width ?? current.Right - current.Left;
            int newHeight = height ?? current.Bottom - current.Top;
            return new RECT { Left = newLeft, Top = newTop, Right = newLeft + newWidth, Bottom = newTop + newHeight };
        }

        /// <summary>
        /// Sets an exact window rectangle without treating negative coordinates as sentinel values.
        /// For a minimised window this changes where it restores to and leaves it minimised.
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

            WindowDpiContext.Execute(() => {
                if (MonitorNativeMethods.IsIconic(windowInfo.Handle)) {
                    WorkspaceWindowPlacement.SetMinimizedNormalBounds(windowInfo.Handle,
                        new RECT { Left = left, Top = top, Right = left + width, Bottom = top + height });
                    return;
                }

                SetWindowPosOrThrow(windowInfo.Handle, IntPtr.Zero, left, top, width, height,
                    MonitorNativeMethods.SWP_NOZORDER, "Set the window rectangle");
            });
        }

        /// <summary>
        /// Snaps a window to a predefined region of its monitor's work area, so it doesn't cover the taskbar.
        /// A minimised or maximised window is restored into the region first. The visible frame fills the
        /// region, leaving no gap for the invisible resize border.
        /// </summary>
        /// <param name="window">The window to snap.</param>
        /// <param name="position">The snap position.</param>
        public void SnapWindow(WindowInfo window, SnapPosition position) {
            ValidateWindowInfo(window);
            WindowDpiContext.Execute(() => SnapWindow(window, ResolveLiveMonitor(window), position));
        }

        private void SnapWindow(WindowInfo window, Monitor monitor, SnapPosition position) {
            RECT area = monitor.GetMonitorWorkArea();
            RECT target;
            if (position == SnapPosition.Center) {
                RECT visible = MonitorNativeMethods.IsIconic(window.Handle) || IsZoomed(window.Handle)
                    ? WorkspaceWindowPlacement.GetNormalBounds(window.Handle)
                    : GetVisibleBounds(window.Handle);
                target = CenterInArea(area, visible.Right - visible.Left, visible.Bottom - visible.Top);
            } else {
                target = GetSnapRectangle(area, position);
            }

            PlaceVisibleFrame(window, target, GetBounds(monitor), $"Snap the window to {position}");
        }

        /// <summary>
        /// Moves the specified window to the target monitor, at the same place relative to the monitor's work area,
        /// kept inside the target's work area. A maximised window is maximised again on the target, and a minimised
        /// window stays minimised and restores on the target.
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

            return WindowDpiContext.Execute(() => MoveWindowToMonitorCore(windowInfo, RefreshMonitor(targetMonitor)));
        }

        private bool MoveWindowToMonitorCore(WindowInfo windowInfo, Monitor targetMonitor) {
            bool minimized = MonitorNativeMethods.IsIconic(windowInfo.Handle);
            RECT windowRect;
            if (minimized) {
                windowRect = WorkspaceWindowPlacement.GetNormalBounds(windowInfo.Handle);
            } else if (!MonitorNativeMethods.GetWindowRect(windowInfo.Handle, out windowRect)) {
                throw CreateWindowOperationException(windowInfo.Handle, "Read the window position");
            }

            var targetBounds = targetMonitor.GetMonitorBounds();

            var currentMonitor = _monitors.GetMonitors(index: windowInfo.MonitorIndex).FirstOrDefault();
            RECT currentBounds;
            if (currentMonitor != null) {
                currentBounds = currentMonitor.GetMonitorBounds();
            } else {
                currentBounds = new RECT { Left = windowRect.Left, Top = windowRect.Top };
            }

            if (currentBounds.Left == targetBounds.Left &&
                currentBounds.Top == targetBounds.Top &&
                currentBounds.Right == targetBounds.Right &&
                currentBounds.Bottom == targetBounds.Bottom) {
                return false;
            }

            RECT currentArea = currentMonitor != null ? currentMonitor.GetMonitorWorkArea() : currentBounds;
            if (minimized) {
                // The on-screen rectangle of a minimised window is the off-screen icon position; move where it restores to instead.
                WorkspaceWindowPlacement.SetMinimizedNormalBounds(windowInfo.Handle,
                    PlaceOnMonitor(windowRect, currentArea, targetMonitor.GetMonitorWorkArea()));
                return true;
            }

            // A maximised window would stay maximised on its old screen: restore it, move it, maximise it on the target.
            bool maximized = IsZoomed(windowInfo.Handle);
            if (maximized) {
                RestoreWindow(windowInfo);
                if (!MonitorNativeMethods.GetWindowRect(windowInfo.Handle, out windowRect)) {
                    throw CreateWindowOperationException(windowInfo.Handle, "Read the window position");
                }
            }

            RECT placed = PlaceOnMonitor(windowRect, currentArea, targetMonitor.GetMonitorWorkArea());
            SetWindowBounds(windowInfo, placed.Left, placed.Top, null, null);
            if (maximized) {
                MaximizeWindow(windowInfo);
            }
            return true;
        }

        /// <summary>
        /// Where a window lands on another monitor: at the same offset from the work area's corner as on its current monitor,
        /// moved back inside the target's work area when that offset would leave it partly off screen.
        /// </summary>
        internal static RECT PlaceOnMonitor(RECT window, RECT fromArea, RECT toArea) {
            int width = window.Right - window.Left;
            int height = window.Bottom - window.Top;
            int left = toArea.Left + (window.Left - fromArea.Left);
            int top = toArea.Top + (window.Top - fromArea.Top);
            // A window larger than the target keeps its top-left corner on screen, where its title bar is.
            left = Math.Max(toArea.Left, Math.Min(left, toArea.Right - width));
            top = Math.Max(toArea.Top, Math.Min(top, toArea.Bottom - height));
            return new RECT { Left = left, Top = top, Right = left + width, Bottom = top + height };
        }

        private const int SwpNoMove = 0x0002;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool IsZoomed(IntPtr hWnd);

        /// <summary>
        /// Closes a window.
        /// </summary>
        /// <param name="windowInfo">The window information.</param>
        public void CloseWindow(WindowInfo windowInfo) {
            ValidateWindowInfo(windowInfo);
            SendWindowCommand(windowInfo.Handle, WindowCommand.SC_CLOSE);
        }

        /// <summary>
        /// Minimizes a window.
        /// </summary>
        /// <param name="windowInfo">The window information.</param>
        public void MinimizeWindow(WindowInfo windowInfo) {
            ValidateWindowInfo(windowInfo);
            SendWindowCommand(windowInfo.Handle, WindowCommand.SC_MINIMIZE);
        }

        /// <summary>
        /// Maximizes a window.
        /// </summary>
        /// <param name="windowInfo">The window information.</param>
        public void MaximizeWindow(WindowInfo windowInfo) {
            ValidateWindowInfo(windowInfo);
            SendWindowCommand(windowInfo.Handle, WindowCommand.SC_MAXIMIZE);
        }

        /// <summary>
        /// Restores a window.
        /// </summary>
        /// <param name="windowInfo">The window information.</param>
        public void RestoreWindow(WindowInfo windowInfo) {
            ValidateWindowInfo(windowInfo);
            SendWindowCommand(windowInfo.Handle, WindowCommand.SC_RESTORE);
        }

        private static void SendWindowCommand(IntPtr handle, WindowCommand command) {
            if (MonitorNativeMethods.SendMessageTimeout(handle, (uint)WindowMessage.WM_SYSCOMMAND,
                    new IntPtr((long)command), IntPtr.Zero, MonitorNativeMethods.SMTO_ABORTIFHUNG, 1000, out _) == IntPtr.Zero) {
                int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                // Windows blocks the message outright for an elevated target or a closed window: the outcome is known.
                if (error == ErrorAccessDenied || error == ErrorInvalidWindowHandle) {
                    throw CreateWindowOperationException(handle, command.ToString(), error);
                }
                throw new NativeOperationOutcomeUnknownException(command.ToString(), 1000);
            }
        }

        /// <summary>
        /// Sets whether a window is topmost.
        /// </summary>
        /// <param name="windowInfo">The window information.</param>
        /// <param name="topMost">True to make the window topmost; false to reset.</param>
        public void SetWindowTopMost(WindowInfo windowInfo, bool topMost) {
            ValidateWindowInfo(windowInfo);

            var insertAfter = topMost ? MonitorNativeMethods.HWND_TOPMOST : MonitorNativeMethods.HWND_NOTOPMOST;
            // Changing the topmost band must not also activate the window and take focus from the user.
            SetWindowPosOrThrow(windowInfo.Handle, insertAfter, 0, 0, 0, 0,
                SwpNoMove | MonitorNativeMethods.SWP_NOSIZE | MonitorNativeMethods.SWP_NOACTIVATE,
                topMost ? "Make the window topmost" : "Clear the window's topmost state");
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
