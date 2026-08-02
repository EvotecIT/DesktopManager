using System;
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

        if (!TryRunWithCurrentUiMessagePump(() => {
            Thread.Sleep(milliseconds);
            return true;
        }, out _)) {
            Thread.Sleep(milliseconds);
        }
    }

    internal static bool WaitForSignalWithCurrentUiMessagePump(WaitHandle signal, int milliseconds) {
        if (signal == null) {
            throw new ArgumentNullException(nameof(signal));
        }

        return TryRunWithCurrentUiMessagePump(() => signal.WaitOne(milliseconds), out bool signaled)
            ? signaled
            : signal.WaitOne(milliseconds);
    }

    /// <summary>
    /// Keeps an owning UI thread responsive while a wait completes. The native queue pump is
    /// deliberately framework-neutral so WPF, WinForms, and other HWND-backed providers share
    /// the same behavior on .NET Framework and modern .NET.
    /// </summary>
    private static bool TryRunWithCurrentUiMessagePump<T>(Func<T> operation, out T result) {
        result = default!;
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            return false;
        }

        T workerResult = default!;
        ExceptionDispatchInfo? workerException = null;
        using var completed = new EventWaitHandle(false, EventResetMode.ManualReset);
        var worker = new Thread(() => {
            try {
                workerResult = operation();
            } catch (Exception ex) {
                workerException = ExceptionDispatchInfo.Capture(ex);
            } finally {
                completed.Set();
            }
        }) {
            IsBackground = true,
            Name = "DesktopManager UI message-pump bridge"
        };
        worker.Start();

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

        worker.Join();
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
