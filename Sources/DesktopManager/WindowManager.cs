using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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
                        if (MonitorNativeMethods.GetWindowRect(handle, out RECT rect)) {
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
            if (MonitorNativeMethods.GetWindowRect(windowInfo.Handle, out RECT rect)) {
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
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern)) {
                return false;
            }

            if (pattern == "*") {
                return true;
            }

            // Handle patterns with multiple wildcards
            if (pattern.Contains("*")) {
                // Convert wildcard pattern to regex pattern
                string regexPattern = "^" + Regex.Escape(pattern)
                    .Replace("\\*", ".*")
                    .Replace("\\?", ".")
                    + "$";

                // Use .NET regex for more robust wildcard matching
                return Regex.IsMatch(text, regexPattern, RegexOptions.IgnoreCase);
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
            if (windowInfo == null) {
                throw new ArgumentNullException(nameof(windowInfo));
            }

            if (string.IsNullOrEmpty(text)) {
                return;
            }

            // Restore window if minimized
            if (windowInfo.State == WindowState.Minimize) {
                RestoreWindow(windowInfo);
                Thread.Sleep(200);
            }

            // First try standard activation
            if (!ActivateWindowCompletely(windowInfo.Handle)) {
                throw new InvalidOperationException($"Failed to activate window '{windowInfo.Title}'");
            }

            // Wait for window to be ready
            Thread.Sleep(300);

            // Try to detect if this is a modern Windows app (like Notepad)
            bool isModernApp = windowInfo.Title.Contains("Notepad") ||
                              (windowInfo.Title.Contains("Remote Desktop") && !windowInfo.Title.Contains("Connection"));

            if (isModernApp && method == TextSendMethod.Type) {
                // Use specialized method for modern apps
                TypeTextUsingVirtualKeys(windowInfo.Handle, text);
            } else {
                switch (method) {
                    case TextSendMethod.Type:
                        TypeTextEnhanced(windowInfo.Handle, text);
                        break;
                    case TextSendMethod.Paste:
                        PasteTextEnhanced(windowInfo.Handle, text);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(method), method, "Unsupported text sending method");
                }
            }
        }

        /// <summary>
        /// Ensures a window is completely activated and ready to receive input
        /// </summary>
        private bool ActivateWindowCompletely(IntPtr hWnd) {
            if (hWnd == IntPtr.Zero) {
                return false;
            }

            try {
                // Get window information for better handling
                uint processId = 0;
                MonitorNativeMethods.GetWindowThreadProcessId(hWnd, out processId);

                // First restore the window if minimized
                MonitorNativeMethods.ShowWindow(hWnd, MonitorNativeMethods.SW_RESTORE);
                Thread.Sleep(100);

                // For applications like Remote Desktop Manager, we need a more aggressive approach
                Process process = null;
                string processName = string.Empty;

                try {
                    process = Process.GetProcessById((int)processId);
                    processName = process?.ProcessName?.ToLowerInvariant() ?? string.Empty;
                } catch {
                    // Process might have terminated
                }

                bool isRemoteDesktopManager = processName.Contains("remotedesktopmanager") ||
                                             processName.Contains("rdm") ||
                                             processName.Contains("devolutions");

                if (isRemoteDesktopManager) {
                    // Special handling for Remote Desktop Manager
                    // First try to show and restore the window
                    MonitorNativeMethods.ShowWindow(hWnd, MonitorNativeMethods.SW_NORMAL);
                    Thread.Sleep(200);

                    // For Remote Desktop Manager, sometimes we need to simulate Alt+Tab to activate
                    SimulateAltTab(hWnd);
                    Thread.Sleep(300);

                    // Now try normal activation
                    MonitorNativeMethods.SetForegroundWindow(hWnd);
                    Thread.Sleep(100);

                    return true; // Assume success for RDM to continue with sending text
                }

                // Try multiple strategies to bring window to foreground
                bool success = false;

                // Strategy 1: SetForegroundWindow
                success = MonitorNativeMethods.SetForegroundWindow(hWnd);
                Thread.Sleep(50);

                // Strategy 2: BringWindowToTop
                MonitorNativeMethods.BringWindowToTop(hWnd);
                Thread.Sleep(50);

                // Strategy 3: Thread attachment
                uint foregroundThreadId = MonitorNativeMethods.GetWindowThreadProcessId(
                    MonitorNativeMethods.GetForegroundWindow(), out _);
                uint currentThreadId = MonitorNativeMethods.GetCurrentThreadId();

                if (foregroundThreadId != currentThreadId) {
                    bool attached = MonitorNativeMethods.AttachThreadInput(foregroundThreadId, currentThreadId, true);
                    if (attached) {
                        MonitorNativeMethods.SetForegroundWindow(hWnd);
                        MonitorNativeMethods.SetFocus(hWnd);
                        MonitorNativeMethods.AttachThreadInput(foregroundThreadId, currentThreadId, false);
                    }
                }

                // Strategy 4: Try ALT keypress to force focus (helps with some applications)
                SendInputVirtualKey(MonitorNativeMethods.VK_MENU);
                Thread.Sleep(50);
                MonitorNativeMethods.SetForegroundWindow(hWnd);
                Thread.Sleep(50);

                // Final check if the window is now in foreground
                IntPtr foregroundWindow = MonitorNativeMethods.GetForegroundWindow();
                success = foregroundWindow == hWnd;

                // Set focus one more time just to be sure
                if (success) {
                    MonitorNativeMethods.SetFocus(hWnd);
                }

                return success;
            } catch (Exception ex) {
                Debug.WriteLine($"Error in ActivateWindowCompletely: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Simulates Alt+Tab to get to a specific window
        /// </summary>
        private void SimulateAltTab(IntPtr targetWindow) {
            // Press Alt
            MonitorNativeMethods.INPUT[] inputs = new MonitorNativeMethods.INPUT[1];
            inputs[0].type = MonitorNativeMethods.INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = MonitorNativeMethods.VK_MENU;
            inputs[0].u.ki.wScan = 0;
            inputs[0].u.ki.dwFlags = 0;
            inputs[0].u.ki.time = 0;
            inputs[0].u.ki.dwExtraInfo = IntPtr.Zero;
            MonitorNativeMethods.SendInput(1, inputs, Marshal.SizeOf(typeof(MonitorNativeMethods.INPUT)));
            Thread.Sleep(50);

            // Press Tab
            inputs[0].u.ki.wVk = MonitorNativeMethods.VK_TAB;
            MonitorNativeMethods.SendInput(1, inputs, Marshal.SizeOf(typeof(MonitorNativeMethods.INPUT)));
            Thread.Sleep(100);

            // Release Tab
            inputs[0].u.ki.wVk = MonitorNativeMethods.VK_TAB;
            inputs[0].u.ki.dwFlags = MonitorNativeMethods.KEYEVENTF_KEYUP;
            MonitorNativeMethods.SendInput(1, inputs, Marshal.SizeOf(typeof(MonitorNativeMethods.INPUT)));
            Thread.Sleep(50);

            // Check if we're on the target window
            IntPtr currentWindow = MonitorNativeMethods.GetForegroundWindow();
            if (currentWindow == targetWindow) {
                // Release Alt
                inputs[0].u.ki.wVk = MonitorNativeMethods.VK_MENU;
                inputs[0].u.ki.dwFlags = MonitorNativeMethods.KEYEVENTF_KEYUP;
                MonitorNativeMethods.SendInput(1, inputs, Marshal.SizeOf(typeof(MonitorNativeMethods.INPUT)));
                return;
            }

            // Try cycling through windows (up to 10 attempts)
            for (int i = 0; i < 10; i++) {
                // Press Tab again
                inputs[0].u.ki.wVk = MonitorNativeMethods.VK_TAB;
                inputs[0].u.ki.dwFlags = 0;
                MonitorNativeMethods.SendInput(1, inputs, Marshal.SizeOf(typeof(MonitorNativeMethods.INPUT)));
                Thread.Sleep(50);

                // Release Tab
                inputs[0].u.ki.wVk = MonitorNativeMethods.VK_TAB;
                inputs[0].u.ki.dwFlags = MonitorNativeMethods.KEYEVENTF_KEYUP;
                MonitorNativeMethods.SendInput(1, inputs, Marshal.SizeOf(typeof(MonitorNativeMethods.INPUT)));
                Thread.Sleep(50);

                // Check if we found the window
                currentWindow = MonitorNativeMethods.GetForegroundWindow();
                if (currentWindow == targetWindow) {
                    break;
                }
            }

            // Release Alt
            inputs[0].u.ki.wVk = MonitorNativeMethods.VK_MENU;
            inputs[0].u.ki.dwFlags = MonitorNativeMethods.KEYEVENTF_KEYUP;
            MonitorNativeMethods.SendInput(1, inputs, Marshal.SizeOf(typeof(MonitorNativeMethods.INPUT)));
        }

        /// <summary>
        /// Types text character by character to the specified window using enhanced method
        /// </summary>
        private void TypeTextEnhanced(IntPtr hWnd, string text) {
            if (string.IsNullOrEmpty(text)) {
                return;
            }

            foreach (char c in text) {
                // Handle special characters
                switch (c) {
                    case '\r':
                        // Skip carriage returns as we'll handle newlines with '\n'
                        continue;
                    case '\n':
                        // Send Enter key
                        SendVirtualKey(hWnd, MonitorNativeMethods.VK_RETURN);
                        Thread.Sleep(10);
                        continue;
                    case '\t':
                        // Send Tab key
                        SendVirtualKey(hWnd, MonitorNativeMethods.VK_TAB);
                        Thread.Sleep(10);
                        continue;
                }

                // Only use one method to avoid duplication
                // Use SendMessage for more reliability across applications
                MonitorNativeMethods.SendMessage(hWnd, MonitorNativeMethods.WM_CHAR, (uint)c, 0);

                // Small delay between characters for reliability
                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// Types text using virtual key codes instead of WM_CHAR - better for modern Windows apps
        /// </summary>
        private void TypeTextUsingVirtualKeys(IntPtr hWnd, string text) {
            // This method uses a different approach for modern Windows apps
            // that might not properly handle WM_CHAR messages

            foreach (char c in text) {
                // Handle special characters
                switch (c) {
                    case '\r':
                        continue;
                    case '\n':
                        // Send Enter key
                        SendInputVirtualKey(MonitorNativeMethods.VK_RETURN);
                        Thread.Sleep(15);
                        continue;
                    case '\t':
                        // Send Tab key
                        SendInputVirtualKey(MonitorNativeMethods.VK_TAB);
                        Thread.Sleep(15);
                        continue;
                    case ' ':
                        // Send Space key
                        SendInputVirtualKey(MonitorNativeMethods.VK_SPACE);
                        Thread.Sleep(15);
                        continue;
                }

                // For regular characters, use SendInput with appropriate key codes
                // This handles the translation of characters to key presses
                SendCharacterAsScanCode(c);
                Thread.Sleep(15);
            }
        }

        /// <summary>
        /// Send a virtual key press and release to the window
        /// </summary>
        private void SendVirtualKey(IntPtr hWnd, ushort keyCode) {
            // Key down
            MonitorNativeMethods.PostMessage(hWnd, MonitorNativeMethods.WM_KEYDOWN, keyCode, 0);
            Thread.Sleep(5);

            // Key up
            MonitorNativeMethods.PostMessage(hWnd, MonitorNativeMethods.WM_KEYUP, keyCode, 0);
            Thread.Sleep(5);
        }

        /// <summary>
        /// Sends a virtual key press using SendInput API - bypasses window message handling
        /// </summary>
        private void SendInputVirtualKey(ushort keyCode) {
            // Create input event array
            MonitorNativeMethods.INPUT[] inputs = new MonitorNativeMethods.INPUT[2];

            // Key down
            inputs[0].type = MonitorNativeMethods.INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = keyCode;
            inputs[0].u.ki.wScan = 0;
            inputs[0].u.ki.dwFlags = 0;
            inputs[0].u.ki.time = 0;
            inputs[0].u.ki.dwExtraInfo = IntPtr.Zero;

            // Key up
            inputs[1].type = MonitorNativeMethods.INPUT_KEYBOARD;
            inputs[1].u.ki.wVk = keyCode;
            inputs[1].u.ki.wScan = 0;
            inputs[1].u.ki.dwFlags = MonitorNativeMethods.KEYEVENTF_KEYUP;
            inputs[1].u.ki.time = 0;
            inputs[1].u.ki.dwExtraInfo = IntPtr.Zero;

            // Send inputs
            MonitorNativeMethods.SendInput(2, inputs, Marshal.SizeOf(typeof(MonitorNativeMethods.INPUT)));
        }

        /// <summary>
        /// Sends a character as a keyboard scan code using SendInput
        /// </summary>
        private void SendCharacterAsScanCode(char c) {
            // For modern Windows apps, use UNICODE input to send characters
            MonitorNativeMethods.INPUT[] inputs = new MonitorNativeMethods.INPUT[1];

            inputs[0].type = MonitorNativeMethods.INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = 0; // No virtual key code
            inputs[0].u.ki.wScan = (ushort)c; // Character as scan code
            inputs[0].u.ki.dwFlags = MonitorNativeMethods.KEYEVENTF_UNICODE; // Use UNICODE flag
            inputs[0].u.ki.time = 0;
            inputs[0].u.ki.dwExtraInfo = IntPtr.Zero;

            MonitorNativeMethods.SendInput(1, inputs, Marshal.SizeOf(typeof(MonitorNativeMethods.INPUT)));
        }

        /// <summary>
        /// Pastes text to the specified window using the clipboard and a single paste strategy
        /// to avoid duplicate keystrokes.
        /// </summary>
        private void PasteTextEnhanced(IntPtr hWnd, string text) {
            if (string.IsNullOrEmpty(text)) {
                return;
            }

            // Store the current clipboard content
            string originalClipboardText = GetClipboardText();

            try {
                // Set new clipboard content
                if (!SetClipboardText(text)) {
                    throw new InvalidOperationException("Failed to set clipboard text");
                }

                // Small delay to ensure clipboard is ready
                Thread.Sleep(100);

                // Use only one paste strategy to avoid duplication
                // Strategy: Send Ctrl+V using SendInput for best compatibility
                SendCtrlVUsingInputs();

                // Allow time for paste operation to complete
                Thread.Sleep(200);
            } finally {
                // Restore original clipboard content
                if (originalClipboardText != null) {
                    SetClipboardText(originalClipboardText);
                }
            }
        }

        /// <summary>
        /// Sends Ctrl+V using the SendInput method
        /// </summary>
        private void SendCtrlVUsingInputs() {
            // Create input events array for Ctrl+V sequence
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

            // Send the input events
            MonitorNativeMethods.SendInput(4, inputs, Marshal.SizeOf(typeof(MonitorNativeMethods.INPUT)));
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
                } finally {
                    MonitorNativeMethods.GlobalUnlock(hClipboardData);
                }
            } finally {
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
                    } finally {
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
                } finally {
                    // Free the memory if SetClipboardData failed
                    if (hGlobal != IntPtr.Zero) {
                        MonitorNativeMethods.GlobalFree(hGlobal);
                    }
                }
            } finally {
                MonitorNativeMethods.CloseClipboard();
            }
        }
    }
}