using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DesktopManager;

internal sealed partial class UiAutomationControlService {
    private const int EnumeratedControlsCacheMilliseconds = 750;
    private const int ActionMatchCacheMilliseconds = 5000;
    private const int ForegroundTextVerificationMilliseconds = 1000;
    private const int ForegroundTextVerificationIntervalMilliseconds = 50;
    private const int ForegroundInputSettleMilliseconds = 75;
    internal const int PreferredSearchRootsMaximumCount = 256;
    private const int EnumeratedControlsCacheMaximumCount = 512;
    private const int ActionMatchCacheMaximumCount = 512;
    private static readonly Lazy<UiAutomationStaDispatcher> StaDispatcher = new(() => new UiAutomationStaDispatcher());
    private static readonly ConcurrentDictionary<IntPtr, IntPtr> PreferredSearchRoots = new();
    private static readonly ConcurrentDictionary<string, CachedControlCollection> EnumeratedControlsCache = new();
    private static readonly ConcurrentDictionary<string, CachedActionMatch> ActionMatchCache = new();
    private readonly Assembly? _automationClientAssembly;
    private readonly Assembly? _automationTypesAssembly;
    private readonly Type? _automationElementType;
    private readonly Type? _automationElementCollectionType;
    private readonly Type? _conditionType;
    private readonly Type? _treeScopeType;

    internal bool LastOperationTimedOut { get; private set; }

    public UiAutomationControlService() {
        _automationClientAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(candidate => string.Equals(candidate.GetName().Name, "UIAutomationClient", StringComparison.OrdinalIgnoreCase));
        _automationClientAssembly ??= TryLoadAssembly("UIAutomationClient");

        _automationTypesAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(candidate => string.Equals(candidate.GetName().Name, "UIAutomationTypes", StringComparison.OrdinalIgnoreCase));
        _automationTypesAssembly ??= TryLoadAssembly("UIAutomationTypes");

        _automationElementType = _automationClientAssembly?.GetType("System.Windows.Automation.AutomationElement", throwOnError: false);
        _automationElementCollectionType = _automationClientAssembly?.GetType("System.Windows.Automation.AutomationElementCollection", throwOnError: false);
        _conditionType = _automationClientAssembly?.GetType("System.Windows.Automation.Condition", throwOnError: false);
        _treeScopeType = _automationTypesAssembly?.GetType("System.Windows.Automation.TreeScope", throwOnError: false)
            ?? _automationClientAssembly?.GetType("System.Windows.Automation.TreeScope", throwOnError: false);
    }

    public bool IsAvailable => _automationElementType != null &&
        _automationElementCollectionType != null &&
        _conditionType != null &&
        _treeScopeType != null;

    public List<WindowControlInfo> EnumerateControls(IntPtr windowHandle, IReadOnlyList<IntPtr>? fallbackRootHandles = null) {
        if (!IsAvailable || windowHandle == IntPtr.Zero) {
            return new List<WindowControlInfo>();
        }

        return RunInSta(service => service.EnumerateControlsCore(windowHandle, fallbackRootHandles), windowHandle);
    }

    internal static IReadOnlyList<IntPtr> GetFallbackRootHandles(IntPtr windowHandle, IEnumerable<WindowControlInfo>? win32Controls) {
        if (windowHandle == IntPtr.Zero || win32Controls == null) {
            return Array.Empty<IntPtr>();
        }

        var prioritized = new List<IntPtr>();
        var remaining = new List<IntPtr>();
        var seen = new HashSet<IntPtr>();
        foreach (WindowControlInfo control in win32Controls) {
            if (control.Handle == IntPtr.Zero || control.Handle == windowHandle || !seen.Add(control.Handle)) {
                continue;
            }

            if (string.Equals(control.ClassName, "Chrome_RenderWidgetHostHWND", StringComparison.OrdinalIgnoreCase)) {
                prioritized.Add(control.Handle);
            } else {
                remaining.Add(control.Handle);
            }
        }

        prioritized.AddRange(remaining);
        return prioritized;
    }

    internal static IntPtr GetPreferredSearchRootHandle(IntPtr windowHandle, IReadOnlyList<IntPtr>? fallbackRootHandles = null) {
        if (windowHandle == IntPtr.Zero) {
            return IntPtr.Zero;
        }

        if (!PreferredSearchRoots.TryGetValue(windowHandle, out IntPtr preferredHandle) || preferredHandle == IntPtr.Zero) {
            return IntPtr.Zero;
        }

        if (preferredHandle == windowHandle) {
            return preferredHandle;
        }

        if (fallbackRootHandles != null && fallbackRootHandles.Contains(preferredHandle)) {
            return preferredHandle;
        }

        PreferredSearchRoots.TryRemove(windowHandle, out _);
        return IntPtr.Zero;
    }

    internal static void RememberPreferredSearchRootHandle(IntPtr windowHandle, IntPtr rootHandle) {
        if (windowHandle == IntPtr.Zero || rootHandle == IntPtr.Zero) {
            return;
        }

        PreferredSearchRoots[windowHandle] = rootHandle;
        TrimPreferredSearchRoots();
    }

    internal static void ForgetPreferredSearchRootHandle(IntPtr windowHandle, IntPtr rootHandle) {
        if (windowHandle == IntPtr.Zero || rootHandle == IntPtr.Zero) {
            return;
        }

        if (PreferredSearchRoots.TryGetValue(windowHandle, out IntPtr current) && current == rootHandle) {
            PreferredSearchRoots.TryRemove(windowHandle, out _);
        }
    }

    public bool TryInvoke(WindowInfo window, WindowControlInfo control) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        return RunInSta(service => service.TryInvokeCore(window, control), window.Handle);
    }

    public bool TrySetValue(WindowInfo window, WindowControlInfo control, string value) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        if (value == null) {
            throw new ArgumentNullException(nameof(value));
        }

        return RunInSta(service => service.TrySetValueCore(window, control, value), window.Handle);
    }

    public bool TrySetText(WindowInfo window, WindowControlInfo control, string value, bool ensureForegroundWindow) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        if (value == null) {
            throw new ArgumentNullException(nameof(value));
        }

        return RunInSta(service => service.TrySetTextCore(window, control, value, ensureForegroundWindow), window.Handle);
    }

    public bool TrySetCheckState(WindowInfo window, WindowControlInfo control, bool check) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        return RunInSta(service => service.TrySetCheckStateCore(window, control, check), window.Handle);
    }

    public bool TrySetSelectedValue(WindowInfo window, WindowControlInfo control, string selectedValue) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        if (selectedValue == null) {
            throw new ArgumentNullException(nameof(selectedValue));
        }

        return RunInSta(service => service.TrySetSelectedValueCore(window, control, selectedValue), window.Handle);
    }

    public bool TrySendKeys(WindowInfo window, WindowControlInfo control, IReadOnlyList<VirtualKey> keys, bool ensureForegroundWindow) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        if (keys == null || keys.Count == 0) {
            throw new ArgumentException("At least one key is required.", nameof(keys));
        }

        return RunInSta(service => service.TrySendKeysCore(window, control, keys, ensureForegroundWindow), window.Handle);
    }

    public bool? TryReadCheckState(WindowInfo window, WindowControlInfo control) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        return RunInSta(service => service.TryReadCheckStateCore(window, control), window.Handle);
    }

    public string? TryReadSelectedValue(WindowInfo window, WindowControlInfo control) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        return RunInSta(service => service.TryReadSelectedValueCore(window, control), window.Handle);
    }

    public bool TryFocus(WindowInfo window, WindowControlInfo control, bool ensureForegroundWindow) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        return RunInSta(service => service.TryFocusCore(window, control, ensureForegroundWindow), window.Handle);
    }

    public IReadOnlyList<DesktopUiAutomationRootDiagnostic> DiagnoseRoots(IntPtr windowHandle, IReadOnlyList<IntPtr>? fallbackRootHandles = null, int sampleLimit = 3) {
        if (sampleLimit < 0) {
            throw new ArgumentOutOfRangeException(nameof(sampleLimit), "sampleLimit must be zero or greater.");
        }

        if (!IsAvailable || windowHandle == IntPtr.Zero) {
            return Array.Empty<DesktopUiAutomationRootDiagnostic>();
        }

        return RunInSta(service => service.DiagnoseRootsCore(windowHandle, fallbackRootHandles, sampleLimit), windowHandle)
            ?? Array.Empty<DesktopUiAutomationRootDiagnostic>();
    }

    public DesktopUiAutomationActionDiagnostic ProbeActionResolution(WindowInfo window, WindowControlInfo control) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        return RunInSta(service => service.ProbeActionResolutionCore(window, control), window.Handle);
    }

    private T RunInSta<T>(Func<UiAutomationControlService, T> operation, IntPtr targetWindowHandle = default) {
        if (!IsAvailable) {
            return default!;
        }

        LastOperationTimedOut = false;
        if (StaDispatcher.Value.IsCurrentThread || IsWindowOwnedByCurrentThread(targetWindowHandle)) {
            return operation(this);
        }

        try {
            if (TryRunWithCurrentUiMessagePump(() => StaDispatcher.Value.Invoke(operation), out T pumpedResult)) {
                return pumpedResult;
            }

            return StaDispatcher.Value.Invoke(operation);
        } catch (TimeoutException) {
            LastOperationTimedOut = true;
            return CreateTimedOutOperationFallback<T>();
        }
    }

    private static bool IsWindowOwnedByCurrentThread(IntPtr windowHandle) {
        return windowHandle != IntPtr.Zero &&
            MonitorNativeMethods.GetWindowThreadProcessId(windowHandle, out _) == MonitorNativeMethods.GetCurrentThreadId();
    }

    private static T CreateTimedOutOperationFallback<T>() {
        Type resultType = typeof(T);
        if (resultType.IsArray) {
            return (T)(object)Array.CreateInstance(resultType.GetElementType()!, 0);
        }

        if (!resultType.IsAbstract && !resultType.IsInterface && resultType.GetConstructor(Type.EmptyTypes) != null) {
            return (T)Activator.CreateInstance(resultType)!;
        }

        return default!;
    }

    private List<WindowControlInfo> EnumerateControlsCore(IntPtr windowHandle, IReadOnlyList<IntPtr>? fallbackRootHandles) {
        try {
            IntPtr preferredRootHandle = GetPreferredSearchRootHandle(windowHandle, fallbackRootHandles);
            if (preferredRootHandle != IntPtr.Zero && preferredRootHandle != windowHandle) {
                List<WindowControlInfo> preferredControls = EnumerateControlsForRoot(preferredRootHandle, includeRoot: true, out _);
                if (preferredControls.Count > 0) {
                    return preferredControls;
                }

                ForgetPreferredSearchRootHandle(windowHandle, preferredRootHandle);
            }

            List<WindowControlInfo> primaryControls = EnumerateControlsForRoot(windowHandle, includeRoot: false, out _);
            if (primaryControls.Count > 0 || fallbackRootHandles == null || fallbackRootHandles.Count == 0) {
                if (primaryControls.Count > 0) {
                    RememberPreferredSearchRootHandle(windowHandle, windowHandle);
                }

                return primaryControls;
            }

            var mergedControls = new List<WindowControlInfo>();
            foreach (IntPtr fallbackRootHandle in OrderFallbackRootHandles(fallbackRootHandles, preferredRootHandle)) {
                List<WindowControlInfo> fallbackControls = EnumerateControlsForRoot(fallbackRootHandle, includeRoot: true, out _);
                if (fallbackControls.Count > 0) {
                    RememberPreferredSearchRootHandle(windowHandle, fallbackRootHandle);
                }

                foreach (WindowControlInfo control in fallbackControls) {
                    if (!ContainsEquivalentControl(mergedControls, control)) {
                        mergedControls.Add(control);
                    }
                }
            }

            return mergedControls;
        } catch {
            return new List<WindowControlInfo>();
        }
    }

    private IReadOnlyList<DesktopUiAutomationRootDiagnostic> DiagnoseRootsCore(IntPtr windowHandle, IReadOnlyList<IntPtr>? fallbackRootHandles, int sampleLimit) {
        IntPtr preferredRootHandle = GetPreferredSearchRootHandle(windowHandle, fallbackRootHandles);
        IReadOnlyList<IntPtr> rootHandles = GetSearchRootHandles(windowHandle, fallbackRootHandles?.Select(handle => new WindowControlInfo {
            Handle = handle
        }));
        var diagnostics = new List<DesktopUiAutomationRootDiagnostic>(rootHandles.Count);
        for (int index = 0; index < rootHandles.Count; index++) {
            IntPtr rootHandle = rootHandles[index];
            bool includeRoot = rootHandle != windowHandle;
            string? error = null;
            bool elementResolved = false;
            List<WindowControlInfo> controls = new();

            try {
                elementResolved = TryResolveRootElement(rootHandle, out _);
                if (elementResolved) {
                    controls = EnumerateControlsForRoot(rootHandle, includeRoot, out bool usedCache);
                    diagnostics.Add(new DesktopUiAutomationRootDiagnostic {
                        Order = index,
                        Handle = rootHandle,
                        ClassName = ReadWindowClassName(rootHandle),
                        IsPrimaryRoot = rootHandle == windowHandle,
                        IsPreferredRoot = preferredRootHandle != IntPtr.Zero && rootHandle == preferredRootHandle,
                        UsedCachedControls = usedCache,
                        IncludeRoot = includeRoot,
                        ElementResolved = elementResolved,
                        ControlCount = controls.Count,
                        SampleControls = controls.Take(sampleLimit).ToArray(),
                        Error = error
                    });
                    continue;
                }
            } catch (Exception ex) {
                error = ex.InnerException?.Message ?? ex.Message;
            }

            diagnostics.Add(new DesktopUiAutomationRootDiagnostic {
                Order = index,
                Handle = rootHandle,
                ClassName = ReadWindowClassName(rootHandle),
                IsPrimaryRoot = rootHandle == windowHandle,
                IsPreferredRoot = preferredRootHandle != IntPtr.Zero && rootHandle == preferredRootHandle,
                UsedCachedControls = false,
                IncludeRoot = includeRoot,
                ElementResolved = elementResolved,
                ControlCount = controls.Count,
                SampleControls = controls.Take(sampleLimit).ToArray(),
                Error = error
            });
        }

        return diagnostics;
    }

    private List<WindowControlInfo> EnumerateControlsForRoot(IntPtr rootHandle, bool includeRoot, out bool usedCache) {
        if (TryGetCachedEnumeratedControls(rootHandle, includeRoot, out List<WindowControlInfo> cachedControls)) {
            usedCache = true;
            return cachedControls;
        }

        usedCache = false;
        if (!TryResolveRootElement(rootHandle, out object? rootElement) || rootElement == null) {
            return new List<WindowControlInfo>();
        }

        object? treeScope = Enum.Parse(_treeScopeType!, includeRoot ? "Subtree" : "Descendants", ignoreCase: false);
        object? trueCondition = _conditionType!.GetField("TrueCondition", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        if (treeScope == null || trueCondition == null) {
            return new List<WindowControlInfo>();
        }

        object? collection = _automationElementType!.GetMethod("FindAll", new[] { _treeScopeType!, _conditionType! })?
            .Invoke(rootElement, new[] { treeScope, trueCondition });
        if (collection == null) {
            return new List<WindowControlInfo>();
        }

        PropertyInfo? countProperty = _automationElementCollectionType!.GetProperty("Count");
        PropertyInfo? itemProperty = _automationElementCollectionType.GetProperty("Item");
        if (countProperty == null || itemProperty == null) {
            return new List<WindowControlInfo>();
        }

        int count = (int)(countProperty.GetValue(collection) ?? 0);
        var controls = new List<WindowControlInfo>(count);
        for (int index = 0; index < count; index++) {
            object? element = itemProperty.GetValue(collection, new object[] { index });
            if (element == null) {
                continue;
            }

            try {
                WindowControlInfo? info = CreateControlInfo(element);
                if (info != null) {
                    controls.Add(info);
                }
            } catch {
                // Some Chromium-hosted UIA elements throw for unsupported patterns.
                // Skip the single element instead of failing the entire root probe.
            }
        }

        CacheEnumeratedControls(rootHandle, includeRoot, controls);
        return controls;
    }

    private bool TryInvokeCore(WindowInfo window, WindowControlInfo control) {
        UiAutomationElementMatchResult match = ResolveMatchingElement(window.Handle, control);
        object? element = match.Element;
        if (element == null) {
            return false;
        }

        return TryPatternAction(element, "System.Windows.Automation.InvokePattern", "Invoke") ||
            TryPatternAction(element, "System.Windows.Automation.SelectionItemPattern", "Select") ||
            TryPatternAction(element, "System.Windows.Automation.ExpandCollapsePattern", "Expand") ||
            TryPatternAction(element, "System.Windows.Automation.TogglePattern", "Toggle") ||
            TryPatternAction(element, "System.Windows.Automation.LegacyIAccessiblePattern", "DoDefaultAction");
    }

    private bool TrySetCheckStateCore(WindowInfo window, WindowControlInfo control, bool check) {
        UiAutomationElementMatchResult match = ResolveMatchingElement(window.Handle, control);
        object? element = match.Element;
        if (element == null) {
            return false;
        }

        for (int attempt = 0; attempt < 3; attempt++) {
            bool? currentState = ReadCheckState(element);
            if (currentState.HasValue && currentState.Value == check) {
                return true;
            }

            bool actionApplied =
                TryPatternAction(element, "System.Windows.Automation.TogglePattern", "Toggle") ||
                TryPatternAction(element, "System.Windows.Automation.InvokePattern", "Invoke") ||
                TryPatternAction(element, "System.Windows.Automation.LegacyIAccessiblePattern", "DoDefaultAction");
            if (!actionApplied) {
                return false;
            }

            if (WaitForResolvedCheckState(window, control, check)) {
                return true;
            }

            match = ResolveMatchingElement(window.Handle, control);
            element = match.Element;
            if (element == null) {
                return false;
            }
        }

        return false;
    }

    private bool TrySetValueCore(WindowInfo window, WindowControlInfo control, string value) {
        UiAutomationElementMatchResult match = ResolveMatchingElement(window.Handle, control);
        object? element = match.Element;
        if (element == null) {
            return false;
        }

        bool patternApplied =
            TryPatternAction(element, "System.Windows.Automation.ValuePattern", "SetValue", value) ||
            TryPatternAction(element, "System.Windows.Automation.LegacyIAccessiblePattern", "SetValue", value);
        if (!patternApplied) {
            return false;
        }

        return WaitForResolvedValue(window, control, value);
    }

    private bool TrySetSelectedValueCore(WindowInfo window, WindowControlInfo control, string selectedValue) {
        UiAutomationElementMatchResult match = ResolveMatchingElement(window.Handle, control);
        object? element = match.Element;
        if (element == null) {
            return false;
        }

        string? currentSelection = ReadSelectedValue(element);
        if (string.Equals(currentSelection, selectedValue, StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        bool patternApplied =
            TryPatternAction(element, "System.Windows.Automation.ValuePattern", "SetValue", selectedValue) ||
            TryPatternAction(element, "System.Windows.Automation.LegacyIAccessiblePattern", "SetValue", selectedValue);
        if (patternApplied && WaitForResolvedSelectedValue(window, control, selectedValue)) {
            return true;
        }

        bool expanded = TryPatternAction(element, "System.Windows.Automation.ExpandCollapsePattern", "Expand");
        if (expanded) {
            WaitWithCurrentUiMessagePump(ForegroundInputSettleMilliseconds);
        }

        try {
            object? candidate = FindSelectionCandidateElement(element, selectedValue);
            if (candidate == null && TryResolveRootElement(window.Handle, out object? rootElement) && rootElement != null) {
                candidate = FindSelectionCandidateElement(rootElement, selectedValue);
            }

            if (candidate == null) {
                return false;
            }

            bool itemApplied =
                TryPatternAction(candidate, "System.Windows.Automation.SelectionItemPattern", "Select") ||
                TryPatternAction(candidate, "System.Windows.Automation.InvokePattern", "Invoke") ||
                TryPatternAction(candidate, "System.Windows.Automation.LegacyIAccessiblePattern", "DoDefaultAction");
            if (!itemApplied) {
                return false;
            }

            control.Value = selectedValue;
            control.Text = selectedValue;
            return true;
        } finally {
            if (expanded) {
                TryPatternAction(element, "System.Windows.Automation.ExpandCollapsePattern", "Collapse");
            }
        }
    }

    private bool TrySetTextCore(WindowInfo window, WindowControlInfo control, string value, bool ensureForegroundWindow) {
        UiAutomationElementMatchResult match = ResolveMatchingElement(window.Handle, control);
        object? element = match.Element;
        if (element == null) {
            return false;
        }

        if (TryPatternAction(element, "System.Windows.Automation.ValuePattern", "SetValue", value) ||
            TryPatternAction(element, "System.Windows.Automation.LegacyIAccessiblePattern", "SetValue", value)) {
            return true;
        }

        TryPatternAction(element, "System.Windows.Automation.ScrollItemPattern", "ScrollIntoView");
        if (!TrySetFocus(element)) {
            return false;
        }

        if (ensureForegroundWindow && !WindowActivationService.TryPrepareWindowForAutomation(window.Handle)) {
            return false;
        }

        if (MonitorNativeMethods.GetForegroundWindow() != window.Handle) {
            return false;
        }

        if (TryReplaceFocusedTextWithPaste(window, control, value)) {
            return true;
        }

        KeyboardInputService.SendToForeground(VirtualKey.VK_CONTROL, VirtualKey.VK_A);
        WaitWithCurrentUiMessagePump(ForegroundInputSettleMilliseconds);
        if (value.Length == 0) {
            KeyboardInputService.SendToForeground(VirtualKey.VK_DELETE);
        } else {
            KeyboardInputService.SendTextToForeground(value);
        }

        return WaitForResolvedValue(window, control, value);
    }

    private bool? TryReadCheckStateCore(WindowInfo window, WindowControlInfo control) {
        UiAutomationElementMatchResult match = ResolveMatchingElement(window.Handle, control);
        return match.Element == null
            ? null
            : ReadCheckState(match.Element);
    }

    private string? TryReadSelectedValueCore(WindowInfo window, WindowControlInfo control) {
        UiAutomationElementMatchResult match = ResolveMatchingElement(window.Handle, control);
        return match.Element == null
            ? null
            : ReadSelectedValue(match.Element);
    }

    private bool TrySendKeysCore(WindowInfo window, WindowControlInfo control, IReadOnlyList<VirtualKey> keys, bool ensureForegroundWindow) {
        UiAutomationElementMatchResult match = ResolveMatchingElement(window.Handle, control);
        object? element = match.Element;
        if (element == null) {
            return false;
        }

        TryPatternAction(element, "System.Windows.Automation.ScrollItemPattern", "ScrollIntoView");
        if (!TrySetFocus(element)) {
            return false;
        }

        if (ensureForegroundWindow && !WindowActivationService.TryPrepareWindowForAutomation(window.Handle)) {
            return false;
        }

        if (MonitorNativeMethods.GetForegroundWindow() != window.Handle) {
            return false;
        }

        KeyboardInputService.SendToForeground(keys.ToArray());
        WaitWithCurrentUiMessagePump(ForegroundInputSettleMilliseconds);
        return true;
    }

    private bool TryFocusCore(WindowInfo window, WindowControlInfo control, bool ensureForegroundWindow) {
        UiAutomationElementMatchResult match = ResolveMatchingElement(window.Handle, control);
        object? element = match.Element;
        if (element == null) {
            return false;
        }

        TryPatternAction(element, "System.Windows.Automation.ScrollItemPattern", "ScrollIntoView");
        if (!TrySetFocus(element)) {
            return false;
        }

        if (ensureForegroundWindow && !WindowActivationService.TryPrepareWindowForAutomation(window.Handle)) {
            return false;
        }

        return !ensureForegroundWindow || MonitorNativeMethods.GetForegroundWindow() == window.Handle;
    }

    private DesktopUiAutomationActionDiagnostic ProbeActionResolutionCore(WindowInfo window, WindowControlInfo control) {
        UiAutomationElementMatchResult match = ResolveMatchingElement(window.Handle, control);
        return new DesktopUiAutomationActionDiagnostic {
            Attempted = true,
            Resolved = match.Element != null,
            UsedCachedActionMatch = match.UsedCachedActionMatch,
            UsedPreferredRoot = match.UsedPreferredRoot,
            RootHandle = match.RootHandle,
            Score = match.Score,
            SearchMode = match.SearchMode
        };
    }

    private UiAutomationElementMatchResult ResolveMatchingElement(IntPtr windowHandle, WindowControlInfo control) {
        var enumerator = new ControlEnumerator();
        List<WindowControlInfo> win32Controls = enumerator.EnumerateControls(windowHandle);
        IReadOnlyList<IntPtr> fallbackRootHandles = GetFallbackRootHandles(windowHandle, win32Controls);
        IntPtr preferredRootHandle = GetPreferredSearchRootHandle(windowHandle, fallbackRootHandles);
        IReadOnlyList<IntPtr> searchRootHandles = GetSearchRootHandles(windowHandle, win32Controls);
        string actionMatchCacheKey = GetActionMatchCacheKey(windowHandle, control);

        if (TryGetCachedActionMatch(actionMatchCacheKey, out CachedActionMatch? cachedMatch) && cachedMatch != null) {
            object? cachedElement = TryFindExactElementInRoot(
                cachedMatch.RootHandle,
                cachedMatch.RootHandle != windowHandle,
                cachedMatch.Control);
            if (cachedElement != null) {
                RememberPreferredSearchRootHandle(windowHandle, cachedMatch.RootHandle);
                return new UiAutomationElementMatchResult {
                    Element = cachedElement,
                    UsedCachedActionMatch = true,
                    UsedPreferredRoot = cachedMatch.RootHandle == preferredRootHandle || cachedMatch.RootHandle != windowHandle,
                    RootHandle = cachedMatch.RootHandle,
                    Score = ScoreMatch(control, cachedMatch.Control),
                    SearchMode = "CachedExactMatch"
                };
            }

            ActionMatchCache.TryRemove(actionMatchCacheKey, out _);
        }

        object? bestMatch = null;
        int bestScore = 0;
        IntPtr bestRootHandle = IntPtr.Zero;
        WindowControlInfo? bestControlInfo = null;
        if (preferredRootHandle != IntPtr.Zero && preferredRootHandle != windowHandle) {
            FindBestMatchInRoot(preferredRootHandle, includeRoot: true, control, ref bestMatch, ref bestScore, ref bestRootHandle, ref bestControlInfo);
            if (bestScore > 0) {
                RememberPreferredSearchRootHandle(windowHandle, preferredRootHandle);
                CacheActionMatch(actionMatchCacheKey, bestRootHandle, bestControlInfo);
                return new UiAutomationElementMatchResult {
                    Element = bestMatch,
                    UsedCachedActionMatch = false,
                    UsedPreferredRoot = true,
                    RootHandle = bestRootHandle,
                    Score = bestScore,
                    SearchMode = "PreferredRootSearch"
                };
            }

            ForgetPreferredSearchRootHandle(windowHandle, preferredRootHandle);
        }

        for (int rootIndex = 0; rootIndex < searchRootHandles.Count; rootIndex++) {
            IntPtr rootHandle = searchRootHandles[rootIndex];
            if (rootHandle == preferredRootHandle && rootHandle != windowHandle) {
                continue;
            }

            bool includeRoot = rootHandle != windowHandle;
            FindBestMatchInRoot(rootHandle, includeRoot, control, ref bestMatch, ref bestScore, ref bestRootHandle, ref bestControlInfo);
            if (bestScore > 0 && rootHandle != windowHandle) {
                RememberPreferredSearchRootHandle(windowHandle, rootHandle);
            }
        }

        CacheActionMatch(actionMatchCacheKey, bestRootHandle, bestControlInfo);
        return new UiAutomationElementMatchResult {
            Element = bestScore > 0 ? bestMatch : null,
            UsedCachedActionMatch = false,
            UsedPreferredRoot = bestRootHandle != IntPtr.Zero && bestRootHandle == preferredRootHandle && preferredRootHandle != windowHandle,
            RootHandle = bestRootHandle,
            Score = bestScore,
            SearchMode = bestScore > 0 ? "FullRootSearch" : "NotFound"
        };
    }

    internal static IReadOnlyList<IntPtr> GetSearchRootHandles(IntPtr windowHandle, IEnumerable<WindowControlInfo>? win32Controls) {
        if (windowHandle == IntPtr.Zero) {
            return Array.Empty<IntPtr>();
        }

        IReadOnlyList<IntPtr> fallbackRootHandles = GetFallbackRootHandles(windowHandle, win32Controls);
        IntPtr preferredRootHandle = GetPreferredSearchRootHandle(windowHandle, fallbackRootHandles);

        var handles = new List<IntPtr>();
        if (preferredRootHandle != IntPtr.Zero && preferredRootHandle != windowHandle) {
            handles.Add(preferredRootHandle);
        }

        handles.Add(windowHandle);
        foreach (IntPtr fallbackRootHandle in OrderFallbackRootHandles(fallbackRootHandles, preferredRootHandle)) {
            if (!handles.Contains(fallbackRootHandle)) {
                handles.Add(fallbackRootHandle);
            }
        }

        return handles;
    }

    private IEnumerable<object> EnumerateElementsForRoot(IntPtr rootHandle, bool includeRoot) {
        if (!TryResolveRootElement(rootHandle, out object? rootElement) || rootElement == null) {
            yield break;
        }

        object? treeScope = Enum.Parse(_treeScopeType!, includeRoot ? "Subtree" : "Descendants", ignoreCase: false);
        object? trueCondition = _conditionType!.GetField("TrueCondition", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        if (treeScope == null || trueCondition == null) {
            yield break;
        }

        object? collection = _automationElementType!.GetMethod("FindAll", new[] { _treeScopeType!, _conditionType! })?
            .Invoke(rootElement, new[] { treeScope, trueCondition });
        if (collection == null) {
            yield break;
        }

        PropertyInfo? countProperty = _automationElementCollectionType!.GetProperty("Count");
        PropertyInfo? itemProperty = _automationElementCollectionType.GetProperty("Item");
        if (countProperty == null || itemProperty == null) {
            yield break;
        }

        int count = (int)(countProperty.GetValue(collection) ?? 0);
        for (int index = 0; index < count; index++) {
            object? candidate = itemProperty.GetValue(collection, new object[] { index });
            if (candidate != null) {
                yield return candidate;
            }
        }
    }

    internal static int ScoreMatch(WindowControlInfo expected, WindowControlInfo candidate) {
        int score = 0;
        if (expected.Handle != IntPtr.Zero && candidate.Handle == expected.Handle) {
            score += 100;
        }

        if (!string.IsNullOrWhiteSpace(expected.AutomationId) &&
            string.Equals(expected.AutomationId, candidate.AutomationId, StringComparison.OrdinalIgnoreCase)) {
            score += 40;
        }

        if (!string.IsNullOrWhiteSpace(expected.ControlType) &&
            string.Equals(expected.ControlType, candidate.ControlType, StringComparison.OrdinalIgnoreCase)) {
            score += 20;
        }

        if (!string.IsNullOrWhiteSpace(expected.ClassName) &&
            string.Equals(expected.ClassName, candidate.ClassName, StringComparison.OrdinalIgnoreCase)) {
            score += 10;
        }

        if (!string.IsNullOrWhiteSpace(expected.Text) &&
            string.Equals(expected.Text, candidate.Text, StringComparison.OrdinalIgnoreCase)) {
            score += 10;
        }

        if (!string.IsNullOrWhiteSpace(expected.FrameworkId) &&
            string.Equals(expected.FrameworkId, candidate.FrameworkId, StringComparison.OrdinalIgnoreCase)) {
            score += 8;
        }

        if (!string.IsNullOrWhiteSpace(expected.Value) &&
            string.Equals(expected.Value, candidate.Value, StringComparison.OrdinalIgnoreCase)) {
            score += 8;
        }

        if (expected.IsEnabled.HasValue && candidate.IsEnabled.HasValue && expected.IsEnabled.Value == candidate.IsEnabled.Value) {
            score += 4;
        }

        if (expected.IsKeyboardFocusable.HasValue &&
            candidate.IsKeyboardFocusable.HasValue &&
            expected.IsKeyboardFocusable.Value == candidate.IsKeyboardFocusable.Value) {
            score += 4;
        }

        if (expected.IsOffscreen.HasValue && candidate.IsOffscreen.HasValue && expected.IsOffscreen.Value == candidate.IsOffscreen.Value) {
            score += 4;
        }

        score += ScoreBoundsMatch(expected, candidate);

        return score;
    }

    private void FindBestMatchInRoot(IntPtr rootHandle, bool includeRoot, WindowControlInfo expected, ref object? bestMatch, ref int bestScore, ref IntPtr bestRootHandle, ref WindowControlInfo? bestControlInfo) {
        foreach (object candidate in EnumerateElementsForRoot(rootHandle, includeRoot)) {
            WindowControlInfo? candidateInfo = CreateControlInfo(candidate);
            if (candidateInfo == null) {
                continue;
            }

            int score = ScoreMatch(expected, candidateInfo);
            if (score > bestScore) {
                bestScore = score;
                bestMatch = candidate;
                bestRootHandle = rootHandle;
                bestControlInfo = CloneControl(candidateInfo);
            }
        }
    }

    private static IReadOnlyList<IntPtr> OrderFallbackRootHandles(IReadOnlyList<IntPtr>? fallbackRootHandles, IntPtr preferredRootHandle) {
        if (fallbackRootHandles == null || fallbackRootHandles.Count == 0) {
            return Array.Empty<IntPtr>();
        }

        if (preferredRootHandle == IntPtr.Zero || !fallbackRootHandles.Contains(preferredRootHandle)) {
            return fallbackRootHandles;
        }

        var ordered = new List<IntPtr>(fallbackRootHandles.Count) {
            preferredRootHandle
        };
        foreach (IntPtr fallbackRootHandle in fallbackRootHandles) {
            if (fallbackRootHandle != preferredRootHandle) {
                ordered.Add(fallbackRootHandle);
            }
        }

        return ordered;
    }

    private static bool TryGetCachedEnumeratedControls(IntPtr rootHandle, bool includeRoot, out List<WindowControlInfo> controls) {
        string cacheKey = GetEnumeratedControlsCacheKey(rootHandle, includeRoot);
        if (EnumeratedControlsCache.TryGetValue(cacheKey, out CachedControlCollection? cached) &&
            DateTime.UtcNow <= cached.ExpiresAtUtc) {
            controls = CloneControls(cached.Controls);
            return true;
        }

        if (cached != null) {
            EnumeratedControlsCache.TryRemove(cacheKey, out _);
        }

        controls = new List<WindowControlInfo>();
        return false;
    }

    private static void CacheEnumeratedControls(IntPtr rootHandle, bool includeRoot, List<WindowControlInfo> controls) {
        string cacheKey = GetEnumeratedControlsCacheKey(rootHandle, includeRoot);
        EnumeratedControlsCache[cacheKey] = new CachedControlCollection {
            ExpiresAtUtc = DateTime.UtcNow.AddMilliseconds(EnumeratedControlsCacheMilliseconds),
            Controls = CloneControls(controls).ToArray()
        };
        TrimExpiringCache(EnumeratedControlsCache, EnumeratedControlsCacheMaximumCount, cached => cached.ExpiresAtUtc);
    }

    private static string GetEnumeratedControlsCacheKey(IntPtr rootHandle, bool includeRoot) {
        return $"{rootHandle.ToInt64():X}:{(includeRoot ? 1 : 0)}";
    }

    internal static string GetActionMatchCacheKey(IntPtr windowHandle, WindowControlInfo control) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        return string.Join("|", new[] {
            windowHandle.ToInt64().ToString("X"),
            control.Handle.ToInt64().ToString("X"),
            control.RuntimeId ?? string.Empty,
            control.AutomationId ?? string.Empty,
            control.ControlType ?? string.Empty,
            control.ClassName ?? string.Empty,
            control.Text ?? string.Empty,
            control.Value ?? string.Empty,
            control.FrameworkId ?? string.Empty
        });
    }

    private static List<WindowControlInfo> CloneControls(IEnumerable<WindowControlInfo> controls) {
        return controls.Select(CloneControl).ToList();
    }

    private object? TryFindExactElementInRoot(IntPtr rootHandle, bool includeRoot, WindowControlInfo expected) {
        if (!TryResolveRootElement(rootHandle, out object? rootElement) || rootElement == null) {
            return null;
        }

        object? treeScope = Enum.Parse(_treeScopeType!, includeRoot ? "Subtree" : "Descendants", ignoreCase: false);
        if (treeScope == null) {
            return null;
        }

        foreach ((string PropertyName, string Value) term in GetFastSearchTerms(expected)) {
            object? condition = CreatePropertyCondition(term.PropertyName, term.Value);
            if (condition == null) {
                continue;
            }

            try {
                object? element = _automationElementType!.GetMethod("FindFirst", new[] { _treeScopeType!, _conditionType! })?
                    .Invoke(rootElement, new[] { treeScope, condition });
                if (element == null) {
                    continue;
                }

                WindowControlInfo? info = CreateControlInfo(element);
                if (info == null) {
                    continue;
                }

                if (IsStrongCachedMatch(expected, info)) {
                    return element;
                }
            } catch {
                // Fall through to the broader root search when fast exact lookup fails.
            }
        }

        return null;
    }

    internal static bool IsStrongCachedMatch(WindowControlInfo expected, WindowControlInfo candidate) {
        if (!string.IsNullOrWhiteSpace(expected.RuntimeId) || !string.IsNullOrWhiteSpace(candidate.RuntimeId)) {
            return !string.IsNullOrWhiteSpace(expected.RuntimeId) &&
                string.Equals(expected.RuntimeId, candidate.RuntimeId, StringComparison.Ordinal);
        }

        if (expected.Handle != IntPtr.Zero || candidate.Handle != IntPtr.Zero) {
            return expected.Handle != IntPtr.Zero && expected.Handle == candidate.Handle;
        }

        bool hasExpectedBounds = expected.Width > 0 && expected.Height > 0;
        bool hasCandidateBounds = candidate.Width > 0 && candidate.Height > 0;
        if (hasExpectedBounds || hasCandidateBounds) {
            if (!hasExpectedBounds || !hasCandidateBounds || ScoreBoundsMatch(expected, candidate) < 16) {
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(expected.ControlType) &&
            !string.Equals(expected.ControlType, candidate.ControlType, StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(expected.ClassName) &&
            !string.Equals(expected.ClassName, candidate.ClassName, StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(expected.AutomationId)) {
            return string.Equals(expected.AutomationId, candidate.AutomationId, StringComparison.OrdinalIgnoreCase);
        }

        return WindowManager.AreEquivalentControls(expected, candidate);
    }

    private IEnumerable<(string PropertyName, string Value)> GetFastSearchTerms(WindowControlInfo control) {
        if (!string.IsNullOrWhiteSpace(control.AutomationId)) {
            yield return ("AutomationIdProperty", control.AutomationId);
        }

        if (!string.IsNullOrWhiteSpace(control.Text)) {
            yield return ("NameProperty", control.Text);
        }

        if (!string.IsNullOrWhiteSpace(control.ClassName)) {
            yield return ("ClassNameProperty", control.ClassName);
        }
    }

    private object? CreatePropertyCondition(string propertyFieldName, object? value) {
        if (string.IsNullOrWhiteSpace(propertyFieldName) || value == null) {
            return null;
        }

        if (value is string text && string.IsNullOrWhiteSpace(text)) {
            return null;
        }

        Type? propertyConditionType = _automationClientAssembly?.GetType("System.Windows.Automation.PropertyCondition", throwOnError: false);
        if (propertyConditionType == null) {
            return null;
        }

        object? property = _automationElementType?.GetField(propertyFieldName, BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        if (property == null) {
            return null;
        }

        ConstructorInfo? constructor = propertyConditionType.GetConstructor(new[] { property.GetType(), typeof(object) });
        if (constructor == null) {
            return null;
        }

        return constructor.Invoke(new object[] { property, value });
    }

    private static bool TryGetCachedActionMatch(string cacheKey, out CachedActionMatch? cachedMatch) {
        if (ActionMatchCache.TryGetValue(cacheKey, out cachedMatch) && DateTime.UtcNow <= cachedMatch.ExpiresAtUtc) {
            return true;
        }

        if (cachedMatch != null) {
            ActionMatchCache.TryRemove(cacheKey, out _);
        }

        cachedMatch = null;
        return false;
    }

    private static void CacheActionMatch(string cacheKey, IntPtr rootHandle, WindowControlInfo? control) {
        if (string.IsNullOrWhiteSpace(cacheKey) || rootHandle == IntPtr.Zero || control == null) {
            return;
        }

        ActionMatchCache[cacheKey] = new CachedActionMatch {
            ExpiresAtUtc = DateTime.UtcNow.AddMilliseconds(ActionMatchCacheMilliseconds),
            RootHandle = rootHandle,
            Control = CloneControl(control)
        };
        TrimExpiringCache(ActionMatchCache, ActionMatchCacheMaximumCount, cached => cached.ExpiresAtUtc);
    }

    internal static int PreferredSearchRootCacheCount => PreferredSearchRoots.Count;

    private static void TrimPreferredSearchRoots() {
        int removeCount = PreferredSearchRoots.Count - PreferredSearchRootsMaximumCount;
        if (removeCount <= 0) {
            return;
        }

        foreach (IntPtr key in PreferredSearchRoots.Keys.Take(removeCount)) {
            PreferredSearchRoots.TryRemove(key, out _);
        }
    }

    private static void TrimExpiringCache<T>(ConcurrentDictionary<string, T> cache, int maximumCount, Func<T, DateTime> getExpiry) {
        DateTime now = DateTime.UtcNow;
        foreach (KeyValuePair<string, T> entry in cache) {
            if (getExpiry(entry.Value) < now) {
                cache.TryRemove(entry.Key, out _);
            }
        }

        int removeCount = cache.Count - maximumCount;
        if (removeCount <= 0) {
            return;
        }

        foreach (string key in cache
            .OrderBy(entry => getExpiry(entry.Value))
            .Take(removeCount)
            .Select(entry => entry.Key)) {
            cache.TryRemove(key, out _);
        }
    }

    private static WindowControlInfo CloneControl(WindowControlInfo control) {
        return new WindowControlInfo {
            ParentWindowHandle = control.ParentWindowHandle,
            Handle = control.Handle,
            ClassName = control.ClassName,
            Id = control.Id,
            Text = control.Text,
            Value = control.Value,
            Source = control.Source,
            HasUiAutomationIdentity = control.HasUiAutomationIdentity,
            RuntimeId = control.RuntimeId,
            AutomationId = control.AutomationId,
            ControlType = control.ControlType,
            FrameworkId = control.FrameworkId,
            IsKeyboardFocusable = control.IsKeyboardFocusable,
            IsEnabled = control.IsEnabled,
            IsPassword = control.IsPassword,
            SupportsBackgroundClick = control.SupportsBackgroundClick,
            SupportsBackgroundText = control.SupportsBackgroundText,
            SupportsBackgroundKeys = control.SupportsBackgroundKeys,
            SupportsForegroundInputFallback = control.SupportsForegroundInputFallback,
            Left = control.Left,
            Top = control.Top,
            Width = control.Width,
            Height = control.Height,
            IsOffscreen = control.IsOffscreen
        };
    }

    private sealed class CachedControlCollection {
        public DateTime ExpiresAtUtc { get; set; }
        public WindowControlInfo[] Controls { get; set; } = Array.Empty<WindowControlInfo>();
    }

    private sealed class CachedActionMatch {
        public DateTime ExpiresAtUtc { get; set; }
        public IntPtr RootHandle { get; set; }
        public WindowControlInfo Control { get; set; } = new();
    }

    private sealed class UiAutomationElementMatchResult {
        public object? Element { get; set; }
        public bool UsedCachedActionMatch { get; set; }
        public bool UsedPreferredRoot { get; set; }
        public IntPtr RootHandle { get; set; }
        public int Score { get; set; }
        public string SearchMode { get; set; } = string.Empty;
    }

    private bool TryPatternAction(object element, string patternTypeName, string methodName, params object[] parameters) {
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

        try {
            method.Invoke(pattern, parameters);
            return true;
        } catch {
            return false;
        }
    }

    private static bool TrySetFocus(object element) {
        MethodInfo? method = element.GetType().GetMethod("SetFocus", Type.EmptyTypes);
        if (method == null) {
            return false;
        }

        try {
            method.Invoke(element, null);
            return true;
        } catch {
            return false;
        }
    }

    private static object? GetCurrentPattern(object element, Type patternType) {
        object? patternIdentifier = patternType.GetField("Pattern", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        if (patternIdentifier == null) {
            return null;
        }

        try {
            return element.GetType().GetMethod("GetCurrentPattern", new[] { patternIdentifier.GetType() })?
                .Invoke(element, new[] { patternIdentifier });
        } catch {
            return null;
        }
    }

    private static Assembly? TryLoadAssembly(string name) {
        try {
            return Assembly.Load(name);
        } catch {
            try {
                string frameworkName = $"{name}, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
                return Assembly.Load(frameworkName);
            } catch {
                return null;
            }
        }
    }

    private bool TryResolveRootElement(IntPtr rootHandle, out object? rootElement) {
        try {
            rootElement = _automationElementType!.GetMethod("FromHandle", BindingFlags.Public | BindingFlags.Static)?
                .Invoke(null, new object[] { rootHandle });
            if (rootElement != null) {
                return true;
            }
        } catch {
        }

        return TryResolveRootElementFromDesktopSearch(rootHandle, out rootElement);
    }

    private bool TryResolveRootElementFromDesktopSearch(IntPtr rootHandle, out object? rootElement) {
        rootElement = null;
        object? desktopRoot = TryGetDesktopRootElement();
        if (desktopRoot == null) {
            return false;
        }

        if (TryFindElementByProperty(desktopRoot, "NativeWindowHandleProperty", ToNativeHandlePropertyValue(rootHandle), out rootElement) && rootElement != null) {
            return true;
        }

        if (!MonitorNativeMethods.GetWindowRect(rootHandle, out RECT windowRect)) {
            return false;
        }

        MonitorNativeMethods.GetWindowThreadProcessId(rootHandle, out uint processId);
        if (processId == 0 || processId > int.MaxValue) {
            return false;
        }

        object? processCondition = CreatePropertyCondition("ProcessIdProperty", (int)processId);
        object? treeScope = Enum.Parse(_treeScopeType!, "Descendants", ignoreCase: false);
        if (processCondition == null || treeScope == null) {
            return false;
        }

        object? collection = _automationElementType!.GetMethod("FindAll", new[] { _treeScopeType!, _conditionType! })?
            .Invoke(desktopRoot, new[] { treeScope, processCondition });
        if (collection == null) {
            return false;
        }

        PropertyInfo? countProperty = _automationElementCollectionType!.GetProperty("Count");
        PropertyInfo? itemProperty = _automationElementCollectionType.GetProperty("Item");
        if (countProperty == null || itemProperty == null) {
            return false;
        }

        int count = (int)(countProperty.GetValue(collection) ?? 0);
        int bestScore = 0;
        object? bestElement = null;
        for (int index = 0; index < count; index++) {
            object? candidate = itemProperty.GetValue(collection, new object[] { index });
            if (candidate == null) {
                continue;
            }

            WindowControlInfo? info;
            try {
                info = CreateControlInfo(candidate);
            } catch {
                continue;
            }

            if (info == null) {
                continue;
            }

            int score = ScoreDesktopSearchRootCandidate(windowRect, info);
            if (score > bestScore) {
                bestScore = score;
                bestElement = candidate;
            }
        }

        rootElement = bestElement;
        return rootElement != null;
    }

    private object? TryGetDesktopRootElement() {
        return _automationElementType?.GetProperty("RootElement", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
    }

    private bool TryFindElementByProperty(object rootElement, string propertyFieldName, object? value, out object? element) {
        element = null;
        if (rootElement == null || value == null) {
            return false;
        }

        object? condition = CreatePropertyCondition(propertyFieldName, value);
        object? treeScope = Enum.Parse(_treeScopeType!, "Descendants", ignoreCase: false);
        if (condition == null || treeScope == null) {
            return false;
        }

        try {
            element = _automationElementType!.GetMethod("FindFirst", new[] { _treeScopeType!, _conditionType! })?
                .Invoke(rootElement, new[] { treeScope, condition });
            return element != null;
        } catch {
            element = null;
            return false;
        }
    }

    internal static int ScoreDesktopSearchRootCandidate(RECT windowRect, WindowControlInfo candidate) {
        if (candidate == null) {
            return 0;
        }

        if (candidate.IsOffscreen == true || candidate.Width <= 0 || candidate.Height <= 0) {
            return 0;
        }

        int candidateRight = candidate.Left + candidate.Width;
        int candidateBottom = candidate.Top + candidate.Height;
        int overlapWidth = Math.Min(windowRect.Right, candidateRight) - Math.Max(windowRect.Left, candidate.Left);
        int overlapHeight = Math.Min(windowRect.Bottom, candidateBottom) - Math.Max(windowRect.Top, candidate.Top);
        if (overlapWidth <= 0 || overlapHeight <= 0) {
            return 0;
        }

        long overlapArea = (long)overlapWidth * overlapHeight;
        long windowArea = Math.Max(1L, (long)(windowRect.Right - windowRect.Left) * Math.Max(1, windowRect.Bottom - windowRect.Top));
        long candidateArea = Math.Max(1L, (long)candidate.Width * candidate.Height);
        int windowCoverageScore = (int)Math.Min(1000L, (overlapArea * 1000L) / windowArea);
        int candidateCoverageScore = (int)Math.Min(1000L, (overlapArea * 1000L) / candidateArea);
        int score = (windowCoverageScore * 8) + candidateCoverageScore;

        int windowCenterX = windowRect.Left + ((windowRect.Right - windowRect.Left) / 2);
        int windowCenterY = windowRect.Top + ((windowRect.Bottom - windowRect.Top) / 2);
        int candidateCenterX = candidate.Left + (candidate.Width / 2);
        int candidateCenterY = candidate.Top + (candidate.Height / 2);
        int centerDistance = Math.Abs(windowCenterX - candidateCenterX) + Math.Abs(windowCenterY - candidateCenterY);
        if (centerDistance == 0) {
            score += 250;
        } else if (centerDistance <= 24) {
            score += 180;
        } else if (centerDistance <= 96) {
            score += 80;
        }

        if (windowCoverageScore >= 950) {
            score += 400;
        }

        if (string.Equals(candidate.ControlType, "Window", StringComparison.OrdinalIgnoreCase)) {
            score += 250;
        } else if (string.Equals(candidate.ControlType, "Pane", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(candidate.ControlType, "Custom", StringComparison.OrdinalIgnoreCase)) {
            score += 120;
        }

        if (candidate.Handle != IntPtr.Zero) {
            score += 30;
        }

        return score;
    }

    private static object? ToNativeHandlePropertyValue(IntPtr handle) {
        long raw = handle.ToInt64();
        if (raw > int.MaxValue || raw < int.MinValue) {
            return null;
        }

        return unchecked((int)raw);
    }

    private static string ReadWindowClassName(IntPtr handle) {
        if (handle == IntPtr.Zero) {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder(256);
        return MonitorNativeMethods.GetClassName(handle, builder, builder.Capacity) > 0
            ? builder.ToString()
            : string.Empty;
    }

    private static bool ContainsEquivalentControl(List<WindowControlInfo> controls, WindowControlInfo candidate) {
        return controls.Any(existing => WindowManager.AreEquivalentControls(existing, candidate));
    }

    private WindowControlInfo? CreateControlInfo(object element, bool readValue = true) {
        object? current = element.GetType().GetProperty("Current", BindingFlags.Public | BindingFlags.Instance)?.GetValue(element);
        if (current == null) {
            return null;
        }

        bool? isPassword = ReadNullableBoolean(current, "IsPassword");
        bool canAccessText = isPassword == false;
        string name = canAccessText ? ReadString(current, "Name") : string.Empty;
        string className = ReadString(current, "ClassName");
        string automationId = ReadString(current, "AutomationId");
        string frameworkId = ReadString(current, "FrameworkId");
        int nativeWindowHandle = ReadInt32(current, "NativeWindowHandle");
        bool? isKeyboardFocusable = ReadNullableBoolean(current, "IsKeyboardFocusable");
        bool? isEnabled = ReadNullableBoolean(current, "IsEnabled");
        bool? isOffscreen = ReadNullableBoolean(current, "IsOffscreen");
        string controlType = ReadControlTypeName(current);
        string value = canAccessText && readValue ? ReadValue(element) : string.Empty;
        bool hasInvokeAction = HasPattern(element, "System.Windows.Automation.InvokePattern") ||
            HasPattern(element, "System.Windows.Automation.SelectionItemPattern") ||
            HasPattern(element, "System.Windows.Automation.ExpandCollapsePattern") ||
            HasPattern(element, "System.Windows.Automation.TogglePattern") ||
            HasPattern(element, "System.Windows.Automation.LegacyIAccessiblePattern");
        bool hasDirectTextAction = HasPattern(element, "System.Windows.Automation.ValuePattern") ||
            HasPattern(element, "System.Windows.Automation.LegacyIAccessiblePattern");
        bool hasNativeHandle = nativeWindowHandle != 0;
        (int left, int top, int width, int height) = ReadBounds(current);

        return new WindowControlInfo {
            Handle = nativeWindowHandle == 0 ? IntPtr.Zero : new IntPtr(nativeWindowHandle),
            ClassName = className,
            Id = 0,
            Text = name,
            Value = value,
            Source = WindowControlSource.UiAutomation,
            HasUiAutomationIdentity = true,
            RuntimeId = ReadRuntimeId(element, errors: null),
            AutomationId = automationId,
            ControlType = controlType,
            FrameworkId = frameworkId,
            IsKeyboardFocusable = isKeyboardFocusable,
            IsEnabled = isEnabled,
            IsPassword = isPassword,
            SupportsBackgroundClick = hasNativeHandle || hasInvokeAction,
            SupportsBackgroundText = canAccessText && (hasNativeHandle || hasDirectTextAction),
            SupportsBackgroundKeys = hasNativeHandle,
            SupportsForegroundInputFallback = SupportsForegroundFallback(hasNativeHandle, isKeyboardFocusable, isEnabled, controlType, className),
            Left = left,
            Top = top,
            Width = width,
            Height = height,
            IsOffscreen = isOffscreen
        };
    }

    private string ReadValue(object element) {
        return ReadPatternValue(element, "System.Windows.Automation.ValuePattern") ??
            ReadPatternValue(element, "System.Windows.Automation.RangeValuePattern") ??
            ReadPatternValue(element, "System.Windows.Automation.LegacyIAccessiblePattern") ??
            string.Empty;
    }

    private bool TryReplaceFocusedTextWithPaste(WindowInfo window, WindowControlInfo control, string value) {
        string clipboardBackup = string.Empty;
        bool restoreClipboard = false;

        try {
            restoreClipboard = ClipboardHelper.TryGetText(out clipboardBackup);
            ClipboardHelper.SetText(value);
        } catch {
            return false;
        }

        try {
            KeyboardInputService.SendToForeground(VirtualKey.VK_CONTROL, VirtualKey.VK_A);
            WaitWithCurrentUiMessagePump(ForegroundInputSettleMilliseconds);
            KeyboardInputService.SendToForeground(VirtualKey.VK_CONTROL, VirtualKey.VK_V);
            return WaitForResolvedValue(window, control, value);
        } finally {
            if (restoreClipboard) {
                try {
                    ClipboardHelper.SetText(clipboardBackup);
                } catch {
                    // Preserve the successful input result even if clipboard restoration fails.
                }
            }
        }
    }

    private bool WaitForResolvedValue(WindowInfo window, WindowControlInfo control, string expectedValue) {
        DateTime deadlineUtc = DateTime.UtcNow.AddMilliseconds(ForegroundTextVerificationMilliseconds);
        while (DateTime.UtcNow <= deadlineUtc) {
            string? currentValue = TryReadResolvedValue(window, control);
            if (currentValue != null && string.Equals(currentValue, expectedValue, StringComparison.Ordinal)) {
                control.Value = expectedValue;
                if (string.IsNullOrWhiteSpace(control.Text) || IsLikelyEditableControl(control.ControlType, control.ClassName)) {
                    control.Text = expectedValue;
                }

                return true;
            }

            WaitWithCurrentUiMessagePump(ForegroundTextVerificationIntervalMilliseconds);
        }

        return false;
    }

    private string? TryReadResolvedValue(WindowInfo window, WindowControlInfo control) {
        UiAutomationElementMatchResult refreshedMatch = ResolveMatchingElement(window.Handle, control);
        if (refreshedMatch.Element == null) {
            return null;
        }

        try {
            return ReadValue(refreshedMatch.Element);
        } catch {
            return null;
        }
    }

    private bool WaitForResolvedCheckState(WindowInfo window, WindowControlInfo control, bool expectedState) {
        DateTime deadlineUtc = DateTime.UtcNow.AddMilliseconds(ForegroundTextVerificationMilliseconds);
        while (DateTime.UtcNow <= deadlineUtc) {
            bool? currentState = TryReadResolvedCheckState(window, control);
            if (currentState.HasValue && currentState.Value == expectedState) {
                return true;
            }

            WaitWithCurrentUiMessagePump(ForegroundTextVerificationIntervalMilliseconds);
        }

        return false;
    }

    private bool WaitForResolvedSelectedValue(WindowInfo window, WindowControlInfo control, string expectedValue) {
        DateTime deadlineUtc = DateTime.UtcNow.AddMilliseconds(ForegroundTextVerificationMilliseconds);
        while (DateTime.UtcNow <= deadlineUtc) {
            string? currentValue = TryReadResolvedSelectedValue(window, control);
            if (!string.IsNullOrWhiteSpace(currentValue) && string.Equals(currentValue, expectedValue, StringComparison.OrdinalIgnoreCase)) {
                control.Value = expectedValue;
                if (string.IsNullOrWhiteSpace(control.Text) || string.Equals(control.ControlType, "ComboBox", StringComparison.OrdinalIgnoreCase)) {
                    control.Text = expectedValue;
                }

                return true;
            }

            WaitWithCurrentUiMessagePump(ForegroundTextVerificationIntervalMilliseconds);
        }

        return false;
    }

    private bool? TryReadResolvedCheckState(WindowInfo window, WindowControlInfo control) {
        UiAutomationElementMatchResult refreshedMatch = ResolveMatchingElement(window.Handle, control);
        if (refreshedMatch.Element == null) {
            return null;
        }

        try {
            return ReadCheckState(refreshedMatch.Element);
        } catch {
            return null;
        }
    }

    private string? TryReadResolvedSelectedValue(WindowInfo window, WindowControlInfo control) {
        UiAutomationElementMatchResult refreshedMatch = ResolveMatchingElement(window.Handle, control);
        if (refreshedMatch.Element == null) {
            return null;
        }

        try {
            return ReadSelectedValue(refreshedMatch.Element);
        } catch {
            return null;
        }
    }

    private bool HasPattern(object element, string patternTypeName) {
        try {
            Type? patternType = _automationClientAssembly?.GetType(patternTypeName, throwOnError: false);
            if (patternType == null) {
                return false;
            }

            return GetCurrentPattern(element, patternType) != null;
        } catch {
            return false;
        }
    }

    private string? ReadPatternValue(object element, string patternTypeName) {
        try {
            Type? patternType = _automationClientAssembly?.GetType(patternTypeName, throwOnError: false);
            if (patternType == null) {
                return null;
            }

            object? pattern = GetCurrentPattern(element, patternType);
            if (pattern == null) {
                return null;
            }

            object? current = pattern.GetType().GetProperty("Current", BindingFlags.Public | BindingFlags.Instance)?.GetValue(pattern);
            if (current == null) {
                return null;
            }

            PropertyInfo? valueProperty = current.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
            if (valueProperty == null) {
                return null;
            }

            object? value = valueProperty.GetValue(current);
            return value?.ToString();
        } catch {
            return null;
        }
    }

    private bool? ReadCheckState(object element) {
        try {
            Type? togglePatternType = _automationClientAssembly?.GetType("System.Windows.Automation.TogglePattern", throwOnError: false);
            if (togglePatternType == null) {
                return null;
            }

            object? pattern = GetCurrentPattern(element, togglePatternType);
            if (pattern == null) {
                return null;
            }

            object? current = pattern.GetType().GetProperty("Current", BindingFlags.Public | BindingFlags.Instance)?.GetValue(pattern);
            object? toggleState = current?.GetType().GetProperty("ToggleState", BindingFlags.Public | BindingFlags.Instance)?.GetValue(current);
            string? toggleStateName = toggleState?.ToString();
            if (string.IsNullOrWhiteSpace(toggleStateName)) {
                return null;
            }

            string toggleStateText = toggleStateName!;
            if (toggleStateText.EndsWith("On", StringComparison.OrdinalIgnoreCase)) {
                return true;
            }

            if (toggleStateText.EndsWith("Off", StringComparison.OrdinalIgnoreCase)) {
                return false;
            }

            return null;
        } catch {
            return null;
        }
    }

    private string? ReadSelectedValue(object element) {
        string? selectionValue = ReadSelectedItemValue(element);
        if (!string.IsNullOrWhiteSpace(selectionValue)) {
            return selectionValue;
        }

        string? directValue =
            ReadPatternValue(element, "System.Windows.Automation.ValuePattern") ??
            ReadPatternValue(element, "System.Windows.Automation.LegacyIAccessiblePattern");
        return string.IsNullOrWhiteSpace(directValue)
            ? null
            : directValue;
    }

    private string? ReadSelectedItemValue(object element) {
        try {
            Type? selectionPatternType = _automationClientAssembly?.GetType("System.Windows.Automation.SelectionPattern", throwOnError: false);
            if (selectionPatternType == null) {
                return null;
            }

            object? pattern = GetCurrentPattern(element, selectionPatternType);
            if (pattern == null) {
                return null;
            }

            MethodInfo? getSelectionMethod = pattern.GetType().GetMethod("GetSelection", Type.EmptyTypes);
            object? selection = getSelectionMethod?.Invoke(pattern, null);
            if (selection == null) {
                return null;
            }

            PropertyInfo? countProperty = _automationElementCollectionType?.GetProperty("Count");
            PropertyInfo? itemProperty = _automationElementCollectionType?.GetProperty("Item");
            if (countProperty == null || itemProperty == null) {
                return null;
            }

            int count = (int)(countProperty.GetValue(selection) ?? 0);
            if (count <= 0) {
                return null;
            }

            object? selectedElement = itemProperty.GetValue(selection, new object[] { 0 });
            if (selectedElement == null) {
                return null;
            }

            WindowControlInfo? selectedInfo = CreateControlInfo(selectedElement);
            if (selectedInfo == null) {
                return null;
            }

            return !string.IsNullOrWhiteSpace(selectedInfo.Value)
                ? selectedInfo.Value
                : !string.IsNullOrWhiteSpace(selectedInfo.Text)
                    ? selectedInfo.Text
                    : null;
        } catch {
            return null;
        }
    }

    private object? FindSelectionCandidateElement(object rootElement, string selectedValue) {
        return FindMatchingDescendantElement(rootElement, includeRoot: false, (candidateElement, info) =>
            !string.IsNullOrWhiteSpace(selectedValue) &&
            (string.Equals(info.Text, selectedValue, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(info.Value, selectedValue, StringComparison.OrdinalIgnoreCase)) &&
            (HasPattern(candidateElement, "System.Windows.Automation.SelectionItemPattern") ||
            HasPattern(candidateElement, "System.Windows.Automation.InvokePattern") ||
            HasPattern(candidateElement, "System.Windows.Automation.LegacyIAccessiblePattern")));
    }

    private object? FindMatchingDescendantElement(object rootElement, bool includeRoot, Func<object, WindowControlInfo, bool> predicate) {
        if (rootElement == null) {
            return null;
        }

        object? treeScope = Enum.Parse(_treeScopeType!, includeRoot ? "Subtree" : "Descendants", ignoreCase: false);
        object? trueCondition = _conditionType!.GetField("TrueCondition", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
        if (treeScope == null || trueCondition == null) {
            return null;
        }

        object? collection = _automationElementType!.GetMethod("FindAll", new[] { _treeScopeType!, _conditionType! })?
            .Invoke(rootElement, new[] { treeScope, trueCondition });
        if (collection == null) {
            return null;
        }

        PropertyInfo? countProperty = _automationElementCollectionType!.GetProperty("Count");
        PropertyInfo? itemProperty = _automationElementCollectionType.GetProperty("Item");
        if (countProperty == null || itemProperty == null) {
            return null;
        }

        int count = (int)(countProperty.GetValue(collection) ?? 0);
        for (int index = 0; index < count; index++) {
            object? candidateElement = itemProperty.GetValue(collection, new object[] { index });
            if (candidateElement == null) {
                continue;
            }

            try {
                WindowControlInfo? info = CreateControlInfo(candidateElement);
                if (info != null && predicate(candidateElement, info)) {
                    return candidateElement;
                }
            } catch {
                // Ignore unsupported descendants and keep searching for a structurally actionable candidate.
            }
        }

        return null;
    }

    private static string ReadString(object instance, string propertyName) {
        return instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance) as string ?? string.Empty;
    }

    internal static bool SupportsForegroundFallback(bool hasNativeHandle, bool? isKeyboardFocusable, bool? isEnabled, string controlType, string className) {
        if (hasNativeHandle) {
            return false;
        }

        if (isEnabled.HasValue && !isEnabled.Value) {
            return false;
        }

        if (isKeyboardFocusable.HasValue) {
            return isKeyboardFocusable.Value;
        }

        return IsLikelyEditableControl(controlType, className);
    }

    internal static bool IsLikelyEditableControl(string controlType, string className) {
        return string.Equals(controlType, "Edit", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(controlType, "Document", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(controlType, "ComboBox", StringComparison.OrdinalIgnoreCase) ||
            className.IndexOf("Edit", StringComparison.OrdinalIgnoreCase) >= 0 ||
            className.IndexOf("TextBox", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static int ReadInt32(object instance, string propertyName) {
        object? value = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance);
        return value is int intValue ? intValue : 0;
    }

    private static (int Left, int Top, int Width, int Height) ReadBounds(object instance) {
        object? value = instance.GetType().GetProperty("BoundingRectangle", BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance);
        if (value == null) {
            return default;
        }

        double left = ReadDouble(value, "Left");
        double top = ReadDouble(value, "Top");
        double width = ReadDouble(value, "Width");
        double height = ReadDouble(value, "Height");
        return (
            (int)Math.Round(left, MidpointRounding.AwayFromZero),
            (int)Math.Round(top, MidpointRounding.AwayFromZero),
            Math.Max(0, (int)Math.Round(width, MidpointRounding.AwayFromZero)),
            Math.Max(0, (int)Math.Round(height, MidpointRounding.AwayFromZero)));
    }

    private static int ScoreBoundsMatch(WindowControlInfo expected, WindowControlInfo candidate) {
        if (expected.Width <= 0 || expected.Height <= 0 || candidate.Width <= 0 || candidate.Height <= 0) {
            return 0;
        }

        int expectedCenterX = expected.Left + (expected.Width / 2);
        int expectedCenterY = expected.Top + (expected.Height / 2);
        int candidateCenterX = candidate.Left + (candidate.Width / 2);
        int candidateCenterY = candidate.Top + (candidate.Height / 2);
        int distance = Math.Abs(expectedCenterX - candidateCenterX) + Math.Abs(expectedCenterY - candidateCenterY);
        if (distance == 0) {
            return 20;
        }

        if (distance <= 4) {
            return 16;
        }

        if (distance <= 16) {
            return 12;
        }

        if (distance <= 48) {
            return 8;
        }

        if (distance <= 96) {
            return 4;
        }

        return 0;
    }

    private static bool? ReadNullableBoolean(object instance, string propertyName) {
        object? value = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance);
        return value is bool boolValue ? boolValue : null;
    }

    private static double ReadDouble(object instance, string propertyName) {
        object? value = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance);
        return value is double doubleValue ? doubleValue : 0;
    }

    private static string ReadControlTypeName(object instance) {
        object? controlType = instance.GetType().GetProperty("ControlType", BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance);
        if (controlType == null) {
            return string.Empty;
        }

        string? programmaticName = controlType.GetType().GetProperty("ProgrammaticName", BindingFlags.Public | BindingFlags.Instance)?.GetValue(controlType) as string;
        if (string.IsNullOrWhiteSpace(programmaticName)) {
            return controlType.ToString() ?? string.Empty;
        }

        string normalized = programmaticName ?? string.Empty;
        const string prefix = "ControlType.";
        return normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? normalized.Substring(prefix.Length)
            : normalized;
    }
}
