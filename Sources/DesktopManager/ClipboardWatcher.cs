using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace DesktopManager;

/// <summary>
/// Reports new text or image content through a clipboard format listener on a dedicated message thread.
/// </summary>
/// <remarks>
/// <para>Copies that apps mark as private are skipped, so password managers and similar apps are not recorded:
/// the <c>ExcludeClipboardContentFromMonitorProcessing</c> and <c>Clipboard Viewer Ignore</c> formats, and
/// <c>CanIncludeInClipboardHistory</c> set to zero.</para>
/// <para>Text is preferred over an image when a copy holds both. <see cref="SetText"/> and
/// <see cref="SetDeviceIndependentBitmap"/> put an earlier entry back without it being reported again.
/// To paste it into the app in front afterwards, send Ctrl+V with
/// <see cref="KeyboardInputService.SendToForeground(VirtualKey[])"/>.</para>
/// <para><see cref="ClipboardChanged"/> is raised on the watcher's thread. Keep handlers short and hand longer
/// work to another thread, or later copies queue behind it. Queued copies can coalesce into the latest clipboard
/// state. If the clipboard stays locked through the immediate attempts, that state is retried on this thread.</para>
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class ClipboardWatcher : IDisposable {
    private const uint WM_PUT = 0x8000 + 0x0151; // WM_APP range, private to this window.
    private const int MinimumDibBytes = 40; // sizeof(BITMAPINFOHEADER)
    private const int OpenAttempts = 5;
    private const int OpenRetryDelayMilliseconds = 20;

    private readonly Thread _thread;
    private readonly ThreadStartup _ready = new();
    private readonly ConcurrentDictionary<int, PutRequest> _puts = new();
    private readonly uint _excludeFromMonitoringFormat;
    private readonly uint _viewerIgnoreFormat;
    private readonly uint _historyFormat;
    private MonitorNativeMethods.WndProc? _wndProc;
    private Exception? _startError;
    private IntPtr _window;
    private uint _threadId;
    private int _nextPut;
    private volatile bool _disposed;
    // The clipboard state already handled; only the watcher thread touches it.
    private uint _handledSequence;
    private bool _retryRead;

    /// <summary>
    /// Starts listening for clipboard changes.
    /// </summary>
    /// <exception cref="Win32Exception">Windows refused the listener window or the clipboard listener.</exception>
    /// <exception cref="TimeoutException">The listener thread did not report readiness within five seconds.
    /// Stop is requested, with up to two more seconds for the thread to exit.</exception>
    public ClipboardWatcher() {
        _excludeFromMonitoringFormat = NativeClipboardMethods.RegisterClipboardFormat("ExcludeClipboardContentFromMonitorProcessing");
        _viewerIgnoreFormat = NativeClipboardMethods.RegisterClipboardFormat("Clipboard Viewer Ignore");
        _historyFormat = NativeClipboardMethods.RegisterClipboardFormat("CanIncludeInClipboardHistory");
        _thread = new Thread(Run) { IsBackground = true, Name = "DesktopManager clipboard" };
        _thread.SetApartmentState(ApartmentState.STA);
        try {
            _ready.Start(_thread, "clipboard listener");
        } catch {
            _disposed = true;
            MonitorNativeMethods.PostThreadMessage(_threadId, MonitorNativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
            if (_thread.IsAlive) {
                _thread.Join(TimeSpan.FromSeconds(2));
            }
            throw;
        }
    }

    /// <summary>Occurs on the watcher's thread for new text or image content that may be recorded.</summary>
    public event EventHandler<ClipboardChangedEventArgs>? ClipboardChanged;

    /// <summary>
    /// Replaces the clipboard with Unicode text without <see cref="ClipboardChanged"/> reporting it.
    /// </summary>
    /// <param name="text">The text to put on the clipboard.</param>
    /// <exception cref="ObjectDisposedException">The watcher has stopped.</exception>
    /// <exception cref="InvalidOperationException">The clipboard stayed busy or Windows refused the data.</exception>
    public void SetText(string text) {
        if (text == null) {
            throw new ArgumentNullException(nameof(text));
        }

        Put(MonitorNativeMethods.CF_UNICODETEXT, ClipboardHelper.ToUnicodeTextBytes(text));
    }

    /// <summary>
    /// Replaces the clipboard with an image in <c>CF_DIB</c> form without <see cref="ClipboardChanged"/> reporting it.
    /// </summary>
    /// <param name="deviceIndependentBitmap">The image bytes, as reported in <see cref="ClipboardChangedEventArgs.DeviceIndependentBitmap"/>.</param>
    /// <exception cref="ObjectDisposedException">The watcher has stopped.</exception>
    /// <exception cref="InvalidOperationException">The clipboard stayed busy or Windows refused the data.</exception>
    public void SetDeviceIndependentBitmap(byte[] deviceIndependentBitmap) {
        if (deviceIndependentBitmap == null) {
            throw new ArgumentNullException(nameof(deviceIndependentBitmap));
        }
        if (deviceIndependentBitmap.Length < MinimumDibBytes) {
            throw new ArgumentException("A device-independent bitmap starts with a 40-byte BITMAPINFOHEADER.", nameof(deviceIndependentBitmap));
        }

        Put(NativeClipboardMethods.CF_DIB, deviceIndependentBitmap);
    }

    /// <summary>
    /// Decides whether a copy is private from the markers apps set on it. A history flag that is present but
    /// unreadable counts as private.
    /// </summary>
    internal static bool IsPrivate(bool excludeFromMonitoring, bool viewerIgnore, bool historyFlagPresent, int? historyFlag) {
        return excludeFromMonitoring || viewerIgnore || (historyFlagPresent && historyFlag.GetValueOrDefault() == 0);
    }

    private void Put(uint format, byte[] data) {
        ThrowIfDisposed();
        var request = new PutRequest(format, data);
        int id = Interlocked.Increment(ref _nextPut);
        _puts[id] = request;
        try {
            // Writing on the watcher thread makes its window the clipboard owner, which is how its own
            // update is recognized and skipped. A call from a handler runs inline.
            NativeClipboardMethods.SendMessage(_window, WM_PUT, new IntPtr(id), IntPtr.Zero);
        } finally {
            _puts.TryRemove(id, out _);
        }

        if (!request.Completed) {
            throw new ObjectDisposedException(nameof(ClipboardWatcher));
        }
        if (request.Error != null) {
            ExceptionDispatchInfo.Capture(request.Error).Throw();
        }
    }

    private void Run() {
        bool listening = false;
        try {
            _threadId = MonitorNativeMethods.GetCurrentThreadId();
            _wndProc = WndProc;
            _window = MonitorNativeMethods.CreateWindowExW(0, "Message", string.Empty, 0, 0, 0, 0, 0,
                MonitorNativeMethods.HWND_MESSAGE, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            if (_window == IntPtr.Zero) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            MonitorNativeMethods.SetWindowLongPtr(_window, MonitorNativeMethods.GWLP_WNDPROC,
                Marshal.GetFunctionPointerForDelegate(_wndProc));
            if (!NativeClipboardMethods.AddClipboardFormatListener(_window)) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            listening = true;
        } catch (Exception exception) {
            _startError = exception;
        }

        try {
            _ready.Signal(_startError);
            if (_startError != null) {
                return;
            }

            while (!_disposed) {
                if (!MonitorNativeMethods.PeekMessage(out MonitorNativeMethods.MSG message, IntPtr.Zero, 0, 0, 1)) {
                    // A released clipboard emits no new notification. Retry on this thread after a bounded wait.
                    MonitorNativeMethods.MsgWaitForMultipleObjectsEx(0, IntPtr.Zero, 100, 0x04FF, 4);
                    if (_retryRead && !_disposed) {
                        OnClipboardUpdate();
                    }
                    continue;
                }
                if (message.message == MonitorNativeMethods.WM_QUIT) {
                    break;
                }
                MonitorNativeMethods.TranslateMessage(ref message);
                MonitorNativeMethods.DispatchMessage(ref message);
            }
        } finally {
            if (listening) {
                NativeClipboardMethods.RemoveClipboardFormatListener(_window);
            }
            if (_window != IntPtr.Zero) {
                MonitorNativeMethods.DestroyWindow(_window);
            }
        }
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam) {
        if (msg == NativeClipboardMethods.WM_CLIPBOARDUPDATE) {
            OnClipboardUpdate();
            return IntPtr.Zero;
        }
        if (msg == WM_PUT) {
            // Only requests this instance queued are honored; any other sender's WM_PUT is ignored.
            long id = wParam.ToInt64();
            if (id >= int.MinValue && id <= int.MaxValue && _puts.TryGetValue((int)id, out PutRequest? request)) {
                request.Run(_window);
                // The watcher's own put is never reported, however the update for it is queued.
                if (request.Error == null) {
                    _handledSequence = request.Sequence;
                }
            }
            return IntPtr.Zero;
        }
        return MonitorNativeMethods.DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    private void OnClipboardUpdate() {
        _retryRead = false;
        EventHandler<ClipboardChangedEventArgs>? handler = ClipboardChanged;
        // Several queued updates all read the newest content; report each clipboard state once.
        uint sequence = NativeClipboardMethods.GetClipboardSequenceNumber();
        if (sequence != 0 && sequence == _handledSequence) {
            return;
        }
        if (_disposed || handler == null || NativeClipboardMethods.GetClipboardOwner() == _window) {
            return;
        }

        ClipboardChangedEventArgs? copy;
        try {
            copy = ReadRecordableCopy();
        } catch (Exception ex) {
            DesktopManagerDiagnostics.Report($"Clipboard change could not be read: {ex.Message}");
            return;
        }
        if (copy == null) {
            return;
        }

        try {
            handler(this, copy);
        } catch (Exception ex) {
            // An exception must not unwind into the native window procedure.
            DesktopManagerDiagnostics.Report($"Clipboard handler failed: {ex.Message}");
        }
    }

    private ClipboardChangedEventArgs? ReadRecordableCopy() {
        _retryRead = true;
        ClipboardHelper.OpenClipboardWithRetry(OpenAttempts, OpenRetryDelayMilliseconds, _window);
        _retryRead = false;
        try {
            bool historyFlagPresent = IsAvailable(_historyFormat);
            int? historyFlag = null;
            if (historyFlagPresent) {
                byte[]? flag = ClipboardHelper.ReadOpenData(_historyFormat, 64);
                historyFlag = flag != null && flag.Length >= sizeof(int) ? BitConverter.ToInt32(flag, 0) : (int?)null;
            }
            if (IsPrivate(IsAvailable(_excludeFromMonitoringFormat), IsAvailable(_viewerIgnoreFormat), historyFlagPresent, historyFlag)) {
                return null;
            }

            string? text = ClipboardHelper.ReadOpenUnicodeText();
            if (!string.IsNullOrWhiteSpace(text)) {
                return new ClipboardChangedEventArgs(text, null, DateTimeOffset.Now);
            }

            byte[]? dib = ClipboardHelper.ReadOpenData(NativeClipboardMethods.CF_DIB, ClipboardHelper.MaximumReadBytes);
            return dib != null && dib.Length >= MinimumDibBytes
                ? new ClipboardChangedEventArgs(null, dib, DateTimeOffset.Now)
                : null;
        } finally {
            // Includes changes caused by delayed rendering during this read, while other writers are excluded.
            _handledSequence = NativeClipboardMethods.GetClipboardSequenceNumber();
            MonitorNativeMethods.CloseClipboard();
        }
    }

    private static bool IsAvailable(uint format) {
        return format != 0 && NativeClipboardMethods.IsClipboardFormatAvailable(format);
    }

    private void ThrowIfDisposed() {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(ClipboardWatcher));
        }
    }

    /// <summary>Stops listening and removes the clipboard listener.</summary>
    /// <exception cref="TimeoutException">The listener thread did not stop within two seconds.</exception>
    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        ClipboardChanged = null;
        MonitorNativeMethods.PostThreadMessage(_threadId, MonitorNativeMethods.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        if (Thread.CurrentThread == _thread) {
            // Disposed from a handler: the loop ends and removes the listener once the handler returns.
            return;
        }

        if (!_thread.Join(TimeSpan.FromSeconds(2))) {
            throw new TimeoutException("The clipboard listener thread did not stop.");
        }

    }

    private sealed class PutRequest {
        private readonly uint _format;
        private readonly byte[] _data;

        internal PutRequest(uint format, byte[] data) {
            _format = format;
            _data = data;
        }

        internal bool Completed { get; private set; }

        internal Exception? Error { get; private set; }

        internal uint Sequence { get; private set; }

        internal void Run(IntPtr owner) {
            try {
                Sequence = ClipboardHelper.SetData(_format, _data, OpenAttempts, OpenRetryDelayMilliseconds, owner);
            } catch (Exception ex) {
                Error = ex;
            } finally {
                Completed = true;
            }
        }
    }
}
