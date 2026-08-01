using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace DesktopManager;

internal sealed partial class UiAutomationControlService {
    /// <summary>
    /// Subscribes to text, property, and structure changes under a window when UI Automation supports events.
    /// </summary>
    public IDisposable? TrySubscribeToChanges(IntPtr windowHandle, Action signal) {
        return TrySubscribeToChanges(windowHandle, signal, UiAutomationStaDispatcher.DefaultInvocationTimeoutMilliseconds);
    }

    internal IDisposable? TrySubscribeToChanges(IntPtr windowHandle, Action signal, int invocationTimeoutMilliseconds) {
        if (signal == null) {
            throw new ArgumentNullException(nameof(signal));
        }

        if (!IsAvailable || windowHandle == IntPtr.Zero) {
            return null;
        }

        return RunInSta(
            service => service.TrySubscribeToChangesCore(windowHandle, signal),
            windowHandle,
            invocationTimeoutMilliseconds: invocationTimeoutMilliseconds);
    }

    private IDisposable? TrySubscribeToChangesCore(IntPtr windowHandle, Action signal) {
        if (!TryResolveRootElement(windowHandle, out object? rootElement) || rootElement == null) {
            return null;
        }

        Type? automationType = _automationClientAssembly?.GetType("System.Windows.Automation.Automation", throwOnError: false);
        if (automationType == null) {
            return null;
        }

        object subtree = Enum.Parse(_treeScopeType!, "Subtree", ignoreCase: false);
        var cleanup = new List<Action>();
        IDisposable signalGuard = CreateGuardedEventSignal(signal, out Action guardedSignal);
        Action structureChangedSignal = CreateStructureChangedSignal(guardedSignal);
        TryAddTextChangedSubscription(automationType, rootElement, subtree, guardedSignal, cleanup);
        TryAddStructureChangedSubscription(automationType, rootElement, subtree, structureChangedSignal, cleanup);
        TryAddPropertyChangedSubscription(automationType, rootElement, subtree, guardedSignal, cleanup);
        if (cleanup.Count == 0) {
            signalGuard.Dispose();
            return null;
        }

        return new UiAutomationChangeSubscription(cleanup, signalGuard);
    }

    private void TryAddTextChangedSubscription(
        Type automationType,
        object rootElement,
        object subtree,
        Action signal,
        List<Action> cleanup) {
        try {
            Type? textPatternType = _automationClientAssembly?.GetType("System.Windows.Automation.TextPattern", throwOnError: false);
            object? textChangedEvent = textPatternType?.GetField("TextChangedEvent", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            MethodInfo? add = automationType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == "AddAutomationEventHandler" && method.GetParameters().Length == 4);
            MethodInfo? remove = automationType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == "RemoveAutomationEventHandler" && method.GetParameters().Length == 3);
            if (textChangedEvent == null || add == null || remove == null) {
                return;
            }

            Type handlerType = add.GetParameters()[3].ParameterType;
            Delegate handler = CreateSignalDelegate(handlerType, signal);
            add.Invoke(null, new[] { textChangedEvent, rootElement, subtree, handler });
            cleanup.Add(() => remove.Invoke(null, new[] { textChangedEvent, rootElement, handler }));
        } catch {
            // Other event families still provide a useful wake signal.
        }
    }

    private static void TryAddStructureChangedSubscription(
        Type automationType,
        object rootElement,
        object subtree,
        Action signal,
        List<Action> cleanup) {
        try {
            MethodInfo? add = automationType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == "AddStructureChangedEventHandler" && method.GetParameters().Length == 3);
            MethodInfo? remove = automationType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == "RemoveStructureChangedEventHandler" && method.GetParameters().Length == 2);
            if (add == null || remove == null) {
                return;
            }

            Type handlerType = add.GetParameters()[2].ParameterType;
            Delegate handler = CreateSignalDelegate(handlerType, signal);
            add.Invoke(null, new[] { rootElement, subtree, handler });
            cleanup.Add(() => remove.Invoke(null, new[] { rootElement, handler }));
        } catch {
            // Property and text events remain available when a provider rejects structure events.
        }
    }

    private void TryAddPropertyChangedSubscription(
        Type automationType,
        object rootElement,
        object subtree,
        Action signal,
        List<Action> cleanup) {
        try {
            MethodInfo? add = automationType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == "AddAutomationPropertyChangedEventHandler" && method.GetParameters().Length == 4);
            MethodInfo? remove = automationType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == "RemoveAutomationPropertyChangedEventHandler" && method.GetParameters().Length == 2);
            if (add == null || remove == null) {
                return;
            }

            Type handlerType = add.GetParameters()[2].ParameterType;
            Type? propertyType = add.GetParameters()[3].ParameterType.GetElementType();
            if (propertyType == null) {
                return;
            }

            IReadOnlyList<object> properties = GetObservedAutomationProperties(propertyType);
            if (properties.Count == 0) {
                return;
            }

            Array propertyArray = Array.CreateInstance(propertyType, properties.Count);
            for (int index = 0; index < properties.Count; index++) {
                propertyArray.SetValue(properties[index], index);
            }

            Delegate handler = CreateSignalDelegate(handlerType, signal);
            add.Invoke(null, new object[] { rootElement, subtree, handler, propertyArray });
            cleanup.Add(() => remove.Invoke(null, new[] { rootElement, handler }));
        } catch {
            // Polling remains the bounded fallback when property subscriptions fail.
        }
    }

    private IReadOnlyList<object> GetObservedAutomationProperties(Type propertyType) {
        var properties = new List<object>();
        AddAutomationProperty(properties, propertyType, _automationElementType, "NameProperty");
        AddAutomationProperty(properties, propertyType, _automationElementType, "HasKeyboardFocusProperty");
        AddAutomationProperty(properties, propertyType, _automationElementType, "IsEnabledProperty");
        AddAutomationProperty(properties, propertyType, _automationElementType, "IsOffscreenProperty");
        AddAutomationProperty(properties, propertyType, _automationClientAssembly?.GetType("System.Windows.Automation.ValuePattern", false), "ValueProperty");
        AddAutomationProperty(properties, propertyType, _automationClientAssembly?.GetType("System.Windows.Automation.RangeValuePattern", false), "ValueProperty");
        AddAutomationProperty(properties, propertyType, _automationClientAssembly?.GetType("System.Windows.Automation.TogglePattern", false), "ToggleStateProperty");
        AddAutomationProperty(properties, propertyType, _automationClientAssembly?.GetType("System.Windows.Automation.ExpandCollapsePattern", false), "ExpandCollapseStateProperty");
        AddAutomationProperty(properties, propertyType, _automationClientAssembly?.GetType("System.Windows.Automation.SelectionItemPattern", false), "IsSelectedProperty");
        AddAutomationProperty(properties, propertyType, _automationClientAssembly?.GetType("System.Windows.Automation.ScrollPattern", false), "HorizontalScrollPercentProperty");
        AddAutomationProperty(properties, propertyType, _automationClientAssembly?.GetType("System.Windows.Automation.ScrollPattern", false), "VerticalScrollPercentProperty");
        return properties;
    }

    private static void AddAutomationProperty(List<object> properties, Type propertyType, Type? ownerType, string fieldName) {
        object? property = ownerType?.GetField(fieldName, BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        if (property != null && propertyType.IsInstanceOfType(property) && !properties.Contains(property)) {
            properties.Add(property);
        }
    }

    internal static Delegate CreateSignalDelegate(Type delegateType, Action signal) {
        MethodInfo invoke = delegateType.GetMethod("Invoke") ?? throw new InvalidOperationException("The event delegate has no Invoke method.");
        ParameterExpression[] parameters = invoke.GetParameters()
            .Select(parameter => Expression.Parameter(parameter.ParameterType, parameter.Name))
            .ToArray();
        MethodCallExpression body = Expression.Call(Expression.Constant(signal), typeof(Action).GetMethod(nameof(Action.Invoke))!);
        return Expression.Lambda(delegateType, body, parameters).Compile();
    }

    internal static IDisposable CreateGuardedEventSignal(Action signal, out Action guardedSignal) {
        var guard = new UiAutomationEventSignal(signal);
        guardedSignal = guard.TrySignal;
        return guard;
    }

    internal static Action CreateStructureChangedSignal(Action signal) {
        if (signal == null) {
            throw new ArgumentNullException(nameof(signal));
        }

        return () => {
            InvalidateControlCaches();
            signal();
        };
    }

    private sealed class UiAutomationChangeSubscription : IDisposable {
        private readonly IReadOnlyList<Action> _cleanup;
        private readonly IDisposable _signalGuard;
        private int _disposed;

        public UiAutomationChangeSubscription(IReadOnlyList<Action> cleanup, IDisposable signalGuard) {
            _cleanup = cleanup;
            _signalGuard = signalGuard;
        }

        public void Dispose() {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) {
                return;
            }

            _signalGuard.Dispose();

            try {
                StaDispatcher.Value.Invoke(_ => {
                    foreach (Action cleanup in _cleanup) {
                        try {
                            cleanup();
                        } catch {
                            // Event providers may disappear before unsubscription.
                        }
                    }

                    return true;
                });
            } catch (TimeoutException) {
                // A wedged provider must not make wait cleanup block indefinitely.
            }
        }
    }

    private sealed class UiAutomationEventSignal : IDisposable {
        private readonly Action _signal;
        private int _disposed;

        internal UiAutomationEventSignal(Action signal) {
            _signal = signal;
        }

        internal void TrySignal() {
            if (Volatile.Read(ref _disposed) != 0) {
                return;
            }

            try {
                _signal();
            } catch {
                // Provider callbacks must not escape when the consumer or its wait handle has gone away.
            }
        }

        public void Dispose() {
            Interlocked.Exchange(ref _disposed, 1);
        }
    }
}
