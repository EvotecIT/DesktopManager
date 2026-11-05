using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace DesktopManager;

/// <summary>
/// Helper methods for working with the Windows clipboard.
/// </summary>
[SupportedOSPlatform("windows")]
public static class ClipboardHelper {
    internal static ClipboardSnapshot CaptureSnapshot() {
        var snapshot = new ClipboardSnapshot();
        OpenClipboardWithRetry(5, 50);
        try {
            uint format = 0;
            while (true) {
                NativeClipboardSnapshot.SetLastError(0);
                format = NativeClipboardSnapshot.EnumClipboardFormats(format);
                if (format == 0) {
                    if (Marshal.GetLastWin32Error() != 0) { throw new InvalidOperationException("Unable to enumerate clipboard formats."); }
                    return snapshot;
                }
                snapshot.Add(format, MonitorNativeMethods.GetClipboardData(format));
            }
        } catch {
            snapshot.Dispose();
            throw;
        } finally {
            MonitorNativeMethods.CloseClipboard();
        }
    }

    /// <summary>
    /// Places Unicode text on the clipboard.
    /// </summary>
    /// <param name="text">Text to place on the clipboard.</param>
    public static void SetText(string text) {
        SetText(text, 5, 50);
    }

    /// <summary>
    /// Places Unicode text on the clipboard with retry settings.
    /// </summary>
    /// <param name="text">Text to place on the clipboard.</param>
    /// <param name="retryCount">Number of attempts to open the clipboard.</param>
    /// <param name="retryDelayMilliseconds">Delay between retries in milliseconds.</param>
    public static void SetText(string text, int retryCount, int retryDelayMilliseconds) {
        if (text == null) {
            throw new ArgumentNullException(nameof(text));
        }

        SetData(MonitorNativeMethods.CF_UNICODETEXT, ToUnicodeTextBytes(text), retryCount, retryDelayMilliseconds, IntPtr.Zero);
    }

    /// <summary>Encodes text as null-terminated UTF-16 exactly as stored, without replacing unpaired surrogates.</summary>
    internal static byte[] ToUnicodeTextBytes(string text) {
        byte[] bytes = new byte[(text.Length + 1) * 2];
        Buffer.BlockCopy(text.ToCharArray(), 0, bytes, 0, text.Length * 2);
        return bytes;
    }

    /// <summary>Replaces the clipboard with one format holding a copy of <paramref name="data"/>.</summary>
    internal static void SetData(uint format, byte[] data, int retryCount, int retryDelayMilliseconds, IntPtr owner) {
        if (data == null) {
            throw new ArgumentNullException(nameof(data));
        }
        if (data.Length == 0) {
            throw new ArgumentException("Clipboard data cannot be empty.", nameof(data));
        }

        // Prepare the transfer before emptying the clipboard so a failed allocation leaves the current content intact.
        IntPtr hGlobal = MonitorNativeMethods.GlobalAlloc(MonitorNativeMethods.GMEM_MOVEABLE, (UIntPtr)data.Length);
        if (hGlobal == IntPtr.Zero) {
            throw new InvalidOperationException("GlobalAlloc failed.");
        }

        bool transferred = false;
        try {
            IntPtr target = MonitorNativeMethods.GlobalLock(hGlobal);
            if (target == IntPtr.Zero) {
                throw new InvalidOperationException("GlobalLock failed.");
            }

            try {
                Marshal.Copy(data, 0, target, data.Length);
            } finally {
                MonitorNativeMethods.GlobalUnlock(hGlobal);
            }

            OpenClipboardWithRetry(retryCount, retryDelayMilliseconds, owner);
            try {
                if (!MonitorNativeMethods.EmptyClipboard()) {
                    throw new InvalidOperationException("Unable to empty clipboard.");
                }

                if (MonitorNativeMethods.SetClipboardData(format, hGlobal) == IntPtr.Zero) {
                    throw new InvalidOperationException("SetClipboardData failed.");
                }
                transferred = true;
            } finally {
                MonitorNativeMethods.CloseClipboard();
            }
        } finally {
            if (!transferred) {
                MonitorNativeMethods.GlobalFree(hGlobal);
            }
        }
    }

    /// <summary>Reads Unicode text from the already open clipboard, bounded by the size of its memory block.</summary>
    internal static string? ReadOpenUnicodeText() {
        byte[]? bytes = ReadOpenData(MonitorNativeMethods.CF_UNICODETEXT, MaximumReadBytes);
        if (bytes == null) {
            return null;
        }

        string text = System.Text.Encoding.Unicode.GetString(bytes, 0, bytes.Length & ~1);
        int terminator = text.IndexOf('\0');
        return terminator >= 0 ? text.Substring(0, terminator) : text;
    }

    /// <summary>
    /// Copies an HGLOBAL clipboard format from the already open clipboard, or returns <c>null</c> when the format is
    /// absent, unreadable or larger than <paramref name="maximumBytes"/>.
    /// </summary>
    internal static byte[]? ReadOpenData(uint format, ulong maximumBytes) {
        if (!NativeClipboardMethods.IsClipboardFormatAvailable(format)) {
            return null;
        }

        IntPtr handle = MonitorNativeMethods.GetClipboardData(format);
        if (handle == IntPtr.Zero) {
            return null;
        }

        ulong size = NativeClipboardSnapshot.GlobalSize(handle).ToUInt64();
        if (size == 0 || size > maximumBytes || size > int.MaxValue) {
            return null;
        }

        IntPtr pointer = MonitorNativeMethods.GlobalLock(handle);
        if (pointer == IntPtr.Zero) {
            return null;
        }

        try {
            byte[] data = new byte[(int)size];
            Marshal.Copy(pointer, data, 0, data.Length);
            return data;
        } finally {
            MonitorNativeMethods.GlobalUnlock(handle);
        }
    }

    internal const ulong MaximumReadBytes = 64 * 1024 * 1024;

    /// <summary>
    /// Attempts to read Unicode text from the clipboard.
    /// </summary>
    /// <param name="text">The clipboard text.</param>
    /// <param name="retryCount">Number of attempts to open the clipboard.</param>
    /// <param name="retryDelayMilliseconds">Delay between retries in milliseconds.</param>
    /// <returns>True if Unicode text was read; otherwise false.</returns>
    public static bool TryGetText(out string text, int retryCount = 5, int retryDelayMilliseconds = 50) {
        text = string.Empty;
        OpenClipboardWithRetry(retryCount, retryDelayMilliseconds);
        try {
            IntPtr handle = MonitorNativeMethods.GetClipboardData(MonitorNativeMethods.CF_UNICODETEXT);
            if (handle == IntPtr.Zero) {
                return false;
            }

            IntPtr pointer = MonitorNativeMethods.GlobalLock(handle);
            if (pointer == IntPtr.Zero) {
                return false;
            }

            try {
                text = Marshal.PtrToStringUni(pointer) ?? string.Empty;
                return true;
            } finally {
                MonitorNativeMethods.GlobalUnlock(handle);
            }
        } finally {
            MonitorNativeMethods.CloseClipboard();
        }
    }

    internal static void OpenClipboardWithRetry(int maxAttempts, int delayMilliseconds, IntPtr owner = default) {
        if (maxAttempts < 1) {
            maxAttempts = 1;
        }

        if (delayMilliseconds < 0) {
            delayMilliseconds = 0;
        }

        for (int attempt = 0; attempt < maxAttempts; attempt++) {
            if (MonitorNativeMethods.OpenClipboard(owner)) {
                return;
            }
            if (attempt < maxAttempts - 1) {
                Thread.Sleep(delayMilliseconds);
            }
        }

        throw new InvalidOperationException("Unable to open clipboard.");
    }

    internal sealed class ClipboardSnapshot : IDisposable {
        private readonly List<KeyValuePair<uint, IntPtr>> _formats = new();
        private bool _restored;
        private bool _disposed;
        private ulong _bytes;

        internal void Add(uint format, IntPtr source) {
            if (_formats.Count >= 1024) { throw new InvalidOperationException("The clipboard contains too many formats to preserve safely."); }
            IntPtr copy = NativeClipboardSnapshot.Duplicate(format, source, ref _bytes);
            _formats.Add(new KeyValuePair<uint, IntPtr>(format, copy));
        }

        internal void Restore() {
            if (_disposed) { throw new ObjectDisposedException(nameof(ClipboardSnapshot)); }
            if (_restored) { return; }
            var pending = new List<KeyValuePair<uint, IntPtr>>();
            IntPtr owner = NativeClipboardSnapshot.CreateOwner();
            bool opened = false;
            try {
                ulong bytes = 0;
                // Prepare every transfer before clearing the current clipboard; retain originals for retries.
                foreach (var entry in _formats) {
                    pending.Add(new KeyValuePair<uint, IntPtr>(entry.Key, NativeClipboardSnapshot.Duplicate(entry.Key, entry.Value, ref bytes)));
                }
                OpenClipboardWithRetry(5, 50, owner);
                opened = true;
                if (!MonitorNativeMethods.EmptyClipboard()) { throw new InvalidOperationException("Unable to empty clipboard."); }
                for (int i = 0; i < pending.Count; i++) {
                    var entry = pending[i];
                    if (MonitorNativeMethods.SetClipboardData(entry.Key, entry.Value) == IntPtr.Zero) {
                        throw new InvalidOperationException("Unable to restore a clipboard format.");
                    }
                    pending[i] = new KeyValuePair<uint, IntPtr>(entry.Key, IntPtr.Zero);
                }
                _restored = true;
            } finally {
                if (opened) { MonitorNativeMethods.CloseClipboard(); }
                foreach (var entry in pending) { NativeClipboardSnapshot.Free(entry.Key, entry.Value); }
                NativeClipboardSnapshot.DestroyWindow(owner);
            }
        }

        public void Dispose() {
            if (_disposed) { return; }
            _disposed = true;
            foreach (var entry in _formats) { NativeClipboardSnapshot.Free(entry.Key, entry.Value); }
            _formats.Clear();
        }
    }
}
