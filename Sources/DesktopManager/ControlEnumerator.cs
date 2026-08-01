using System;
using System.Collections.Generic;
using System.Text;

namespace DesktopManager;

/// <summary>
/// Provides methods for enumerating child window controls.
/// </summary>
public class ControlEnumerator {
    private const long EditStylePassword = 0x0020;

    /// <summary>Enumerates all child controls of the given parent window.</summary>
    /// <param name="parent">Handle of the parent window.</param>
    /// <returns>List of control information.</returns>
    public List<WindowControlInfo> EnumerateControls(IntPtr parent) {
        List<WindowControlInfo> controls = EnumerateControlMetadata(parent);
        PopulateControlValues(controls);
        return controls;
    }

    internal List<WindowControlInfo> EnumerateControlMetadata(IntPtr parent) {
        List<WindowControlInfo> controls = new List<WindowControlInfo>();
        MonitorNativeMethods.EnumChildWindows(parent, (hWnd, lParam) => {
            controls.Add(CreateControlInfo(parent, hWnd, null, readValue: false));
            return true;
        }, IntPtr.Zero);
        return controls;
    }

    internal static void PopulateControlValues(IEnumerable<WindowControlInfo> controls) {
        foreach (WindowControlInfo control in controls) {
            if (control.IsPassword == true) {
                control.Text = string.Empty;
                control.Value = string.Empty;
                continue;
            }

            control.Text = WindowTextHelper.GetWindowText(control.Handle);
            control.Value = WindowControlService.SupportsSelection(control)
                ? WindowControlService.GetSelectedValue(control)
                : control.Text;
        }
    }

    internal WindowControlInfo GetControl(IntPtr parent, IntPtr handle, int maxTextLength) {
        if (handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid control handle.", nameof(handle));
        }

        if (maxTextLength < 1) {
            throw new ArgumentOutOfRangeException(nameof(maxTextLength), "maxTextLength must be greater than zero.");
        }

        return CreateControlInfo(parent, handle, maxTextLength, readValue: true);
    }

    internal static bool IsPasswordStyle(string? className, long style) {
        return className != null && className.Length > 0 &&
            className.IndexOf("Edit", StringComparison.OrdinalIgnoreCase) >= 0 &&
            (style & EditStylePassword) == EditStylePassword;
    }

    private static WindowControlInfo CreateControlInfo(IntPtr parent, IntPtr handle, int? maxTextLength, bool readValue) {
        WindowControlInfo info = new WindowControlInfo {
            ParentWindowHandle = parent,
            Handle = handle,
            Id = MonitorNativeMethods.GetDlgCtrlID(handle),
            Source = WindowControlSource.Win32,
            SupportsBackgroundClick = true,
            SupportsBackgroundText = true,
            SupportsBackgroundKeys = true
        };

        StringBuilder classBuilder = new StringBuilder(256);
        MonitorNativeMethods.GetClassName(handle, classBuilder, classBuilder.Capacity);
        info.ClassName = classBuilder.ToString();
        long style = MonitorNativeMethods.GetWindowLongPtr(handle, MonitorNativeMethods.GWL_STYLE).ToInt64();
        info.IsPassword = IsPasswordStyle(info.ClassName, style);
        if (info.IsPassword != true && readValue) {
            info.Text = maxTextLength.HasValue
                ? WindowTextHelper.GetWindowText(handle, maxTextLength.Value, out _)
                : WindowTextHelper.GetWindowText(handle);
            info.Value = WindowControlService.SupportsSelection(info)
                ? BoundValue(WindowControlService.GetSelectedValue(info), maxTextLength)
                : info.Text;
        }

        PopulateBounds(info, handle);
        return info;
    }

    internal static string BoundValue(string value, int? maxTextLength) {
        if (!maxTextLength.HasValue || value.Length <= maxTextLength.Value) {
            return value;
        }

        return value.Substring(0, maxTextLength.Value);
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
