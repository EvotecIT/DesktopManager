using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;

namespace DesktopManager;

/// <summary>
/// Provides methods for pasting or typing text into windows.
/// </summary>
[SupportedOSPlatform("windows")]
public static class WindowInputService {
    private const int NativeTextVerificationTimeoutMilliseconds = 1000;

    internal enum WindowTextDeliveryMode {
        ForegroundInput,
        WindowMessage
    }

    internal readonly struct WindowScriptChunk {
        public WindowScriptChunk(string text, bool sendLineBreak) {
            Text = text ?? string.Empty;
            SendLineBreak = sendLineBreak;
        }

        public string Text { get; }

        public bool SendLineBreak { get; }
    }

    /// <summary>
    /// Pastes the specified text into the window using the clipboard.
    /// </summary>
    /// <param name="window">Target window.</param>
    /// <param name="text">Text to paste.</param>
    public static void PasteText(WindowInfo window, string text) {
        PasteText(window, text, null);
    }

    /// <summary>
    /// Pastes the specified text into the window using the clipboard and options.
    /// </summary>
    /// <param name="window">Target window.</param>
    /// <param name="text">Text to paste.</param>
    /// <param name="options">Input options.</param>
    public static void PasteText(WindowInfo window, string text, WindowInputOptions? options) {
        ValidateWindow(window);
        if (text == null) {
            throw new ArgumentNullException(nameof(text));
        }

        WindowInputOptions settings = options ?? new WindowInputOptions();
        NormalizeOptions(settings);

        IntPtr previousForeground = IntPtr.Zero;
        if (settings.ActivateWindow || settings.RestoreFocus) {
            previousForeground = MonitorNativeMethods.GetForegroundWindow();
        }

        string? clipboardBackup = null;
        bool restoreClipboard = false;
        if (settings.PreserveClipboard) {
            restoreClipboard = ClipboardHelper.TryGetText(out clipboardBackup, settings.ClipboardRetryCount, settings.ClipboardRetryDelayMilliseconds);
        }

        ClipboardHelper.SetText(text, settings.ClipboardRetryCount, settings.ClipboardRetryDelayMilliseconds);

        if (settings.ActivateWindow) {
            TryActivateWindow(window.Handle, settings.ActivationRetryCount, settings.ActivationRetryDelayMilliseconds);
        }

        SendPaste(window.Handle, settings.InputRetryCount, settings.ActivationRetryDelayMilliseconds);
        EnsureTextApplied(window, text);

        if (settings.RestoreFocus && previousForeground != IntPtr.Zero && previousForeground != window.Handle) {
            MonitorNativeMethods.SetForegroundWindow(previousForeground);
        }

        if (settings.PreserveClipboard && restoreClipboard) {
            ClipboardHelper.SetText(clipboardBackup ?? string.Empty, settings.ClipboardRetryCount, settings.ClipboardRetryDelayMilliseconds);
        }
    }

    /// <summary>
    /// Types the specified text into the window using simulated keyboard input.
    /// </summary>
    /// <param name="window">Target window.</param>
    /// <param name="text">Text to type.</param>
    /// <param name="delay">Optional delay in milliseconds between characters.</param>
    public static void TypeText(WindowInfo window, string text, int delay = 0) {
        var options = new WindowInputOptions {
            KeyDelayMilliseconds = delay
        };

        TypeText(window, text, options);
    }

    /// <summary>
    /// Types the specified text into the window using options.
    /// </summary>
    /// <param name="window">Target window.</param>
    /// <param name="text">Text to type.</param>
    /// <param name="options">Input options.</param>
    public static void TypeText(WindowInfo window, string text, WindowInputOptions? options) {
        ValidateWindow(window);
        if (text == null) {
            throw new ArgumentNullException(nameof(text));
        }

        WindowInputOptions settings = options ?? new WindowInputOptions();
        NormalizeOptions(settings);

        IntPtr previousForeground = IntPtr.Zero;
        if (settings.ActivateWindow || settings.RestoreFocus) {
            previousForeground = MonitorNativeMethods.GetForegroundWindow();
        }

        if (settings.ActivateWindow) {
            TryActivateWindow(window.Handle, settings.ActivationRetryCount, settings.ActivationRetryDelayMilliseconds);
        }

        bool targetOwnsForeground = MonitorNativeMethods.GetForegroundWindow() == window.Handle;
        WindowTextDeliveryMode deliveryMode = ResolveTextDeliveryMode(settings, targetOwnsForeground);
        if (settings.TypeTextAsScript) {
            SendScriptText(window, text, settings, deliveryMode);
        } else {
            if (deliveryMode == WindowTextDeliveryMode.ForegroundInput) {
                SendForegroundText(window, text, settings);
            } else {
                IntPtr targetHandle = ResolvePreferredTextHandle(window.Handle);
                SendMessageText(targetHandle, text, settings.KeyDelayMilliseconds);
                EnsureTextApplied(window, text);
            }
        }

        if (settings.RestoreFocus && previousForeground != IntPtr.Zero && previousForeground != window.Handle) {
            MonitorNativeMethods.SetForegroundWindow(previousForeground);
        }
    }

    /// <summary>
    /// Sends one or more keys to the specified window.
    /// </summary>
    /// <param name="window">Target window.</param>
    /// <param name="keys">Keys to send.</param>
    public static void SendKeys(WindowInfo window, params VirtualKey[] keys) {
        SendKeys(window, keys, null);
    }

    /// <summary>
    /// Sends one or more keys to the specified window using input options.
    /// </summary>
    /// <param name="window">Target window.</param>
    /// <param name="keys">Keys to send.</param>
    /// <param name="options">Input options.</param>
    public static void SendKeys(WindowInfo window, IReadOnlyList<VirtualKey> keys, WindowInputOptions? options) {
        ValidateWindow(window);
        if (keys == null) {
            throw new ArgumentNullException(nameof(keys));
        }
        if (keys.Count == 0) {
            throw new ArgumentException("No keys specified", nameof(keys));
        }

        WindowInputOptions settings = options ?? new WindowInputOptions();
        NormalizeOptions(settings);

        IntPtr previousForeground = IntPtr.Zero;
        if (settings.ActivateWindow || settings.RestoreFocus) {
            previousForeground = MonitorNativeMethods.GetForegroundWindow();
        }

        if (settings.ActivateWindow) {
            TryActivateWindow(window.Handle, settings.ActivationRetryCount, settings.ActivationRetryDelayMilliseconds);
        }

        if (MonitorNativeMethods.GetForegroundWindow() != window.Handle) {
            throw new InvalidOperationException("Window must own the foreground before sending window-level keys.");
        }

        KeyboardInputService.SendToForeground(keys is VirtualKey[] keyArray ? keyArray : keys.ToArray());

        if (settings.RestoreFocus && previousForeground != IntPtr.Zero && previousForeground != window.Handle) {
            MonitorNativeMethods.SetForegroundWindow(previousForeground);
        }
    }

    private static void ValidateWindow(WindowInfo window) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }
        if (window.Handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid window handle", nameof(window));
        }
    }

    private static void NormalizeOptions(WindowInputOptions options) {
        if (options.UseHostedSessionScanCodes) {
            options.ActivateWindow = false;
        }

        if (options.ClipboardRetryCount < 1) {
            options.ClipboardRetryCount = 1;
        }
        if (options.ClipboardRetryDelayMilliseconds < 0) {
            options.ClipboardRetryDelayMilliseconds = 0;
        }
        if (options.ActivationRetryCount < 1) {
            options.ActivationRetryCount = 1;
        }
        if (options.ActivationRetryDelayMilliseconds < 0) {
            options.ActivationRetryDelayMilliseconds = 0;
        }
        if (options.InputRetryCount < 1) {
            options.InputRetryCount = 1;
        }
        if (options.KeyDelayMilliseconds < 0) {
            options.KeyDelayMilliseconds = 0;
        }
        if (options.ScriptChunkLength < 1) {
            options.ScriptChunkLength = 1;
        }
        if (options.ScriptLineDelayMilliseconds < 0) {
            options.ScriptLineDelayMilliseconds = 0;
        }
    }

    internal static WindowTextDeliveryMode ResolveTextDeliveryMode(WindowInputOptions options, bool targetOwnsForeground) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        if ((options.RequireForegroundWindowForTyping || options.UsePhysicalKeyboardLayout || options.UseHostedSessionScanCodes) && !options.UseSendInput) {
            throw new InvalidOperationException("Foreground-only window typing requires SendInput.");
        }

        if (options.UseHostedSessionScanCodes) {
            if (!targetOwnsForeground) {
                throw new InvalidOperationException(BuildForegroundOwnershipMessage(
                    "Window must own the foreground before typing with hosted-session scan code input.",
                    IntPtr.Zero));
            }

            return WindowTextDeliveryMode.ForegroundInput;
        }

        if (options.UsePhysicalKeyboardLayout) {
            if (!targetOwnsForeground) {
                throw new InvalidOperationException(BuildForegroundOwnershipMessage(
                    "Window must own the foreground before typing with physical key input.",
                    IntPtr.Zero));
            }

            return WindowTextDeliveryMode.ForegroundInput;
        }

        if (options.UseSendInput && targetOwnsForeground) {
            return WindowTextDeliveryMode.ForegroundInput;
        }

        if (options.RequireForegroundWindowForTyping) {
            throw new InvalidOperationException(BuildForegroundOwnershipMessage(
                "Window must own the foreground before typing with foreground input.",
                IntPtr.Zero));
        }

        return WindowTextDeliveryMode.WindowMessage;
    }

    private static uint ResolveWindowThreadId(WindowInfo window) {
        if (window.ThreadId != 0) {
            return window.ThreadId;
        }

        return MonitorNativeMethods.GetWindowThreadProcessId(window.Handle, out _);
    }

    internal static IReadOnlyList<WindowScriptChunk> CreateScriptChunks(string text, int chunkLength) {
        if (text == null) {
            throw new ArgumentNullException(nameof(text));
        }
        if (chunkLength < 1) {
            throw new ArgumentOutOfRangeException(nameof(chunkLength));
        }

        var chunks = new List<WindowScriptChunk>();
        string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        if (normalized.Length == 0) {
            return chunks;
        }

        int index = 0;
        while (index < normalized.Length) {
            int newlineIndex = normalized.IndexOf('\n', index);
            bool hasLineBreak = newlineIndex >= 0;
            int lineEnd = hasLineBreak ? newlineIndex : normalized.Length;
            string line = normalized.Substring(index, lineEnd - index);
            AppendLineChunks(chunks, line, chunkLength, hasLineBreak);
            index = hasLineBreak ? lineEnd + 1 : normalized.Length;
        }

        return chunks;
    }

    private static void AppendLineChunks(List<WindowScriptChunk> chunks, string line, int chunkLength, bool hasLineBreak) {
        if (line.Length == 0) {
            if (hasLineBreak) {
                chunks.Add(new WindowScriptChunk(string.Empty, sendLineBreak: true));
            }

            return;
        }

        for (int offset = 0; offset < line.Length; offset += chunkLength) {
            int length = Math.Min(chunkLength, line.Length - offset);
            bool sendLineBreak = hasLineBreak && offset + length >= line.Length;
            chunks.Add(new WindowScriptChunk(line.Substring(offset, length), sendLineBreak));
        }
    }

    private static void SendScriptText(WindowInfo window, string text, WindowInputOptions options, WindowTextDeliveryMode deliveryMode) {
        IReadOnlyList<WindowScriptChunk> chunks = CreateScriptChunks(text, options.ScriptChunkLength);
        if (chunks.Count == 0) {
            return;
        }

        if (deliveryMode == WindowTextDeliveryMode.ForegroundInput) {
            foreach (WindowScriptChunk chunk in chunks) {
                EnsureForegroundOwnership(window, options);
                if (!string.IsNullOrEmpty(chunk.Text)) {
                    SendForegroundText(window, chunk.Text, options);
                }

                if (chunk.SendLineBreak) {
                    EnsureForegroundOwnership(window, options);
                    KeyboardInputService.SendToForeground(VirtualKey.VK_RETURN);
                    if (options.ScriptLineDelayMilliseconds > 0) {
                        Thread.Sleep(options.ScriptLineDelayMilliseconds);
                    }
                }
            }

            return;
        }

        IntPtr targetHandle = ResolvePreferredTextHandle(window.Handle);
        foreach (WindowScriptChunk chunk in chunks) {
            if (!string.IsNullOrEmpty(chunk.Text)) {
                SendMessageText(targetHandle, chunk.Text, options.KeyDelayMilliseconds);
            }

            if (chunk.SendLineBreak) {
                SendMessageText(targetHandle, Environment.NewLine, options.KeyDelayMilliseconds);
                if (options.ScriptLineDelayMilliseconds > 0) {
                    Thread.Sleep(options.ScriptLineDelayMilliseconds);
                }
            }
        }
    }

    private static void SendForegroundText(WindowInfo window, string text, WindowInputOptions options) {
        if (string.IsNullOrEmpty(text)) {
            return;
        }

        uint windowThreadId = 0;
        if (options.UsePhysicalKeyboardLayout) {
            windowThreadId = ResolveWindowThreadId(window);
        }

        foreach (char character in text) {
            EnsureForegroundOwnership(window, options);

            if (options.UseHostedSessionScanCodes) {
                KeyboardInputService.SendCharacterToForegroundUsingUsScanCodes(character, options.KeyDelayMilliseconds);
                continue;
            }

            if (options.UsePhysicalKeyboardLayout) {
                KeyboardInputService.SendCharacterToForegroundUsingKeyboardLayout(character, windowThreadId, options.KeyDelayMilliseconds);
                continue;
            }

            SendInputCharacter(character, options);
        }
    }

    private static void EnsureForegroundOwnership(WindowInfo window, WindowInputOptions options) {
        if (!options.RequireForegroundWindowForTyping && !options.UsePhysicalKeyboardLayout && !options.UseHostedSessionScanCodes) {
            return;
        }

        if (MonitorNativeMethods.GetForegroundWindow() != window.Handle) {
            throw new InvalidOperationException(BuildForegroundOwnershipMessage(
                "Foreground ownership changed while typing. Hosted-session and foreground typing stop immediately when focus drifts.",
                window.Handle));
        }
    }

    private static void TryActivateWindow(IntPtr handle, int retryCount, int retryDelayMilliseconds) {
        WindowActivationService.TryActivateWindow(handle, retryCount, retryDelayMilliseconds);
    }

    private static void SendPaste(IntPtr handle, int retryCount, int retryDelayMilliseconds) {
        for (int attempt = 0; attempt < retryCount; attempt++) {
            MonitorNativeMethods.SendMessage(handle, MonitorNativeMethods.WM_PASTE, 0, 0);
            if (attempt < retryCount - 1 && retryDelayMilliseconds > 0) {
                Thread.Sleep(retryDelayMilliseconds);
            }
        }
    }

    private static void SendMessageText(IntPtr handle, string text, int delayMilliseconds) {
        foreach (char c in text) {
            MonitorNativeMethods.SendMessage(handle, MonitorNativeMethods.WM_CHAR, (uint)c, 0);
            if (delayMilliseconds > 0) {
                Thread.Sleep(delayMilliseconds);
            }
        }
    }

    private static void SendInputCharacter(char character, WindowInputOptions options) {
        MonitorNativeMethods.INPUT[] inputs = new MonitorNativeMethods.INPUT[2];

        inputs[0].Type = MonitorNativeMethods.INPUT_KEYBOARD;
        inputs[0].Data.Keyboard = new MonitorNativeMethods.KEYBDINPUT {
            Vk = 0,
            Scan = character,
            Flags = MonitorNativeMethods.KEYEVENTF_UNICODE,
            Time = 0,
            ExtraInfo = IntPtr.Zero
        };

        inputs[1].Type = MonitorNativeMethods.INPUT_KEYBOARD;
        inputs[1].Data.Keyboard = new MonitorNativeMethods.KEYBDINPUT {
            Vk = 0,
            Scan = character,
            Flags = MonitorNativeMethods.KEYEVENTF_UNICODE | MonitorNativeMethods.KEYEVENTF_KEYUP,
            Time = 0,
            ExtraInfo = IntPtr.Zero
        };

        uint sent = 0;
        for (int attempt = 0; attempt < options.InputRetryCount; attempt++) {
            sent = MonitorNativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<MonitorNativeMethods.INPUT>());
            if (sent == inputs.Length) {
                break;
            }
            if (attempt < options.InputRetryCount - 1 && options.ActivationRetryDelayMilliseconds > 0) {
                Thread.Sleep(options.ActivationRetryDelayMilliseconds);
            }
        }

        try {
            KeyboardInputService.EnsureInputDelivery((uint)inputs.Length, sent, "Unicode text input");
        } catch (KeyboardInputDeliveryException) {
            KeyboardInputService.TryReleaseUnicodeCharacter(character);
            throw;
        }

        if (options.KeyDelayMilliseconds > 0) {
            Thread.Sleep(options.KeyDelayMilliseconds);
        }
    }

    private static string BuildForegroundOwnershipMessage(string message, IntPtr expectedForegroundHandle) {
        IntPtr currentForegroundHandle = MonitorNativeMethods.GetForegroundWindow();
        if (expectedForegroundHandle != IntPtr.Zero) {
            return message +
                " Expected: " + DescribeWindowHandle(expectedForegroundHandle) +
                ". Current: " + DescribeWindowHandle(currentForegroundHandle) + ".";
        }

        return message + " Current: " + DescribeWindowHandle(currentForegroundHandle) + ".";
    }

    private static string DescribeWindowHandle(IntPtr handle) {
        if (handle == IntPtr.Zero) {
            return "no foreground window";
        }

        string title;
        try {
            title = WindowTextHelper.GetWindowText(handle);
        } catch {
            title = string.Empty;
        }

        var classNameBuilder = new StringBuilder(256);
        string className = MonitorNativeMethods.GetClassName(handle, classNameBuilder, classNameBuilder.Capacity) > 0
            ? classNameBuilder.ToString()
            : string.Empty;

        if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(className)) {
            return "'" + title + "' [" + "0x" + handle.ToInt64().ToString("X") + "] class=" + className;
        }

        if (!string.IsNullOrWhiteSpace(title)) {
            return "'" + title + "' [" + "0x" + handle.ToInt64().ToString("X") + "]";
        }

        if (!string.IsNullOrWhiteSpace(className)) {
            return "[" + "0x" + handle.ToInt64().ToString("X") + "] class=" + className;
        }

        return "[" + "0x" + handle.ToInt64().ToString("X") + "]";
    }

    private static void EnsureTextApplied(WindowInfo window, string text) {
        WindowControlInfo? editable = FindPreferredEditableControl(window.Handle);
        if (editable == null) {
            return;
        }

        if (!WindowControlService.TryGetControlText(
                editable,
                DesktopTextObservationOptions.MaximumTextLength,
                NativeTextVerificationTimeoutMilliseconds,
                out string current,
                out bool isTruncated)) {
            throw new NativeTextMutationOutcomeUnknownException(
                "WM_GETTEXT",
                NativeTextVerificationTimeoutMilliseconds);
        }
        if (isTruncated) {
            throw new InvalidOperationException("The editable control text was too large to verify before applying the fallback text.");
        }

        if (string.Equals(current, text, StringComparison.Ordinal)) {
            return;
        }

        WindowControlService.SetText(editable, text);
    }

    private static WindowControlInfo? FindPreferredEditableControl(IntPtr windowHandle) {
        var enumerator = new ControlEnumerator();
        List<WindowControlInfo> controls = enumerator.EnumerateControls(windowHandle);

        return controls.Find(control => control.IsPassword == false && control.ClassName.Equals("RichEditD2DPT", StringComparison.OrdinalIgnoreCase))
            ?? controls.Find(control => control.IsPassword == false && control.ClassName.Equals("NotepadTextBox", StringComparison.OrdinalIgnoreCase))
            ?? controls.Find(control => control.IsPassword == false && control.ClassName.IndexOf("RichEdit", StringComparison.OrdinalIgnoreCase) >= 0)
            ?? controls.Find(control => control.IsPassword == false && control.ClassName.IndexOf("Edit", StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static IntPtr ResolvePreferredTextHandle(IntPtr windowHandle) {
        WindowControlInfo? editable = FindPreferredEditableControl(windowHandle);
        return editable?.Handle ?? windowHandle;
    }
}

