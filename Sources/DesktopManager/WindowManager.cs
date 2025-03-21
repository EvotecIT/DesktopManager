using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

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

            // If no wildcard, do a case-insensitive contains check
            return text.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Sends text to a window using the specified method.
        /// </summary>
        /// <param name="windowInfo">The window information.</param>
        /// <param name="text">The text to send.</param>
        /// <param name="method">The method to use for sending text.</param>
        /// <exception cref="InvalidOperationException">Thrown when failed to send text to the window.</exception>
        public void SendText(WindowInfo windowInfo, string text, TextSendMethod method = TextSendMethod.Type) {
            // Make sure the window is active
            if (!ActivateWindow(windowInfo.Handle)) {
                throw new InvalidOperationException("Failed to activate window");
            }

            // Small delay to ensure the window is ready
            Thread.Sleep(100);

            switch (method) {
                case TextSendMethod.Type:
                    TypeText(windowInfo.Handle, text);
                    break;
                case TextSendMethod.Paste:
                    PasteText(windowInfo.Handle, text);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(method), method, "Unsupported text sending method");
            }
        }

        /// <summary>
        /// Activates the specified window.
        /// </summary>
        /// <param name="hWnd">The window handle.</param>
        /// <returns>True if successful, otherwise false.</returns>
        private bool ActivateWindow(IntPtr hWnd) {
            if (hWnd == IntPtr.Zero) {
                return false;
            }

            // Get foreground thread
            uint foregroundThreadId = MonitorNativeMethods.GetWindowThreadProcessId(
                MonitorNativeMethods.GetForegroundWindow(), out _);

            // Get current thread
            uint currentThreadId = MonitorNativeMethods.GetCurrentThreadId();

            // Attach threads if necessary
            bool threadsAttached = false;
            if (foregroundThreadId != currentThreadId) {
                // Attach foreground thread to our thread
                threadsAttached = MonitorNativeMethods.AttachThreadInput(
                    foregroundThreadId, currentThreadId, true);
            }

            // Bring window to foreground and set focus
            bool result = MonitorNativeMethods.SetForegroundWindow(hWnd);
            MonitorNativeMethods.SetFocus(hWnd);

            // Detach threads if they were attached
            if (threadsAttached) {
                MonitorNativeMethods.AttachThreadInput(
                    foregroundThreadId, currentThreadId, false);
            }

            // Allow window to process focus change
            Thread.Sleep(50);
            return result;
        }

        /// <summary>
        /// Types text character by character to the specified window.
        /// </summary>
        /// <param name="hWnd">The window handle.</param>
        /// <param name="text">The text to type.</param>
        private void TypeText(IntPtr hWnd, string text) {
            if (string.IsNullOrEmpty(text)) {
                return;
            }

            // Process each character in the text
            foreach (char c in text) {
                // Handle special characters
                switch (c) {
                    case '\r':
                        // Skip carriage returns as we'll handle newlines with '\n'
                        continue;
                    case '\n':
                        // Send Enter key for newline
                        MonitorNativeMethods.SendMessage(hWnd,
                            MonitorNativeMethods.WM_CHAR,
                            (uint)'\r', 0);
                        Thread.Sleep(5); // Small delay between characters
                        continue;
                    case '\t':
                        // Send Tab key
                        MonitorNativeMethods.SendMessage(hWnd,
                            MonitorNativeMethods.WM_CHAR,
                            (uint)'\t', 0);
                        Thread.Sleep(5); // Small delay between characters
                        continue;
                }

                // Send regular character
                MonitorNativeMethods.SendMessage(hWnd,
                    MonitorNativeMethods.WM_CHAR,
                    (uint)c, 0);

                // Small delay between characters to simulate realistic typing
                Thread.Sleep(5);
            }
        }

        /// <summary>
        /// Pastes text to the specified window using the clipboard.
        /// </summary>
        /// <param name="hWnd">The window handle.</param>
        /// <param name="text">The text to paste.</param>
        /// <exception cref="InvalidOperationException">Thrown when clipboard operations fail.</exception>
        private void PasteText(IntPtr hWnd, string text) {
            if (string.IsNullOrEmpty(text)) {
                return;
            }

            // Store the current clipboard content to restore later
            string originalClipboardText = GetClipboardText();

            try {
                // Set new clipboard content
                if (!SetClipboardText(text)) {
                    throw new InvalidOperationException("Failed to set clipboard text");
                }

                // Small delay to ensure clipboard is ready
                Thread.Sleep(50);

                // Send Ctrl+V to paste
                // Press Ctrl
                MonitorNativeMethods.INPUT[] inputs = new MonitorNativeMethods.INPUT[4];

                // Press Ctrl
                inputs[0].type = MonitorNativeMethods.INPUT_KEYBOARD;
                inputs[0].u.ki.wVk = MonitorNativeMethods.VK_CONTROL;
                inputs[0].u.ki.wScan = 0;
                inputs[0].u.ki.dwFlags = 0;
                inputs[0].u.ki.time = 0;
                inputs[0].u.ki.dwExtraInfo = IntPtr.Zero;

                // Press V
                inputs[1].type = MonitorNativeMethods.INPUT_KEYBOARD;
                inputs[1].u.ki.wVk = MonitorNativeMethods.VK_V;
                inputs[1].u.ki.wScan = 0;
                inputs[1].u.ki.dwFlags = 0;
                inputs[1].u.ki.time = 0;
                inputs[1].u.ki.dwExtraInfo = IntPtr.Zero;

                // Release V
                inputs[2].type = MonitorNativeMethods.INPUT_KEYBOARD;
                inputs[2].u.ki.wVk = MonitorNativeMethods.VK_V;
                inputs[2].u.ki.wScan = 0;
                inputs[2].u.ki.dwFlags = MonitorNativeMethods.KEYEVENTF_KEYUP;
                inputs[2].u.ki.time = 0;
                inputs[2].u.ki.dwExtraInfo = IntPtr.Zero;

                // Release Ctrl
                inputs[3].type = MonitorNativeMethods.INPUT_KEYBOARD;
                inputs[3].u.ki.wVk = MonitorNativeMethods.VK_CONTROL;
                inputs[3].u.ki.wScan = 0;
                inputs[3].u.ki.dwFlags = MonitorNativeMethods.KEYEVENTF_KEYUP;
                inputs[3].u.ki.time = 0;
                inputs[3].u.ki.dwExtraInfo = IntPtr.Zero;

                // Send keyboard input
                MonitorNativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(MonitorNativeMethods.INPUT)));

                // Allow time for paste to complete
                Thread.Sleep(100);
            }
            finally {
                // Restore original clipboard content
                if (originalClipboardText != null) {
                    SetClipboardText(originalClipboardText);
                }
            }
        }

        /// <summary>
        /// Gets the current text from the clipboard.
        /// </summary>
        /// <returns>The clipboard text, or null if no text is available.</returns>
        private string GetClipboardText() {
            if (!MonitorNativeMethods.OpenClipboard(IntPtr.Zero)) {
                return null;
            }

            try {
                IntPtr hClipboardData = MonitorNativeMethods.GetClipboardData(MonitorNativeMethods.CF_UNICODETEXT);
                if (hClipboardData == IntPtr.Zero) {
                    return null;
                }

                IntPtr pchData = MonitorNativeMethods.GlobalLock(hClipboardData);
                if (pchData == IntPtr.Zero) {
                    return null;
                }

                try {
                    return Marshal.PtrToStringUni(pchData);
                }
                finally {
                    MonitorNativeMethods.GlobalUnlock(hClipboardData);
                }
            }
            finally {
                MonitorNativeMethods.CloseClipboard();
            }
        }

        /// <summary>
        /// Sets text to the clipboard.
        /// </summary>
        /// <param name="text">The text to set.</param>
        /// <returns>True if successful, otherwise false.</returns>
        private bool SetClipboardText(string text) {
            if (!MonitorNativeMethods.OpenClipboard(IntPtr.Zero)) {
                return false;
            }

            try {
                // Empty the clipboard
                if (!MonitorNativeMethods.EmptyClipboard()) {
                    return false;
                }

                // Allocate global memory for the text
                int size = (text.Length + 1) * 2; // Size in bytes for Unicode string (including null terminator)
                IntPtr hGlobal = MonitorNativeMethods.GlobalAlloc(MonitorNativeMethods.GMEM_MOVEABLE, (uint)size);
                if (hGlobal == IntPtr.Zero) {
                    return false;
                }

                try {
                    // Lock the memory and copy the data
                    IntPtr pGlobal = MonitorNativeMethods.GlobalLock(hGlobal);
                    if (pGlobal == IntPtr.Zero) {
                        return false;
                    }

                    try {
                        Marshal.Copy(text.ToCharArray(), 0, pGlobal, text.Length);
                        // Set the null terminator
                        Marshal.WriteInt16(pGlobal, text.Length * 2, 0);
                    }
                    finally {
                        MonitorNativeMethods.GlobalUnlock(hGlobal);
                    }

                    // Set the clipboard data
                    IntPtr result = MonitorNativeMethods.SetClipboardData(MonitorNativeMethods.CF_UNICODETEXT, hGlobal);
                    if (result == IntPtr.Zero) {
                        return false;
                    }

                    // The system now owns the memory, so don't free it
                    hGlobal = IntPtr.Zero;
                    return true;
                }
                finally {
                    // Free the memory if SetClipboardData failed
                    if (hGlobal != IntPtr.Zero) {
                        MonitorNativeMethods.GlobalFree(hGlobal);
                    }
                }
            }
            finally {
                MonitorNativeMethods.CloseClipboard();
            }
        }
    }
}