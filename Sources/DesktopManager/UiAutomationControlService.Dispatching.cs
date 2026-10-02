using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace DesktopManager;

internal sealed partial class UiAutomationControlService {
    private const uint InfiniteWait = 0xFFFFFFFF;
    private const uint MessageQueueInput = 0x04FF;
    private const uint MessageWaitInputAvailable = 0x0004;
    private const uint PeekMessageRemove = 0x0001;
    private const uint WaitObject0 = 0;
    private const uint WaitFailed = 0xFFFFFFFF;
    private const uint WindowMessageQuit = 0x0012;

    internal static void WaitWithCurrentUiMessagePump(int milliseconds) {
        if (milliseconds <= 0) {
            return;
        }

        if (ShouldPumpCurrentThread()) {
            PumpWait(Array.Empty<IntPtr>(), milliseconds);
        } else {
            Thread.Sleep(milliseconds);
        }
    }

    internal static bool WaitForSignalWithCurrentUiMessagePump(WaitHandle signal, int milliseconds) {
        if (signal == null) {
            throw new ArgumentNullException(nameof(signal));
        }

        if (milliseconds < Timeout.Infinite) { throw new ArgumentOutOfRangeException(nameof(milliseconds)); }
        if (!ShouldPumpCurrentThread()) { return signal.WaitOne(milliseconds); }
        bool referenceAdded = false;
        try {
            signal.SafeWaitHandle.DangerousAddRef(ref referenceAdded);
            return PumpWait(new[] { signal.SafeWaitHandle.DangerousGetHandle() }, milliseconds);
        } finally {
            if (referenceAdded) { signal.SafeWaitHandle.DangerousRelease(); }
        }
    }

    private static bool ShouldPumpCurrentThread() {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA || SynchronizationContext.Current != null);
    }

    private static bool PumpWait(IntPtr[] handles, int milliseconds) {
        Stopwatch elapsed = Stopwatch.StartNew();
        bool repostQuit = false;
        int quitCode = 0;
        try {
            while (true) {
                uint remaining = milliseconds == Timeout.Infinite ? InfiniteWait :
                    (uint)Math.Max(0, milliseconds - elapsed.ElapsedMilliseconds);
                uint result = MsgWaitForMultipleObjectsEx((uint)handles.Length, handles, remaining,
                    MessageQueueInput, MessageWaitInputAvailable);
                if (handles.Length > 0 && result == WaitObject0) { return true; }
                if (result == 0x102) { return false; }
                if (result == WaitFailed) { throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()); }
                for (int count = 0; count < 64 && PeekMessage(out MonitorNativeMethods.MSG message, IntPtr.Zero, 0, 0, PeekMessageRemove); count++) {
                    if (message.message == WindowMessageQuit) {
                        repostQuit = true;
                        quitCode = unchecked((int)message.wParam.ToInt64());
                    } else {
                        MonitorNativeMethods.TranslateMessage(ref message);
                        MonitorNativeMethods.DispatchMessage(ref message);
                    }
                }
                if (milliseconds != Timeout.Infinite && elapsed.ElapsedMilliseconds >= milliseconds) {
                    return handles.Length > 0 && MsgWaitForMultipleObjectsEx((uint)handles.Length, handles, 0,
                        MessageQueueInput, MessageWaitInputAvailable) == WaitObject0;
                }
            }
        } finally {
            if (repostQuit) { PostQuitMessage(quitCode); }
        }
    }

    /// <summary>
    /// Keeps an owning UI thread responsive while a wait completes. The native queue pump is
    /// deliberately framework-neutral so WPF, WinForms, and other HWND-backed providers share
    /// the same behavior on .NET Framework and modern .NET.
    /// </summary>
    private static bool TryRunWithCurrentUiMessagePump<T>(Func<T> operation, out T result) {
        result = default!;
        if (!ShouldPumpCurrentThread()) {
            return false;
        }

        T workerResult = default!;
        ExceptionDispatchInfo? workerException = null;
        using var completed = new EventWaitHandle(false, EventResetMode.ManualReset);
        ThreadPool.QueueUserWorkItem(_ => {
            try {
                workerResult = operation();
            } catch (Exception ex) {
                workerException = ExceptionDispatchInfo.Capture(ex);
            } finally {
                completed.Set();
            }
        });

        bool repostQuit = false;
        int quitCode = 0;
        IntPtr[] handles = { completed.SafeWaitHandle.DangerousGetHandle() };
        while (true) {
            uint waitResult = MsgWaitForMultipleObjectsEx(
                1,
                handles,
                InfiniteWait,
                MessageQueueInput,
                MessageWaitInputAvailable);
            if (waitResult == WaitObject0) {
                break;
            }

            if (waitResult == WaitFailed) {
                completed.WaitOne();
                break;
            }

            while (PeekMessage(out MonitorNativeMethods.MSG message, IntPtr.Zero, 0, 0, PeekMessageRemove)) {
                if (message.message == WindowMessageQuit) {
                    repostQuit = true;
                    quitCode = unchecked((int)message.wParam.ToInt64());
                    continue;
                }

                MonitorNativeMethods.TranslateMessage(ref message);
                MonitorNativeMethods.DispatchMessage(ref message);
            }
        }

        if (repostQuit) {
            PostQuitMessage(quitCode);
        }

        workerException?.Throw();
        result = workerResult;
        return true;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint MsgWaitForMultipleObjectsEx(
        uint count,
        IntPtr[] handles,
        uint milliseconds,
        uint wakeMask,
        uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PeekMessage(
        out MonitorNativeMethods.MSG message,
        IntPtr windowHandle,
        uint minimumMessage,
        uint maximumMessage,
        uint removeMessage);

    [DllImport("user32.dll")]
    private static extern void PostQuitMessage(int exitCode);
}
