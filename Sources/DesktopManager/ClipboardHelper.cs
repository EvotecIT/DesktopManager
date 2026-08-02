using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;
using System.Threading;

namespace DesktopManager;

/// <summary>
/// Helper methods for working with the Windows clipboard.
/// </summary>
[SupportedOSPlatform("windows")]
public static class ClipboardHelper {
    internal static ClipboardSnapshot CaptureSnapshot() {
        int initializeResult = OleInitialize(IntPtr.Zero);
        if (initializeResult < 0) {
            Marshal.ThrowExceptionForHR(initializeResult);
        }

        int result = 0;
        IDataObject? dataObject = null;
        for (int attempt = 0; attempt < 5; attempt++) {
            result = OleGetClipboard(out dataObject);
            if (result >= 0) {
                return new ClipboardSnapshot(dataObject, oleInitialized: true);
            }

            if (attempt < 4) {
                Thread.Sleep(50);
            }
        }

        try {
            try {
                OpenClipboardWithRetry(5, 50);
                if (CountClipboardFormats() == 0) {
                    return new ClipboardSnapshot(dataObject: null, oleInitialized: true);
                }
            } finally {
                MonitorNativeMethods.CloseClipboard();
            }
        } catch {
            OleUninitialize();
            throw;
        }

        OleUninitialize();
        Marshal.ThrowExceptionForHR(result);
        throw new InvalidOperationException("Unable to capture clipboard data.");
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

        OpenClipboardWithRetry(retryCount, retryDelayMilliseconds);
        try {
            if (!MonitorNativeMethods.EmptyClipboard()) {
                throw new InvalidOperationException("Unable to empty clipboard.");
            }

            int bytes = (text.Length + 1) * 2;
            IntPtr hGlobal = MonitorNativeMethods.GlobalAlloc(MonitorNativeMethods.GMEM_MOVEABLE, (UIntPtr)bytes);
            if (hGlobal == IntPtr.Zero) {
                throw new InvalidOperationException("GlobalAlloc failed.");
            }

            IntPtr target = MonitorNativeMethods.GlobalLock(hGlobal);
            if (target == IntPtr.Zero) {
                MonitorNativeMethods.GlobalFree(hGlobal);
                throw new InvalidOperationException("GlobalLock failed.");
            }

            try {
                Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
                Marshal.WriteInt16(target, text.Length * 2, 0);
            } finally {
                MonitorNativeMethods.GlobalUnlock(hGlobal);
            }

            if (MonitorNativeMethods.SetClipboardData(MonitorNativeMethods.CF_UNICODETEXT, hGlobal) == IntPtr.Zero) {
                MonitorNativeMethods.GlobalFree(hGlobal);
                throw new InvalidOperationException("SetClipboardData failed.");
            }
        } finally {
            MonitorNativeMethods.CloseClipboard();
        }
    }

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

    private static void OpenClipboardWithRetry(int maxAttempts, int delayMilliseconds) {
        if (maxAttempts < 1) {
            maxAttempts = 1;
        }

        if (delayMilliseconds < 0) {
            delayMilliseconds = 0;
        }

        for (int attempt = 0; attempt < maxAttempts; attempt++) {
            if (MonitorNativeMethods.OpenClipboard(IntPtr.Zero)) {
                return;
            }
            if (attempt < maxAttempts - 1) {
                Thread.Sleep(delayMilliseconds);
            }
        }

        throw new InvalidOperationException("Unable to open clipboard.");
    }

    [DllImport("ole32.dll")]
    private static extern int OleGetClipboard([MarshalAs(UnmanagedType.Interface)] out IDataObject? dataObject);

    [DllImport("user32.dll")]
    private static extern int CountClipboardFormats();

    [DllImport("ole32.dll")]
    private static extern int OleInitialize(IntPtr reserved);

    [DllImport("ole32.dll")]
    private static extern int OleSetClipboard([MarshalAs(UnmanagedType.Interface)] IDataObject? dataObject);

    [DllImport("ole32.dll")]
    private static extern void OleUninitialize();

    internal sealed class ClipboardSnapshot : IDisposable {
        private IDataObject? _dataObject;
        private bool _oleInitialized;
        private bool _restored;

        internal ClipboardSnapshot(IDataObject? dataObject, bool oleInitialized) {
            _dataObject = dataObject;
            _oleInitialized = oleInitialized;
        }

        internal void Restore() {
            if (_restored) {
                return;
            }

            int result = 0;
            for (int attempt = 0; attempt < 5; attempt++) {
                result = OleSetClipboard(_dataObject);
                if (result >= 0) {
                    _restored = true;
                    return;
                }

                if (attempt < 4) {
                    Thread.Sleep(50);
                }
            }

            Marshal.ThrowExceptionForHR(result);
        }

        public void Dispose() {
            IDataObject? dataObject = _dataObject;
            _dataObject = null;
            if (dataObject != null && Marshal.IsComObject(dataObject)) {
                Marshal.FinalReleaseComObject(dataObject);
            }

            if (_oleInitialized) {
                _oleInitialized = false;
                OleUninitialize();
            }
        }
    }
}
