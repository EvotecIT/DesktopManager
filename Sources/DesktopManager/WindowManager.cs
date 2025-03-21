using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DesktopManager {
    /// <summary>
    /// Provides methods to manage windows, including getting window information and controlling window states.
    /// </summary>
    public class WindowManager {
        private readonly Monitors _monitors;

        /// <summary>
        /// Initializes a new instance of the WindowManager class.
        /// </summary>
        public WindowManager() {
            _monitors = new Monitors();
        }

        /// <summary>
        /// Gets all visible windows.
        /// </summary>
        /// <param name="name">Optional window title filter. Supports wildcards.</param>
        /// <returns>A list of WindowInfo objects.</returns>
        public List<WindowInfo> GetWindows(string name = "*") {
            var handles = new List<IntPtr>();
            var shellWindowhWnd = MonitorNativeMethods.GetShellWindow();

            MonitorNativeMethods.EnumWindows(
                (handle, lParam) => {
                    if (handle != shellWindowhWnd && MonitorNativeMethods.IsWindowVisible(handle)) {
                        handles.Add(handle);
                    }
                    return true;
                }, 0);

            var windows = new List<WindowInfo>();
            foreach (var handle in handles) {
                var titleLength = MonitorNativeMethods.GetWindowTextLength(handle);
                if (titleLength > 0) {
                    var titleBuilder = new StringBuilder(titleLength);
                    MonitorNativeMethods.GetWindowText(handle, titleBuilder, titleLength + 1);
                    var title = titleBuilder.ToString();

                    if (MatchesWildcard(title, name)) {
                        uint processId = 0;
                        MonitorNativeMethods.GetWindowThreadProcessId(handle, out processId);

                        var windowInfo = new WindowInfo {
                            Title = title,
                            Handle = handle,
                            ProcessId = processId
                        };

                        // Get window position and state
                        RECT rect = new RECT();
                        if (MonitorNativeMethods.GetWindowRect(handle, out rect)) {
                            windowInfo.Left = rect.Left;
                            windowInfo.Top = rect.Top;
                            windowInfo.Right = rect.Right;
                            windowInfo.Bottom = rect.Bottom;

                            // Get window state
                            int style = MonitorNativeMethods.GetWindowLong(handle, MonitorNativeMethods.GWL_STYLE);
                            if ((style & MonitorNativeMethods.WS_MINIMIZE) != 0) {
                                windowInfo.State = WindowState.Minimize;
                            } else if ((style & MonitorNativeMethods.WS_MAXIMIZE) != 0) {
                                windowInfo.State = WindowState.Maximize;
                            } else {
                                windowInfo.State = WindowState.Normal;
                            }

                            // Find which monitor this window is primarily on
                            var monitors = _monitors.GetMonitors();
                            foreach (var monitor in monitors) {
                                var monitorRect = monitor.GetMonitorBounds();
                                // Check if window center point is within monitor bounds
                                int windowCenterX = (rect.Left + rect.Right) / 2;
                                int windowCenterY = (rect.Top + rect.Bottom) / 2;

                                if (windowCenterX >= monitorRect.Left && windowCenterX < monitorRect.Right &&
                                    windowCenterY >= monitorRect.Top && windowCenterY < monitorRect.Bottom) {
                                    windowInfo.MonitorIndex = monitor.Index;
                                    windowInfo.MonitorDeviceId = monitor.DeviceId;
                                    windowInfo.MonitorDeviceName = monitor.DeviceName;
                                    windowInfo.IsOnPrimaryMonitor = monitor.IsPrimary;
                                    break;
                                }
                            }
                        }

                        windows.Add(windowInfo);
                    }
                }
            }

            return windows;
        }

        /// <summary>
        /// Gets the position of a window.
        /// </summary>
        /// <param name="windowInfo">The window information.</param>
        /// <returns>The window position.</returns>
        public WindowPosition GetWindowPosition(WindowInfo windowInfo) {
            RECT rect = new RECT();
            if (MonitorNativeMethods.GetWindowRect(windowInfo.Handle, out rect)) {
                return new WindowPosition {
                    Title = windowInfo.Title,
                    Handle = windowInfo.Handle,
                    ProcessId = windowInfo.ProcessId,
                    Left = rect.Left,
                    Top = rect.Top,
                    Right = rect.Right,
                    Bottom = rect.Bottom
                };
            }
            throw new InvalidOperationException("Failed to get window position");
        }

        /// <summary>
        /// Sets the position of a window.
        /// </summary>
        /// <param name="windowInfo">The window information.</param>
        /// <param name="left">The left position.</param>
        /// <param name="top">The top position.</param>
        public void SetWindowPosition(WindowInfo windowInfo, int left, int top) {
            if (!MonitorNativeMethods.SetWindowPos(
                windowInfo.Handle,
                IntPtr.Zero,
                left,
                top,
                -1,
                -1,
                1)) {
                throw new InvalidOperationException("Failed to set window position");
            }
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
            const int SWP_NOZORDER = 0x0004;
            const int SWP_NOMOVE = 0x0002;
            const int SWP_NOSIZE = 0x0001;

            int flags = SWP_NOZORDER;

            // If position is -1, don't move
            if (left < 0 && top < 0) {
                flags |= SWP_NOMOVE;
            }

            // If size is -1, don't resize
            if (width < 0 && height < 0) {
                flags |= SWP_NOSIZE;
            }

            if (!MonitorNativeMethods.SetWindowPos(
                windowInfo.Handle,
                IntPtr.Zero,
                left < 0 ? windowInfo.Left : left,
                top < 0 ? windowInfo.Top : top,
                width < 0 ? windowInfo.Width : width,
                height < 0 ? windowInfo.Height : height,
                flags)) {
                throw new InvalidOperationException("Failed to set window position");
            }
        }

        /// <summary>
        /// Closes a window.
        /// </summary>
        /// <param name="windowInfo">The window information.</param>
        public void CloseWindow(WindowInfo windowInfo) {
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
            MonitorNativeMethods.SendMessage(
                windowInfo.Handle,
                (uint)WindowMessage.WM_SYSCOMMAND,
                (uint)WindowCommand.SC_RESTORE,
                0);
        }

        private bool MatchesWildcard(string text, string pattern) {
            if (pattern == "*") {
                return true;
            }

            if (pattern.Contains("*")) {
                // Handle basic wildcard pattern with * only
                int starIndex = pattern.IndexOf('*');
                if (starIndex == 0) {
                    // Pattern starts with *, check if text ends with rest of pattern
                    return text.EndsWith(pattern.Substring(1), StringComparison.OrdinalIgnoreCase);
                } else if (starIndex == pattern.Length - 1) {
                    // Pattern ends with *, check if text starts with rest of pattern
                    return text.StartsWith(pattern.Substring(0, pattern.Length - 1), StringComparison.OrdinalIgnoreCase);
                } else {
                    // Pattern has * in middle, check both parts
                    string[] parts = pattern.Split('*');
                    return text.StartsWith(parts[0], StringComparison.OrdinalIgnoreCase) &&
                           text.EndsWith(parts[1], StringComparison.OrdinalIgnoreCase);
                }
            }

            return text.Contains(pattern);
        }
    }
}