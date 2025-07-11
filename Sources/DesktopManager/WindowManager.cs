using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace DesktopManager;

/// <summary>
/// Provides methods to manage windows, including getting window information and controlling window states.
/// </summary>
public partial class WindowManager {
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
        /// <param name="processName">Optional process name filter. Supports wildcards.</param>
        /// <param name="className">Optional window class name filter. Supports wildcards.</param>
        /// <param name="regex">Optional regular expression to match the window title.</param>
        /// <returns>A list of WindowInfo objects.</returns>
        public List<WindowInfo> GetWindows(string name = "*", string processName = "*", string className = "*", Regex regex = null) {
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
                    var titleBuilder = new StringBuilder(titleLength + 1);
                    MonitorNativeMethods.GetWindowText(handle, titleBuilder, titleLength + 1);
                    var title = titleBuilder.ToString();

                    bool titleMatches = regex != null ? regex.IsMatch(title) : MatchesWildcard(title, name);
                    if (!titleMatches) {
                        continue;
                    }

                    uint processId = 0;
                    MonitorNativeMethods.GetWindowThreadProcessId(handle, out processId);

                    if (!string.IsNullOrEmpty(processName) && processName != "*") {
                        try {
                            var process = Process.GetProcessById((int)processId);
                            if (!MatchesWildcard(process.ProcessName, processName)) {
                                continue;
                            }
                        } catch {
                            continue;
                        }
                    }

                    if (!string.IsNullOrEmpty(className) && className != "*") {
                        var classBuilder = new StringBuilder(256);
                        MonitorNativeMethods.GetClassName(handle, classBuilder, classBuilder.Capacity);
                        if (!MatchesWildcard(classBuilder.ToString(), className)) {
                            continue;
                        }
                    }

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
