using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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
            SnapOptions = new WindowSnapOptions();
        }

        /// <summary>
        /// Gets all visible windows, optionally filtered by title, process name,
        /// class name, regular expression or process ID.
        /// </summary>
        /// <param name="name">Optional window title filter. Supports wildcards.</param>
        /// <param name="processName">Optional process name filter. Supports wildcards.</param>
        /// <param name="className">Optional window class name filter. Supports wildcards.</param>
        /// <param name="regex">Optional regular expression to match the window title.</param>
        /// <param name="processId">Optional process ID filter.</param>
        /// <param name="includeHidden">Whether to include hidden windows.</param>
        /// <param name="includeCloaked">Whether to include DWM-cloaked windows.</param>
        /// <param name="includeOwned">Whether to include owned top-level windows.</param>
        /// <returns>A list of WindowInfo objects.</returns>
        public List<WindowInfo> GetWindows(string name = "*", string processName = "*", string className = "*", Regex? regex = null, int processId = 0, bool includeHidden = false, bool includeCloaked = true, bool includeOwned = true) {     
            var options = new WindowQueryOptions {
                TitlePattern = name,
                ProcessNamePattern = processName,
                ClassNamePattern = className,
                TitleRegex = regex,
                ProcessId = processId,
                IncludeHidden = includeHidden,
                IncludeCloaked = includeCloaked,
                IncludeOwned = includeOwned
            };

            return GetWindows(options);
        }

        /// <summary>
        /// Gets windows using the specified query options.
        /// </summary>
        /// <param name="options">Window query options.</param>
        /// <returns>A list of WindowInfo objects.</returns>
        public List<WindowInfo> GetWindows(WindowQueryOptions options) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            var handles = new List<IntPtr>();
            var shellWindowhWnd = MonitorNativeMethods.GetShellWindow();
            bool includeHidden = options.IncludeHidden || options.IsVisible == false;
            IntPtr activeWindowHandle = options.ActiveWindow
                ? GetRootWindowHandle(MonitorNativeMethods.GetForegroundWindow())
                : IntPtr.Zero;

            if (!MonitorNativeMethods.EnumWindows(
                (handle, lParam) => {
                    if (handle != shellWindowhWnd) {
                        if (includeHidden || MonitorNativeMethods.IsWindowVisible(handle)) {
                            handles.Add(handle);
                        }
                    }
                    return true;
                }, IntPtr.Zero)) {
                throw new InvalidOperationException("Failed to enumerate windows");
            }

            var windows = new List<WindowInfo>();
            List<Monitor>? connectedMonitors = null;
            for (int index = 0; index < handles.Count; index++) {
                var handle = handles[index];
                if (options.ActiveWindow && handle != activeWindowHandle) {
                    continue;
                }

                if (options.Handle.HasValue && handle != options.Handle.Value) {
                    continue;
                }

                var ownerHandle = MonitorNativeMethods.GetWindow(handle, MonitorNativeMethods.GW_OWNER);
                if (!options.IncludeOwned && ownerHandle != IntPtr.Zero) {
                    continue;
                }

                bool isCloaked = false;
                MonitorNativeMethods.TryGetWindowCloaked(handle, out isCloaked);
                if (!options.IncludeCloaked && isCloaked) {
                    continue;
                }

                bool isVisible = MonitorNativeMethods.IsWindowVisible(handle);
                if (options.IsVisible.HasValue && options.IsVisible.Value != isVisible) {
                    continue;
                }

                uint windowProcessId = 0;
                uint windowThreadId = MonitorNativeMethods.GetWindowThreadProcessId(handle, out windowProcessId);
                if (options.ProcessId > 0 && windowProcessId != options.ProcessId) {
                    continue;
                }

                string? processName = null;
                if (!string.IsNullOrEmpty(options.ProcessNamePattern) && options.ProcessNamePattern != "*") {
                    try {
                        using var process = Process.GetProcessById((int)windowProcessId);
                        processName = process.ProcessName;
                        if (!MatchesWildcard(processName, options.ProcessNamePattern)) {
                            continue;
                        }
                    } catch {
                        continue;
                    }
                }

                string className = string.Empty;
                if (!string.IsNullOrEmpty(options.ClassNamePattern) && options.ClassNamePattern != "*") {
                    var classBuilder = new StringBuilder(256);
                    MonitorNativeMethods.GetClassName(handle, classBuilder, classBuilder.Capacity);
                    className = classBuilder.ToString();
                    if (!MatchesWildcard(className, options.ClassNamePattern)) {
                        continue;
                    }
                }

                var title = WindowTextHelper.GetWindowText(handle);

                // For process-specific queries, include windows even with empty titles
                // For name-based queries, skip empty titles unless using wildcard "*"
                bool includeEmptyTitles = options.IncludeEmptyTitles ?? (options.ProcessId > 0 ||
                    (options.TitlePattern == "*" && options.ProcessNamePattern == "*" && options.ClassNamePattern == "*" && options.TitleRegex == null));
                bool shouldInclude = includeEmptyTitles || !string.IsNullOrEmpty(title);

                if (!shouldInclude) {
                    continue;
                }

                // Skip title matching if title is empty and we're doing process-based search
                if (!string.IsNullOrEmpty(title) || options.ProcessId <= 0) {
                    bool titleMatches = options.TitleRegex != null ?
                        (string.IsNullOrEmpty(title) ? false : options.TitleRegex.IsMatch(title)) :
                        MatchesWildcard(title ?? string.Empty, options.TitlePattern);
                    if (!titleMatches) {
                        continue;
                    }
                }

                connectedMonitors ??= _monitors.GetMonitors(connectedOnly: true, refresh: true);
                var windowInfo = BuildWindowInfo(handle, title, windowProcessId, windowThreadId, ownerHandle, isCloaked, isVisible, index, connectedMonitors);

                if (options.State.HasValue && windowInfo.State != options.State.Value) {
                    continue;
                }

                if (options.IsTopMost.HasValue && windowInfo.IsTopMost != options.IsTopMost.Value) {
                    continue;
                }

                if (options.ZOrderMin.HasValue && windowInfo.ZOrder < options.ZOrderMin.Value) {
                    continue;
                }

                if (options.ZOrderMax.HasValue && windowInfo.ZOrder > options.ZOrderMax.Value) {
                    continue;
                }

                windows.Add(windowInfo);
            }

            return windows;
        }

        /// <summary>
        /// Enumerates child windows of the specified parent window.
        /// </summary>
        /// <param name="parent">Parent window information.</param>
        /// <param name="includeHidden">Whether to include hidden windows.</param>
        /// <param name="includeCloaked">Whether to include DWM-cloaked windows.</param>
        /// <returns>A list of child window information.</returns>
        public List<WindowInfo> GetChildWindows(WindowInfo parent, bool includeHidden = false, bool includeCloaked = true) {
            ValidateWindowInfo(parent);

            var handles = new List<IntPtr>();
            if (!MonitorNativeMethods.EnumChildWindows(
                parent.Handle,
                (handle, lParam) => {
                    if (includeHidden || MonitorNativeMethods.IsWindowVisible(handle)) {
                        handles.Add(handle);
                    }
                    return true;
                }, IntPtr.Zero)) {
                throw new InvalidOperationException("Failed to enumerate child windows");
            }

            var windows = new List<WindowInfo>();
            for (int index = 0; index < handles.Count; index++) {
                var handle = handles[index];
                bool isCloaked = false;
                MonitorNativeMethods.TryGetWindowCloaked(handle, out isCloaked);
                if (!includeCloaked && isCloaked) {
                    continue;
                }

                uint windowProcessId = 0;
                uint windowThreadId = MonitorNativeMethods.GetWindowThreadProcessId(handle, out windowProcessId);
                var ownerHandle = MonitorNativeMethods.GetWindow(handle, MonitorNativeMethods.GW_OWNER);
                var title = WindowTextHelper.GetWindowText(handle);
                bool isVisible = MonitorNativeMethods.IsWindowVisible(handle);
                windows.Add(BuildWindowInfo(handle, title, windowProcessId, windowThreadId, ownerHandle, isCloaked, isVisible, index, connectedMonitors: null));
            }

            return windows;
        }

        /// <summary>
        /// Finds windows by process name and title pattern.
        /// </summary>
        /// <param name="processName">Name of the process (e.g., "notepad").</param>
        /// <param name="titlePattern">Optional title pattern to match.</param>
        /// <param name="includeHidden">Whether to include hidden windows.</param>
        /// <returns>List of matching windows.</returns>
        public List<WindowInfo> FindWindowsByProcess(string processName, string titlePattern = "*", bool includeHidden = false) {
            if (string.IsNullOrEmpty(processName)) {
                throw new ArgumentNullException(nameof(processName));
            }
            
            // Get all windows matching the title pattern
            var windows = GetWindows(name: titlePattern, includeHidden: includeHidden);
            
            // Filter by process name
            var result = new List<WindowInfo>();
            foreach (var window in windows) {
                try {
                    using var proc = Process.GetProcessById((int)window.ProcessId);
                    if (string.Equals(proc.ProcessName, processName, StringComparison.OrdinalIgnoreCase)) {
                        result.Add(window);
                    }
                } catch {
                    // Process might have exited, skip it
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Gets windows belonging to the specified process.
        /// </summary>
        /// <param name="process">Process whose windows to retrieve.</param>
        /// <param name="includeHidden">Whether to include hidden windows.</param>
        /// <param name="includeRelatedProcesses">Whether helper processes with the same name should also be searched when the exact process has no windows.</param>
        /// <returns>List of windows owned by the process.</returns>
        public List<WindowInfo> GetWindowsForProcess(Process process, bool includeHidden = false, bool includeRelatedProcesses = false) {
            if (process == null) {
                throw new ArgumentNullException(nameof(process));
            }

            // First try direct process ID match
            var windows = GetWindows(processId: process.Id, includeHidden: includeHidden);
            
            // If no windows found and process has a MainWindowHandle, try to find it
            if (windows.Count == 0 && process.MainWindowHandle != IntPtr.Zero) {
                var allWindows = GetWindows(includeHidden: includeHidden);
                var mainWindow = allWindows.FirstOrDefault(w => w.Handle == process.MainWindowHandle);
                if (mainWindow != null) {
                    windows.Add(mainWindow);
                }
            }
            
            // Some modern apps surface a window from a broker or helper process.
            // Keep this behavior opt-in so the default method stays exact-process safe.
            if (windows.Count == 0 && includeRelatedProcesses) {
                try {
                    // Get all processes with the same name
                    var relatedProcesses = Process.GetProcessesByName(process.ProcessName);
                    foreach (var related in relatedProcesses) {
                        if (related.Id != process.Id) {
                            var relatedWindows = GetWindows(processId: related.Id, includeHidden: includeHidden);
                            windows.AddRange(relatedWindows);
                        }
                        related.Dispose();
                    }
                } catch {
                    // Ignore errors when checking related processes
                }
            }
            
            return windows;
        }

        /// <summary>
        /// Gets windows belonging to the specified process or related helper processes with the same name.
        /// </summary>
        /// <param name="process">Process whose windows to retrieve.</param>
        /// <param name="includeHidden">Whether to include hidden windows.</param>
        /// <returns>List of windows owned by the process family.</returns>
        public List<WindowInfo> GetWindowsForProcessFamily(Process process, bool includeHidden = false) {
            return GetWindowsForProcess(process, includeHidden, includeRelatedProcesses: true);
        }
        
        /// <summary>
        /// Gets the position of a window.
        /// </summary>
        /// <param name="windowInfo">The window information.</param>
        /// <returns>The window position.</returns>
        public WindowPosition GetWindowPosition(WindowInfo windowInfo) {        
            ValidateWindowInfo(windowInfo);
            RECT rect = new RECT();
            if (MonitorNativeMethods.GetWindowRect(windowInfo.Handle, out rect)) {
                // Re-evaluate the current state directly from the window to avoid stale data
                IntPtr stylePtr = MonitorNativeMethods.GetWindowLongPtr(windowInfo.Handle, MonitorNativeMethods.GWL_STYLE);
                long style = stylePtr.ToInt64();
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

        private WindowInfo BuildWindowInfo(IntPtr handle, string? title, uint windowProcessId, uint windowThreadId, IntPtr ownerHandle, bool isCloaked, bool isVisible, int zOrder, IReadOnlyList<Monitor>? connectedMonitors) {
            var windowInfo = new WindowInfo {
                Title = title ?? string.Empty,
                Handle = handle,
                ProcessId = windowProcessId,
                ThreadId = windowThreadId,
                IsVisible = isVisible,
                OwnerHandle = ownerHandle,
                ParentHandle = MonitorNativeMethods.GetParent(handle),
                IsCloaked = isCloaked,
                ZOrder = zOrder
            };

            IntPtr stylePtr = MonitorNativeMethods.GetWindowLongPtr(handle, MonitorNativeMethods.GWL_STYLE);
            long style = stylePtr.ToInt64();
            if ((style & MonitorNativeMethods.WS_MINIMIZE) != 0) {
                windowInfo.State = WindowState.Minimize;
            } else if ((style & MonitorNativeMethods.WS_MAXIMIZE) != 0) {
                windowInfo.State = WindowState.Maximize;
            } else {
                windowInfo.State = WindowState.Normal;
            }

            IntPtr exStylePtr = MonitorNativeMethods.GetWindowLongPtr(handle, MonitorNativeMethods.GWL_EXSTYLE);
            long exStyle = exStylePtr.ToInt64();
            windowInfo.IsTopMost = (exStyle & MonitorNativeMethods.WS_EX_TOPMOST) != 0;

            // Get window position and state
            RECT rect = new RECT();
            if (MonitorNativeMethods.GetWindowRect(handle, out rect)) {
                windowInfo.Left = rect.Left;
                windowInfo.Top = rect.Top;
                windowInfo.Right = rect.Right;
                windowInfo.Bottom = rect.Bottom;

                IReadOnlyList<Monitor> monitors = connectedMonitors ?? _monitors.GetMonitors(connectedOnly: true, refresh: true);
                foreach (var monitor in monitors) {
                    RECT monitorRect = monitor.Rect;
                    if (monitorRect.Right <= monitorRect.Left || monitorRect.Bottom <= monitorRect.Top) {
                        continue;
                    }

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

            return windowInfo;
        }
    }
