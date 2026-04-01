using System;
using System.Threading;

namespace DesktopManager;

internal static class WindowActivationService {
    public static IntPtr GetFocusedControlHandle(IntPtr windowHandle) {
        if (windowHandle == IntPtr.Zero) {
            return IntPtr.Zero;
        }

        uint threadId = MonitorNativeMethods.GetWindowThreadProcessId(windowHandle, out _);
        if (threadId == 0) {
            return IntPtr.Zero;
        }

        MonitorNativeMethods.GUITHREADINFO threadInfo = new() {
            cbSize = System.Runtime.InteropServices.Marshal.SizeOf<MonitorNativeMethods.GUITHREADINFO>()
        };

        return MonitorNativeMethods.GetGUIThreadInfo(threadId, ref threadInfo)
            ? threadInfo.hwndFocus
            : IntPtr.Zero;
    }

    public static bool TryPrepareWindowForAutomation(IntPtr handle, int retryCount = 3, int retryDelayMilliseconds = 100) {
        const int SWP_NOMOVE = 0x0002;

        if (handle == IntPtr.Zero) {
            return false;
        }

        if (retryCount < 1) {
            retryCount = 1;
        }

        if (retryDelayMilliseconds < 0) {
            retryDelayMilliseconds = 0;
        }

        for (int attempt = 0; attempt < retryCount; attempt++) {
            if (MonitorNativeMethods.IsIconic(handle)) {
                MonitorNativeMethods.ShowWindow(handle, MonitorNativeMethods.SW_RESTORE);
            } else if (!MonitorNativeMethods.IsWindowVisible(handle)) {
                MonitorNativeMethods.ShowWindow(handle, MonitorNativeMethods.SW_SHOW);
            }

            MonitorNativeMethods.BringWindowToTop(handle);
            MonitorNativeMethods.SetWindowPos(handle, MonitorNativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | MonitorNativeMethods.SWP_NOSIZE | MonitorNativeMethods.SWP_NOACTIVATE);
            MonitorNativeMethods.SetWindowPos(handle, MonitorNativeMethods.HWND_NOTOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | MonitorNativeMethods.SWP_NOSIZE | MonitorNativeMethods.SWP_NOACTIVATE);

            if (MonitorNativeMethods.GetForegroundWindow() == handle) {
                return true;
            }

            if (attempt < retryCount - 1 && retryDelayMilliseconds > 0) {
                Thread.Sleep(retryDelayMilliseconds);
            }
        }

        return MonitorNativeMethods.GetForegroundWindow() == handle;
    }

    public static bool TryActivateWindow(IntPtr handle, int retryCount = 5, int retryDelayMilliseconds = 100) {
        if (handle == IntPtr.Zero) {
            return false;
        }

        if (retryCount < 1) {
            retryCount = 1;
        }

        if (retryDelayMilliseconds < 0) {
            retryDelayMilliseconds = 0;
        }

        for (int attempt = 0; attempt < retryCount; attempt++) {
            TryPrepareWindowForAutomation(handle, retryCount: 1, retryDelayMilliseconds: 0);
            TryForceForegroundActivation(handle);

            if (MonitorNativeMethods.GetForegroundWindow() == handle) {
                return true;
            }

            if (attempt < retryCount - 1 && retryDelayMilliseconds > 0) {
                Thread.Sleep(retryDelayMilliseconds);
            }
        }

        return MonitorNativeMethods.GetForegroundWindow() == handle;
    }

    public static bool TryFocusControl(IntPtr windowHandle, IntPtr controlHandle, bool ensureForegroundWindow = false, int retryCount = 3, int retryDelayMilliseconds = 100) {
        if (windowHandle == IntPtr.Zero || controlHandle == IntPtr.Zero) {
            return false;
        }

        if (retryCount < 1) {
            retryCount = 1;
        }

        if (retryDelayMilliseconds < 0) {
            retryDelayMilliseconds = 0;
        }

        for (int attempt = 0; attempt < retryCount; attempt++) {
            if (ensureForegroundWindow) {
                if (!TryActivateWindow(windowHandle, retryCount: 1, retryDelayMilliseconds: 0)) {
                    if (attempt < retryCount - 1 && retryDelayMilliseconds > 0) {
                        Thread.Sleep(retryDelayMilliseconds);
                    }
                    continue;
                }
            } else {
                TryPrepareWindowForAutomation(windowHandle, retryCount: 1, retryDelayMilliseconds: 0);
            }

            TryForceControlFocus(windowHandle, controlHandle);
            if (GetFocusedControlHandle(windowHandle) == controlHandle) {
                return true;
            }

            if (attempt < retryCount - 1 && retryDelayMilliseconds > 0) {
                Thread.Sleep(retryDelayMilliseconds);
            }
        }

        return GetFocusedControlHandle(windowHandle) == controlHandle;
    }

    private static void TryForceForegroundActivation(IntPtr handle) {
        if (handle == IntPtr.Zero) {
            return;
        }

        IntPtr foregroundHandle = MonitorNativeMethods.GetForegroundWindow();
        uint currentThreadId = MonitorNativeMethods.GetCurrentThreadId();
        uint targetThreadId = MonitorNativeMethods.GetWindowThreadProcessId(handle, out _);
        uint foregroundThreadId = foregroundHandle == IntPtr.Zero ? 0 : MonitorNativeMethods.GetWindowThreadProcessId(foregroundHandle, out _);
        bool attachedToTarget = false;
        bool attachedToForeground = false;

        try {
            if (targetThreadId != 0 && targetThreadId != currentThreadId) {
                attachedToTarget = MonitorNativeMethods.AttachThreadInput(currentThreadId, targetThreadId, true);
            }

            if (foregroundThreadId != 0 && foregroundThreadId != currentThreadId && foregroundThreadId != targetThreadId) {
                attachedToForeground = MonitorNativeMethods.AttachThreadInput(currentThreadId, foregroundThreadId, true);
            }

            MonitorNativeMethods.BringWindowToTop(handle);
            MonitorNativeMethods.SetActiveWindow(handle);
            MonitorNativeMethods.SetForegroundWindow(handle);
            MonitorNativeMethods.SetFocus(handle);
        } finally {
            if (attachedToForeground) {
                MonitorNativeMethods.AttachThreadInput(currentThreadId, foregroundThreadId, false);
            }

            if (attachedToTarget) {
                MonitorNativeMethods.AttachThreadInput(currentThreadId, targetThreadId, false);
            }
        }
    }

    private static void TryForceControlFocus(IntPtr windowHandle, IntPtr controlHandle) {
        if (windowHandle == IntPtr.Zero || controlHandle == IntPtr.Zero) {
            return;
        }

        IntPtr foregroundHandle = MonitorNativeMethods.GetForegroundWindow();
        uint currentThreadId = MonitorNativeMethods.GetCurrentThreadId();
        uint targetThreadId = MonitorNativeMethods.GetWindowThreadProcessId(windowHandle, out _);
        uint controlThreadId = MonitorNativeMethods.GetWindowThreadProcessId(controlHandle, out _);
        uint foregroundThreadId = foregroundHandle == IntPtr.Zero ? 0 : MonitorNativeMethods.GetWindowThreadProcessId(foregroundHandle, out _);
        bool attachedToWindow = false;
        bool attachedToControl = false;
        bool attachedToForeground = false;

        try {
            if (targetThreadId != 0 && targetThreadId != currentThreadId) {
                attachedToWindow = MonitorNativeMethods.AttachThreadInput(currentThreadId, targetThreadId, true);
            }

            if (controlThreadId != 0 && controlThreadId != currentThreadId && controlThreadId != targetThreadId) {
                attachedToControl = MonitorNativeMethods.AttachThreadInput(currentThreadId, controlThreadId, true);
            }

            if (foregroundThreadId != 0 &&
                foregroundThreadId != currentThreadId &&
                foregroundThreadId != targetThreadId &&
                foregroundThreadId != controlThreadId) {
                attachedToForeground = MonitorNativeMethods.AttachThreadInput(currentThreadId, foregroundThreadId, true);
            }

            MonitorNativeMethods.BringWindowToTop(windowHandle);
            MonitorNativeMethods.SetActiveWindow(windowHandle);
            MonitorNativeMethods.SetForegroundWindow(windowHandle);
            MonitorNativeMethods.SetFocus(controlHandle);
        } finally {
            if (attachedToForeground) {
                MonitorNativeMethods.AttachThreadInput(currentThreadId, foregroundThreadId, false);
            }

            if (attachedToControl) {
                MonitorNativeMethods.AttachThreadInput(currentThreadId, controlThreadId, false);
            }

            if (attachedToWindow) {
                MonitorNativeMethods.AttachThreadInput(currentThreadId, targetThreadId, false);
            }
        }
    }
}
