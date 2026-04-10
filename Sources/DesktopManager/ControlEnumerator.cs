using System;
using System.Collections.Generic;
using System.Text;

namespace DesktopManager;

/// <summary>
/// Provides methods for enumerating child window controls.
/// </summary>
public class ControlEnumerator {
    /// <summary>Enumerates all child controls of the given parent window.</summary>
    /// <param name="parent">Handle of the parent window.</param>
    /// <returns>List of control information.</returns>
    public List<WindowControlInfo> EnumerateControls(IntPtr parent) {
        List<WindowControlInfo> controls = new List<WindowControlInfo>();
        MonitorNativeMethods.EnumChildWindows(parent, (hWnd, lParam) => {
            WindowControlInfo info = new WindowControlInfo {
                ParentWindowHandle = parent,
                Handle = hWnd,
                Id = MonitorNativeMethods.GetDlgCtrlID(hWnd),
                Source = WindowControlSource.Win32,
                SupportsBackgroundClick = true,
                SupportsBackgroundText = true,
                SupportsBackgroundKeys = true
            };

            StringBuilder classSb = new StringBuilder(256);
            MonitorNativeMethods.GetClassName(hWnd, classSb, classSb.Capacity);
            info.ClassName = classSb.ToString();
            info.Text = WindowTextHelper.GetWindowText(hWnd);
            info.Value = WindowControlService.SupportsSelection(info)
                ? WindowControlService.GetSelectedValue(info)
                : info.Text;
            PopulateBounds(info, hWnd);

            controls.Add(info);
            return true;
        }, IntPtr.Zero);
        return controls;
    }

    private static void PopulateBounds(WindowControlInfo control, IntPtr handle) {
        if (!MonitorNativeMethods.GetWindowRect(handle, out RECT rect)) {
            return;
        }

        control.Left = rect.Left;
        control.Top = rect.Top;
        control.Width = Math.Max(0, rect.Right - rect.Left);
        control.Height = Math.Max(0, rect.Bottom - rect.Top);
        control.IsOffscreen = false;
    }
}
