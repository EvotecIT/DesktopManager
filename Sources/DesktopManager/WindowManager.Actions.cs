using System;
using System.Linq;

namespace DesktopManager;

public partial class WindowManager
{
        /// <summary>
        /// Sets the position of a window.
        /// </summary>
        /// <param name="windowInfo">The window information.</param>
        /// <param name="left">The left position.</param>
        /// <param name="top">The top position.</param>
        public void SetWindowPosition(WindowInfo windowInfo, int left, int top) {
            SetWindowPosition(windowInfo, left, top, -1, -1);
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
            const int SWP_NOMOVE = 0x0002;

            int flags = MonitorNativeMethods.SWP_NOZORDER;

            // If position is -1, don't move
            if (left == -1 && top == -1) {
                flags |= SWP_NOMOVE;
            }

            // If size is -1, don't resize
            if (width < 0 && height < 0) {
                flags |= MonitorNativeMethods.SWP_NOSIZE;
            }

            if (!MonitorNativeMethods.SetWindowPos(
                windowInfo.Handle,
                IntPtr.Zero,
                left == -1 ? windowInfo.Left : left,
                top == -1 ? windowInfo.Top : top,
                width < 0 ? windowInfo.Width : width,
                height < 0 ? windowInfo.Height : height,
                flags)) {
                throw new InvalidOperationException("Failed to set window position");
            }
        }

        /// <summary>
        /// Snaps a window to a predefined region of its monitor.
        /// </summary>
        /// <param name="window">The window to snap.</param>
        /// <param name="position">The snap position.</param>
        public void SnapWindow(WindowInfo window, SnapPosition position) {
            if (window == null) {
                throw new ArgumentNullException(nameof(window));
            }

            var monitor = _monitors.GetMonitors(index: window.MonitorIndex).FirstOrDefault();
            if (monitor == null) {
                monitor = _monitors.GetMonitors(primaryOnly: true).FirstOrDefault();
                if (monitor == null) {
                    throw new InvalidOperationException("No monitor found for snapping");
                }
            }

            var bounds = monitor.GetMonitorBounds();
            int monitorWidth = bounds.Right - bounds.Left;
            int monitorHeight = bounds.Bottom - bounds.Top;

            int width = monitorWidth;
            int height = monitorHeight;
            int left = bounds.Left;
            int top = bounds.Top;

            switch (position) {
                case SnapPosition.Left:
                    width = monitorWidth / 2;
                    break;
                case SnapPosition.Right:
                    width = monitorWidth / 2;
                    left += monitorWidth / 2;
                    break;
                case SnapPosition.TopLeft:
                    width = monitorWidth / 2;
                    height = monitorHeight / 2;
                    break;
                case SnapPosition.TopRight:
                    width = monitorWidth / 2;
                    height = monitorHeight / 2;
                    left += monitorWidth / 2;
                    break;
                case SnapPosition.BottomLeft:
                    width = monitorWidth / 2;
                    height = monitorHeight / 2;
                    top += monitorHeight / 2;
                    break;
                case SnapPosition.BottomRight:
                    width = monitorWidth / 2;
                    height = monitorHeight / 2;
                    left += monitorWidth / 2;
                    top += monitorHeight / 2;
                    break;
            }

            SetWindowPosition(window, left, top, width, height);
        }

        /// <summary>
        /// Moves the specified window to the target monitor while preserving its relative position.
        /// </summary>
        /// <param name="windowInfo">The window to move.</param>
        /// <param name="targetMonitor">The monitor to move the window to.</param>
        /// <returns>
        /// True if the window was repositioned; false if the window was already on the target monitor
        /// at the same coordinates.
        /// </returns>
        public bool MoveWindowToMonitor(WindowInfo windowInfo, Monitor targetMonitor) {
            if (targetMonitor == null) {
                throw new ArgumentNullException(nameof(targetMonitor));
            }

            RECT windowRect = new RECT();
            if (!MonitorNativeMethods.GetWindowRect(windowInfo.Handle, out windowRect)) {
                throw new InvalidOperationException("Failed to get window position");
            }

            var targetBounds = targetMonitor.GetMonitorBounds();

            var currentMonitor = _monitors.GetMonitors(index: windowInfo.MonitorIndex).FirstOrDefault();
            RECT currentBounds;
            if (currentMonitor != null) {
                currentBounds = currentMonitor.GetMonitorBounds();
            } else {
                currentBounds = new RECT { Left = windowRect.Left, Top = windowRect.Top };
            }

            int offsetX = windowRect.Left - currentBounds.Left;
            int offsetY = windowRect.Top - currentBounds.Top;

            int newLeft = targetBounds.Left + offsetX;
            int newTop = targetBounds.Top + offsetY;

            if (currentBounds.Left == targetBounds.Left &&
                currentBounds.Top == targetBounds.Top &&
                currentBounds.Right == targetBounds.Right &&
                currentBounds.Bottom == targetBounds.Bottom) {
                return false;
            }

            SetWindowPosition(windowInfo, newLeft, newTop);
            return true;
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

        /// <summary>
        /// Sets whether a window is topmost.
        /// </summary>
        /// <param name="windowInfo">The window information.</param>
        /// <param name="topMost">True to make the window topmost; false to reset.</param>
        public void SetWindowTopMost(WindowInfo windowInfo, bool topMost) {
            const int SWP_NOMOVE = 0x0002;
            const int SWP_NOSIZE = 0x0001;

            var insertAfter = topMost ? MonitorNativeMethods.HWND_TOPMOST : MonitorNativeMethods.HWND_NOTOPMOST;
            if (!MonitorNativeMethods.SetWindowPos(windowInfo.Handle, insertAfter, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE)) {
                throw new InvalidOperationException("Failed to set window topmost state");
            }
        }

        /// <summary>
        /// Activates a window.
        /// </summary>
        /// <param name="windowInfo">The window information.</param>
        public void ActivateWindow(WindowInfo windowInfo) {
            if (!MonitorNativeMethods.SetForegroundWindow(windowInfo.Handle)) {
                throw new InvalidOperationException("Failed to activate window");
            }
        }

        /// <summary>
        /// Sets the transparency level of a window.
        /// </summary>
        /// <param name="windowInfo">The window information.</param>
        /// <param name="alpha">Transparency alpha from 0 (transparent) to 255 (opaque).</param>
        public void SetWindowTransparency(WindowInfo windowInfo, byte alpha) {
            IntPtr stylePtr = MonitorNativeMethods.GetWindowLongPtr(windowInfo.Handle, MonitorNativeMethods.GWL_EXSTYLE);
            int style = stylePtr.ToInt32();
            if ((style & MonitorNativeMethods.WS_EX_LAYERED) == 0) {
                MonitorNativeMethods.SetWindowLongPtr(windowInfo.Handle, MonitorNativeMethods.GWL_EXSTYLE, new IntPtr(style | MonitorNativeMethods.WS_EX_LAYERED));
            }

            if (!MonitorNativeMethods.SetLayeredWindowAttributes(windowInfo.Handle, 0, alpha, MonitorNativeMethods.LWA_ALPHA)) {
                throw new InvalidOperationException("Failed to set window transparency");
            }
        }
}
