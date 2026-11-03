using System;
using System.Reflection;

namespace DesktopManager;

internal sealed partial class UiAutomationControlService {
    private static object? GetAutomationProperty(object instance, string name) {
#if NET8_0_OR_GREATER
        return instance switch {
            UiaElement value => name switch {
                "Current" => value.Current,
                "Name" => value.Name,
                "ClassName" => value.ClassName,
                "AutomationId" => value.AutomationId,
                "FrameworkId" => value.FrameworkId,
                "ProcessId" => value.ProcessId,
                "NativeWindowHandle" => value.NativeWindowHandle,
                "IsPassword" => value.IsPassword,
                "IsEnabled" => value.IsEnabled,
                "IsKeyboardFocusable" => value.IsKeyboardFocusable,
                "HasKeyboardFocus" => value.HasKeyboardFocus,
                "IsOffscreen" => value.IsOffscreen,
                "HelpText" => value.HelpText,
                "LocalizedControlType" => value.LocalizedControlType,
                "ItemStatus" => value.ItemStatus,
                "ItemType" => value.ItemType,
                "AccessKey" => value.AccessKey,
                "AcceleratorKey" => value.AcceleratorKey,
                "AriaRole" => value.AriaRole,
                "AriaProperties" => value.AriaProperties,
                "ControlType" => value.ControlType,
                "BoundingRectangle" => value.BoundingRectangle,
                _ => null
            },
            UiaBounds value => name switch {
                "Left" => value.Left,
                "Top" => value.Top,
                "Width" => value.Width,
                "Height" => value.Height,
                _ => null
            },
            UiaPattern value => name switch {
                "Current" => value.Current,
                "Value" => value.Value,
                "IsReadOnly" => value.IsReadOnly,
                "Minimum" => value.Minimum,
                "Maximum" => value.Maximum,
                "LargeChange" => value.LargeChange,
                "SmallChange" => value.SmallChange,
                "CanSelectMultiple" => value.CanSelectMultiple,
                "IsSelectionRequired" => value.IsSelectionRequired,
                "IsSelected" => value.IsSelected,
                "HorizontalScrollPercent" => value.HorizontalScrollPercent,
                "VerticalScrollPercent" => value.VerticalScrollPercent,
                "HorizontalViewSize" => value.HorizontalViewSize,
                "VerticalViewSize" => value.VerticalViewSize,
                "HorizontallyScrollable" => value.HorizontallyScrollable,
                "VerticallyScrollable" => value.VerticallyScrollable,
                "RowCount" => value.RowCount,
                "ColumnCount" => value.ColumnCount,
                "Row" => value.Row,
                "Column" => value.Column,
                "RowSpan" => value.RowSpan,
                "ColumnSpan" => value.ColumnSpan,
                "ToggleState" => value.ToggleState,
                "ExpandCollapseState" => value.ExpandCollapseState,
                "RowOrColumnMajor" => value.RowOrColumnMajor,
                "DocumentRange" => value.DocumentRange,
                "SupportedTextSelection" => value.SupportedTextSelection,
                _ => null
            },
            _ => null
        };
#else
        return instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance);
#endif
    }
}
