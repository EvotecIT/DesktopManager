using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

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

            if (!MonitorNativeMethods.EnumWindows(
                (handle, lParam) => {
                    if (handle != shellWindowhWnd && MonitorNativeMethods.IsWindowVisible(handle)) {
                        handles.Add(handle);
                    }
                    return true;
                }, IntPtr.Zero)) {
                throw new InvalidOperationException("Failed to enumerate windows");
            }

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

                            // Get window state using the IntPtr wrapper to work on x86 and x64
                            IntPtr stylePtr = MonitorNativeMethods.GetWindowLongPtr(handle, MonitorNativeMethods.GWL_STYLE);
                            int style = unchecked((int)(long)stylePtr);
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
                // Re-evaluate the current state directly from the window to avoid stale data
                IntPtr stylePtr = MonitorNativeMethods.GetWindowLongPtr(windowInfo.Handle, MonitorNativeMethods.GWL_STYLE);
                int style = unchecked((int)(long)stylePtr);
                var state = WindowState.Normal;
                if ((style & MonitorNativeMethods.WS_MINIMIZE) != 0) {
                    state = WindowState.Minimize;
                } else if ((style & MonitorNativeMethods.WS_MAXIMIZE) != 0) {
                    state = WindowState.Maximize;
                }

                return new WindowPosition {
                    Title = windowInfo.Title,
                    Handle = windowInfo.Handle,
                    ProcessId = windowInfo.ProcessId,
                    Left = rect.Left,
                    Top = rect.Top,
                    Right = rect.Right,
                    Bottom = rect.Bottom,
                    State = state
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

            int flags = MonitorNativeMethods.SWP_NOZORDER;

            // If position is -1, don't move
            if (left < 0 && top < 0) {
                flags |= SWP_NOMOVE;
            }

            // If size is -1, don't resize
            if (width < 0 && height < 0) {
                flags |= MonitorNativeMethods.SWP_NOSIZE;
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
        /// Moves the specified window to the target monitor while preserving its relative position.
        /// </summary>
        /// <param name="windowInfo">The window to move.</param>
        /// <param name="targetMonitor">The monitor to move the window to.</param>
        public void MoveWindowToMonitor(WindowInfo windowInfo, Monitor targetMonitor) {
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

            SetWindowPosition(windowInfo, newLeft, newTop);
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

        /// <summary>
        /// Sets whether a window is topmost.
        /// </summary>
        /// <param name="windowInfo">The window information.</param>
        /// <param name="topMost">True to make the window topmost; false to reset.</param>
        public void SetWindowTopMost(WindowInfo windowInfo, bool topMost) {
            const int SWP_NOMOVE = 0x0002;
            const int SWP_NOSIZE = 0x0001;

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
            if (!MonitorNativeMethods.SetForegroundWindow(windowInfo.Handle)) {
                throw new InvalidOperationException("Failed to activate window");
            }
        }

        /// <summary>
        /// Saves the current window layout to a JSON file.
        /// </summary>
        /// <param name="path">Destination path for the layout.</param>
        public void SaveLayout(string path) {
            var layout = new WindowLayout {
                Windows = GetWindows().Select(GetWindowPosition).ToList()
            };
            var json = System.Text.Json.JsonSerializer.Serialize(layout,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            var fullPath = System.IO.Path.GetFullPath(path);
            var directory = System.IO.Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory)) {
                System.IO.Directory.CreateDirectory(directory);
            }
            System.IO.File.WriteAllText(fullPath, json);
        }

        /// <summary>
        /// Loads a window layout from a JSON file and applies it.
        /// </summary>
        /// <param name="path">Path to the layout file.</param>
        /// <param name="validate">Validate layout before applying.</param>
        public void LoadLayout(string path, bool validate = false) {
            if (!System.IO.File.Exists(path)) {
                throw new System.IO.FileNotFoundException("Layout file not found", path);
            }

            var json = System.IO.File.ReadAllText(path);
            WindowLayout? layout;
            try {
                layout = System.Text.Json.JsonSerializer.Deserialize<WindowLayout>(json);
            } catch (System.Text.Json.JsonException ex) {
                throw new InvalidOperationException($"Invalid layout file: {ex.Message}", ex);
            }
            if (layout == null) {
                return;
            }

            if (validate) {
                ValidateLayout(layout);
            }

            var current = GetWindows();
            foreach (var target in layout.Windows) {
                var window = current.FirstOrDefault(w => w.ProcessId == target.ProcessId && w.Title == target.Title);
                if (window != null) {
                    // restore window to allow repositioning
                    RestoreWindow(window);
                    SetWindowPosition(window, target.Left, target.Top, target.Width, target.Height);
                    if (target.State.HasValue) {
                        switch (target.State.Value) {
                            case WindowState.Minimize:
                                MinimizeWindow(window);
                                break;
                            case WindowState.Maximize:
                                MaximizeWindow(window);
                                break;
                            case WindowState.Normal:
                                // already restored above
                                break;
                            case WindowState.Close:
                                CloseWindow(window);
                                break;
                        }
                        window.State = target.State;
                    }
                }
            }
        }

        private static void ValidateLayout(WindowLayout layout) {
            if (layout.Windows == null) {
                throw new InvalidDataException("Layout does not contain any windows.");
            }

            foreach (var window in layout.Windows) {
                if (string.IsNullOrWhiteSpace(window.Title)) {
                    throw new InvalidDataException("Window title is required.");
                }

                if (window.ProcessId == 0) {
                    throw new InvalidDataException($"Window '{window.Title}' has invalid process id.");
                }
            }
        }

        private bool MatchesWildcard(string text, string pattern) {
            if (string.IsNullOrEmpty(pattern)) {
                return false;
            }

            // If the pattern contains wildcard characters, convert it to a regex
            if (pattern.Contains('*') || pattern.Contains('?')) {
                string regexPattern = "^" + Regex.Escape(pattern)
                    .Replace("\\*", ".*")
                    .Replace("\\?", ".") + "$";

                return Regex.IsMatch(text, regexPattern, RegexOptions.IgnoreCase);
            }

            // For plain text patterns without wildcards, check if it occurs anywhere
            return text.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
