using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DesktopManager;

internal sealed partial class UiAutomationControlService {
    private DesktopControlIdentity CreateObservationIdentity(
        WindowInfo window,
        WindowControlInfo control,
        object element,
        DesktopControlObservationOptions options,
        List<string> errors) {
        string runtimeId = ReadRuntimeId(element, errors);
        string ancestorPath = ReadAncestorPath(element, options.MaxAncestorDepth, errors);
        var identity = new DesktopControlIdentity {
            ProcessId = window.ProcessId,
            WindowHandle = window.Handle,
            ControlHandle = control.Handle,
            RuntimeId = ResolveObservationRuntimeId(runtimeId, control.RuntimeId),
            AutomationId = control.AutomationId,
            ControlType = control.ControlType,
            FrameworkId = control.FrameworkId,
            ClassName = control.ClassName,
            AncestorPath = ancestorPath,
            Left = control.Left,
            Top = control.Top,
            Width = control.Width,
            Height = control.Height
        };
        identity.SessionKey = CreateObservationSessionKey(identity);
        return identity;
    }

    internal static string ResolveObservationRuntimeId(string? refreshedRuntimeId, string? discoveredRuntimeId) {
        return !string.IsNullOrWhiteSpace(refreshedRuntimeId)
            ? refreshedRuntimeId!
            : discoveredRuntimeId ?? string.Empty;
    }

    internal static string CreateObservationSessionKey(DesktopControlIdentity identity) {
        if (identity == null) {
            throw new ArgumentNullException(nameof(identity));
        }

        if (!string.IsNullOrWhiteSpace(identity.RuntimeId)) {
            return $"p:{identity.ProcessId}|w:{identity.WindowHandle.ToInt64()}|r:{identity.RuntimeId}";
        }

        if (identity.ControlHandle != IntPtr.Zero) {
            return $"p:{identity.ProcessId}|w:{identity.WindowHandle.ToInt64()}|h:{identity.ControlHandle.ToInt64()}";
        }

        return $"p:{identity.ProcessId}|w:{identity.WindowHandle.ToInt64()}|a:{identity.AutomationId}|t:{identity.ControlType}|f:{identity.FrameworkId}|c:{identity.ClassName}|b:{identity.Left},{identity.Top},{identity.Width},{identity.Height}|x:{identity.AncestorPath}";
    }

    private static string ReadRuntimeId(object element, List<string>? errors) {
        try {
            object? value = element.GetType().GetMethod("GetRuntimeId", Type.EmptyTypes)?.Invoke(element, null);
            if (value is int[] runtimeId) {
                return string.Join(".", runtimeId.Select(item => item.ToString()));
            }

            return string.Empty;
        } catch (Exception ex) {
            if (errors != null) {
                AddObservationError(errors, "identity.runtimeId", ex);
            }
            return string.Empty;
        }
    }

    private string ReadAncestorPath(object element, int maxDepth, List<string> errors) {
        if (maxDepth <= 0) {
            return string.Empty;
        }

        try {
            Type? treeWalkerType = _automationClientAssembly?.GetType("System.Windows.Automation.TreeWalker", throwOnError: false);
            object? walker = treeWalkerType?.GetProperty("ControlViewWalker", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            MethodInfo? getParent = walker?.GetType().GetMethod("GetParent", new[] { _automationElementType! });
            if (walker == null || getParent == null) {
                return string.Empty;
            }

            var segments = new List<string>();
            object? currentElement = element;
            for (int depth = 0; depth < maxDepth && currentElement != null; depth++) {
                object? current = currentElement.GetType().GetProperty("Current", BindingFlags.Public | BindingFlags.Instance)?.GetValue(currentElement);
                if (current == null) {
                    break;
                }

                string controlType = ReadControlTypeName(current);
                string automationId = ReadString(current, "AutomationId");
                string className = ReadString(current, "ClassName");
                segments.Add(CreateAncestorSegment(controlType, automationId, className));
                currentElement = getParent.Invoke(walker, new[] { currentElement });
            }

            segments.Reverse();
            return string.Join("/", segments);
        } catch (Exception ex) {
            AddObservationError(errors, "identity.ancestorPath", ex);
            return string.Empty;
        }
    }

    internal static string CreateAncestorSegment(string? controlType, string? automationId, string? className) {
        string type = string.IsNullOrWhiteSpace(controlType) ? "Control" : controlType!;
        if (!string.IsNullOrWhiteSpace(automationId)) {
            return $"{type}#{automationId}";
        }

        return !string.IsNullOrWhiteSpace(className) ? $"{type}.{className}" : type;
    }
}
