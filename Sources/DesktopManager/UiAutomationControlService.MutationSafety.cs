using System;
using System.Linq;
using System.Reflection;

namespace DesktopManager;

internal sealed partial class UiAutomationControlService {
    private bool TryPatternAction(object element, string patternTypeName, string methodName, params object[] parameters) {
        return TryPatternAction(element, patternTypeName, methodName, mutation: false, parameters: parameters);
    }

    private bool TryPatternMutation(object element, string patternTypeName, string methodName, params object[] parameters) {
        return TryPatternAction(element, patternTypeName, methodName, mutation: true, parameters: parameters);
    }

    private bool TryPatternAction(
        object element,
        string patternTypeName,
        string methodName,
        bool mutation,
        params object[] parameters) {
        Type? patternType = _automationClientAssembly?.GetType(patternTypeName, throwOnError: false);
        if (patternType == null) {
            return false;
        }

        object? pattern = GetCurrentPattern(element, patternType);
        if (pattern == null) {
            return false;
        }

        MethodInfo? method = pattern.GetType().GetMethod(methodName, parameters.Select(parameter => parameter.GetType()).ToArray());
        if (method == null) {
            return false;
        }

        if (mutation) {
            InvokeProviderMutation(pattern, method, parameters, patternTypeName, methodName);
            return true;
        }

        try {
            method.Invoke(pattern, parameters);
            return true;
        } catch {
            return false;
        }
    }

    internal static void InvokeProviderMutation(
        object pattern,
        MethodInfo method,
        object[] parameters,
        string patternTypeName,
        string methodName) {
        try {
            method.Invoke(pattern, parameters);
        } catch (Exception ex) {
            Exception providerException = ex is TargetInvocationException invocationException && invocationException.InnerException != null
                ? invocationException.InnerException
                : ex;
            throw new UiAutomationMutationOutcomeUnknownException(patternTypeName, methodName, providerException);
        }
    }
}

/// <summary>
/// Indicates that a UI Automation provider threw after a mutation invocation began, so the resulting state cannot be known safely.
/// </summary>
internal sealed class UiAutomationMutationOutcomeUnknownException : InvalidOperationException {
    internal UiAutomationMutationOutcomeUnknownException(string patternTypeName, string methodName, Exception innerException)
        : base($"UI Automation mutation {patternTypeName}.{methodName} failed after invocation began; its outcome is unknown.", innerException) {
    }
}
