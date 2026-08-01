using System;
using System.Collections.Generic;
using System.Text;

namespace DesktopManager;

/// <summary>
/// Provides methods for enumerating child window controls.
/// </summary>
public class ControlEnumerator {
    private const long EditStylePassword = 0x0020;
    private const int NativeReadTimeoutMilliseconds = 1000;

    /// <summary>Enumerates all child controls of the given parent window.</summary>
    /// <param name="parent">Handle of the parent window.</param>
    /// <returns>List of control information.</returns>
    public List<WindowControlInfo> EnumerateControls(IntPtr parent) {
        List<WindowControlInfo> controls = EnumerateControlMetadata(parent);
        PopulateControlValues(controls);
        return controls;
    }

    internal List<WindowControlInfo> EnumerateControls(IntPtr parent, int maxTextLength) {
        if (maxTextLength < 1) {
            throw new ArgumentOutOfRangeException(nameof(maxTextLength), "maxTextLength must be greater than zero.");
        }

        List<WindowControlInfo> controls = EnumerateControlMetadata(parent);
        PopulateControlValues(controls, maxTextLength);
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

    internal static void PopulateControlValues(
        IEnumerable<WindowControlInfo> controls,
        int? maxTextLength = null,
        Func<int>? getRemainingTimeoutMilliseconds = null) {
        if (maxTextLength.HasValue && maxTextLength.Value < 1) {
            throw new ArgumentOutOfRangeException(nameof(maxTextLength), "maxTextLength must be greater than zero.");
        }

        foreach (WindowControlInfo control in controls) {
            if (control.IsPassword != false) {
                control.Text = string.Empty;
                control.Value = string.Empty;
                continue;
            }

            int boundedLength = maxTextLength ?? DesktopTextObservationOptions.MaximumTextLength;
            int textTimeoutMilliseconds = getRemainingTimeoutMilliseconds?.Invoke() ?? NativeReadTimeoutMilliseconds;
            bool textAvailable = WindowControlService.TryGetControlText(
                control,
                boundedLength,
                textTimeoutMilliseconds,
                out string text,
                out _);
            control.Text = textAvailable ? text : string.Empty;
            control.ValueIsTruncated = false;
            if (WindowControlService.SupportsSelection(control)) {
                int selectionTimeoutMilliseconds = getRemainingTimeoutMilliseconds?.Invoke() ?? NativeReadTimeoutMilliseconds;
                if (WindowControlService.TryGetSelectedValue(
                        control,
                        boundedLength,
                        selectionTimeoutMilliseconds,
                        out string selectedValue,
                        out bool valueIsTruncated)) {
                    control.Value = selectedValue;
                    control.ValueIsTruncated = valueIsTruncated;
                } else {
                    control.Value = string.Empty;
                }
            } else {
                control.Value = control.Text;
            }
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

    internal WindowControlInfo GetControlMetadata(IntPtr parent, IntPtr handle) {
        if (handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid control handle.", nameof(handle));
        }

        return CreateControlInfo(parent, handle, maxTextLength: null, readValue: false);
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
        int classNameLength = MonitorNativeMethods.GetClassName(handle, classBuilder, classBuilder.Capacity);
        info.ClassName = classBuilder.ToString();
        long style = MonitorNativeMethods.GetWindowLongPtr(handle, MonitorNativeMethods.GWL_STYLE).ToInt64();
        info.IsPassword = classNameLength > 0 ? IsPasswordStyle(info.ClassName, style) : null;
        if (info.IsPassword == false && readValue) {
            PopulateControlValues(new[] { info }, maxTextLength);
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
