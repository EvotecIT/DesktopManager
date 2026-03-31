using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace DesktopManager;

internal sealed class UiAutomationControlService {
    private const int EnumeratedControlsCacheMilliseconds = 750;
    private const int ActionMatchCacheMilliseconds = 5000;
    private const int ForegroundTextVerificationMilliseconds = 1000;
    private const int ForegroundTextVerificationIntervalMilliseconds = 50;
    private const int ForegroundInputSettleMilliseconds = 75;
    private static readonly ConcurrentDictionary<IntPtr, IntPtr> PreferredSearchRoots = new();
    private static readonly ConcurrentDictionary<string, CachedControlCollection> EnumeratedControlsCache = new();
    private static readonly ConcurrentDictionary<string, CachedActionMatch> ActionMatchCache = new();
    private readonly Assembly? _automationClientAssembly;
    private readonly Assembly? _automationTypesAssembly;
    private readonly Type? _automationElementType;
    private readonly Type? _automationElementCollectionType;
    private readonly Type? _conditionType;
    private readonly Type? _treeScopeType;

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

        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA) {
            return EnumerateControlsCore(windowHandle, fallbackRootHandles);
        }

        List<WindowControlInfo> controls = new List<WindowControlInfo>();
        Exception? workerException = null;
        var thread = new Thread(() => {
            try {
                controls = new UiAutomationControlService().EnumerateControlsCore(windowHandle, fallbackRootHandles);
            } catch (Exception ex) {
                workerException = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (workerException != null) {
            throw workerException;
        }
        return controls;
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

        return RunInSta(service => service.TryInvokeCore(window, control));
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

        return RunInSta(service => service.TrySetValueCore(window, control, value));
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

        return RunInSta(service => service.TrySetTextCore(window, control, value, ensureForegroundWindow));
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

        return RunInSta(service => service.TrySendKeysCore(window, control, keys, ensureForegroundWindow));
    }

    public bool TryFocus(WindowInfo window, WindowControlInfo control, bool ensureForegroundWindow) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        return RunInSta(service => service.TryFocusCore(window, control, ensureForegroundWindow));
    }

    public IReadOnlyList<DesktopUiAutomationRootDiagnostic> DiagnoseRoots(IntPtr windowHandle, IReadOnlyList<IntPtr>? fallbackRootHandles = null, int sampleLimit = 3) {
        if (sampleLimit < 0) {
            throw new ArgumentOutOfRangeException(nameof(sampleLimit), "sampleLimit must be zero or greater.");
        }

        if (!IsAvailable || windowHandle == IntPtr.Zero) {
            return Array.Empty<DesktopUiAutomationRootDiagnostic>();
        }

        return RunInSta(service => service.DiagnoseRootsCore(windowHandle, fallbackRootHandles, sampleLimit));
    }

    public DesktopUiAutomationActionDiagnostic ProbeActionResolution(WindowInfo window, WindowControlInfo control) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        return RunInSta(service => service.ProbeActionResolutionCore(window, control));
    }

    private T RunInSta<T>(Func<UiAutomationControlService, T> operation) {
        if (!IsAvailable) {
            return default!;
        }

        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA) {
            return operation(this);
        }

        T result = default!;
        Exception? workerException = null;
        var thread = new Thread(() => {
            try {
                result = operation(new UiAutomationControlService());
            } catch (Exception ex) {
                workerException = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (workerException != null) {
            throw workerException;
        }

        return result;
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

    private bool TrySetValueCore(WindowInfo window, WindowControlInfo control, string value) {
        UiAutomationElementMatchResult match = ResolveMatchingElement(window.Handle, control);
        object? element = match.Element;
        if (element == null) {
            return false;
        }

        return TryPatternAction(element, "System.Windows.Automation.ValuePattern", "SetValue", value) ||
            TryPatternAction(element, "System.Windows.Automation.LegacyIAccessiblePattern", "SetValue", value);
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
        Thread.Sleep(ForegroundInputSettleMilliseconds);
        if (value.Length == 0) {
            KeyboardInputService.SendToForeground(VirtualKey.VK_DELETE);
        } else {
            KeyboardInputService.SendTextToForeground(value);
        }

        return WaitForResolvedValue(window, control, value);
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

                if (ScoreMatch(expected, info) > 0) {
                    return element;
                }
            } catch {
                // Fall through to the broader root search when fast exact lookup fails.
            }
        }

        return null;
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

    private object? CreatePropertyCondition(string propertyFieldName, string value) {
        if (string.IsNullOrWhiteSpace(propertyFieldName) || string.IsNullOrWhiteSpace(value)) {
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
            AutomationId = control.AutomationId,
            ControlType = control.ControlType,
            FrameworkId = control.FrameworkId,
            IsKeyboardFocusable = control.IsKeyboardFocusable,
            IsEnabled = control.IsEnabled,
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
            return null;
        }
    }

    private bool TryResolveRootElement(IntPtr rootHandle, out object? rootElement) {
        try {
            rootElement = _automationElementType!.GetMethod("FromHandle", BindingFlags.Public | BindingFlags.Static)?
                .Invoke(null, new object[] { rootHandle });
            return rootElement != null;
        } catch {
            rootElement = null;
            return false;
        }
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
        return controls.Any(existing =>
            (existing.Handle != IntPtr.Zero &&
            candidate.Handle != IntPtr.Zero &&
            existing.Handle == candidate.Handle) ||
            (string.Equals(existing.AutomationId, candidate.AutomationId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(existing.ControlType, candidate.ControlType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(existing.Text, candidate.Text, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(existing.ClassName, candidate.ClassName, StringComparison.OrdinalIgnoreCase)));
    }

    private WindowControlInfo? CreateControlInfo(object element) {
        object? current = element.GetType().GetProperty("Current", BindingFlags.Public | BindingFlags.Instance)?.GetValue(element);
        if (current == null) {
            return null;
        }

        string name = ReadString(current, "Name");
        string className = ReadString(current, "ClassName");
        string automationId = ReadString(current, "AutomationId");
        string frameworkId = ReadString(current, "FrameworkId");
        int nativeWindowHandle = ReadInt32(current, "NativeWindowHandle");
        bool? isKeyboardFocusable = ReadNullableBoolean(current, "IsKeyboardFocusable");
        bool? isEnabled = ReadNullableBoolean(current, "IsEnabled");
        bool? isOffscreen = ReadNullableBoolean(current, "IsOffscreen");
        string controlType = ReadControlTypeName(current);
        string value = ReadValue(element);
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
            AutomationId = automationId,
            ControlType = controlType,
            FrameworkId = frameworkId,
            IsKeyboardFocusable = isKeyboardFocusable,
            IsEnabled = isEnabled,
            SupportsBackgroundClick = hasNativeHandle || hasInvokeAction,
            SupportsBackgroundText = hasNativeHandle || hasDirectTextAction,
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
            Thread.Sleep(ForegroundInputSettleMilliseconds);
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

            Thread.Sleep(ForegroundTextVerificationIntervalMilliseconds);
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
