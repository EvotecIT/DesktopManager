using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace DesktopManager;

public partial class WindowManager {
    /// <summary>
    /// Clicks the specified control.
    /// </summary>
    /// <param name="control">Control to click.</param>
    /// <param name="button">Mouse button to use.</param>
    public void ClickControl(WindowControlInfo control, MouseButton button = MouseButton.Left) {
        WindowControlService.ControlClick(control, button);
    }

    /// <summary>
    /// Sets text on the specified control.
    /// </summary>
    /// <param name="control">Control to modify.</param>
    /// <param name="text">Text to apply.</param>
    public void SetControlText(WindowControlInfo control, string text) {
        WindowControlService.SetText(control, text);
    }

    /// <summary>
    /// Sends key input directly to the specified control.
    /// </summary>
    /// <param name="control">Target control.</param>
    /// <param name="keys">Keys to send.</param>
    public void SendControlKeys(WindowControlInfo control, params VirtualKey[] keys) {
        WindowControlService.SendKeys(control, keys);
    }

    /// <summary>
    /// Gets child controls for the specified window.
    /// </summary>
    /// <param name="window">Target window.</param>
    /// <param name="options">Optional control filter options.</param>
    /// <returns>A list of matching controls.</returns>
    public List<WindowControlInfo> GetControls(WindowInfo window, WindowControlQueryOptions? options = null) {
        return GetControls(window, options, maxTextLength: null, getUiAutomationTimeoutMilliseconds: null);
    }

    internal List<WindowControlInfo> GetControls(
        WindowInfo window,
        WindowControlQueryOptions? options,
        int? maxTextLength,
        Func<int>? getUiAutomationTimeoutMilliseconds = null) {
        ValidateWindowInfo(window);

        if (maxTextLength.HasValue && maxTextLength.Value < 1) {
            throw new ArgumentOutOfRangeException(nameof(maxTextLength), "maxTextLength must be greater than zero.");
        }

        WindowControlQueryOptions filter = options ?? new WindowControlQueryOptions();
        PrepareWindowForUiAutomation(window, filter);
        List<WindowControlInfo> controls = GetControlsInternal(window.Handle, filter, maxTextLength, getUiAutomationTimeoutMilliseconds);
        foreach (WindowControlInfo control in controls) {
            control.ParentWindowHandle = window.Handle;
        }

        if (maxTextLength.HasValue && !IsWildcardFilter(filter.ValuePattern)) {
            PopulateBoundedUiAutomationValues(window, controls, filter.ValuePattern, maxTextLength.Value, getUiAutomationTimeoutMilliseconds);
        }

        return controls.FindAll(control => MatchesControl(control, filter));
    }

    /// <summary>
    /// Collects shared diagnostics for control discovery against a single window.
    /// </summary>
    /// <param name="window">Target window.</param>
    /// <param name="options">Optional control filter options.</param>
    /// <param name="sampleLimit">Maximum number of sample controls to include.</param>
    /// <param name="includeActionProbe">Whether to include a read-only UI Automation action-resolution probe.</param>
    /// <returns>Discovery diagnostics for the supplied window.</returns>
    public DesktopControlDiscoveryDiagnostics DiagnoseControls(WindowInfo window, WindowControlQueryOptions? options = null, int sampleLimit = 10, bool includeActionProbe = false) {
        ValidateWindowInfo(window);
        if (sampleLimit < 0) {
            throw new ArgumentOutOfRangeException(nameof(sampleLimit), "sampleLimit must be zero or greater.");
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        WindowControlQueryOptions filter = options ?? new WindowControlQueryOptions();
        UiAutomationPreparationResult preparation = PrepareWindowForUiAutomation(window, filter);

        var enumerator = new ControlEnumerator();
        List<WindowControlInfo> win32Controls = filter.RequiresUiAutomation()
            ? enumerator.EnumerateControlMetadata(window.Handle)
            : enumerator.EnumerateControls(window.Handle);
        IReadOnlyList<IntPtr> fallbackRootHandles = UiAutomationControlService.GetFallbackRootHandles(window.Handle, win32Controls);
        IntPtr preferredRootHandle = UiAutomationControlService.GetPreferredSearchRootHandle(window.Handle, fallbackRootHandles);
        var uiAutomation = new UiAutomationControlService();
        IReadOnlyList<DesktopUiAutomationRootDiagnostic> rootDiagnostics = filter.RequiresUiAutomation()
            ? uiAutomation.DiagnoseRoots(window.Handle, fallbackRootHandles)
            : Array.Empty<DesktopUiAutomationRootDiagnostic>();
        List<WindowControlInfo> primaryUiAutomationControls = filter.RequiresUiAutomation()
            ? uiAutomation.EnumerateControls(window.Handle)
            : new List<WindowControlInfo>();
        List<WindowControlInfo> uiAutomationControls = filter.RequiresUiAutomation()
            ? primaryUiAutomationControls.Count > 0
                ? primaryUiAutomationControls
                : uiAutomation.EnumerateControls(window.Handle, fallbackRootHandles)
            : new List<WindowControlInfo>();
        if (filter.RequiresUiAutomation()) {
            ApplyUiAutomationPasswordMetadata(win32Controls, uiAutomationControls);
            ControlEnumerator.PopulateControlValues(win32Controls);
        }
        List<WindowControlInfo> effectiveControls = SelectDiscoveredControls(filter, win32Controls, uiAutomationControls);
        List<WindowControlInfo> matchedControls = effectiveControls.FindAll(control => MatchesControl(control, filter));
        DesktopUiAutomationActionDiagnostic? actionProbe = null;
        if (includeActionProbe) {
            WindowControlInfo? probeControl = matchedControls
                .FirstOrDefault(control => control.Source == WindowControlSource.UiAutomation)
                ?? effectiveControls.FirstOrDefault(control => control.Source == WindowControlSource.UiAutomation);
            if (probeControl != null) {
                Stopwatch actionProbeStopwatch = Stopwatch.StartNew();
                actionProbe = uiAutomation.ProbeActionResolution(window, probeControl);
                actionProbe.ElapsedMilliseconds = (int)actionProbeStopwatch.ElapsedMilliseconds;
            }
        }

        stopwatch.Stop();

        return new DesktopControlDiscoveryDiagnostics {
            Window = window,
            RequiresUiAutomation = filter.RequiresUiAutomation(),
            UseUiAutomation = filter.UseUiAutomation,
            IncludeUiAutomation = filter.IncludeUiAutomation,
            EnsureForegroundWindow = filter.EnsureForegroundWindow,
            UiAutomationAvailable = uiAutomation.IsAvailable,
            ElapsedMilliseconds = (int)stopwatch.ElapsedMilliseconds,
            PreparationAttempted = preparation.Attempted,
            PreparationSucceeded = preparation.Succeeded,
            UiAutomationFallbackRootCount = fallbackRootHandles.Count,
            UsedUiAutomationFallbackRoots = filter.RequiresUiAutomation() && rootDiagnostics.Any(root => !root.IsPrimaryRoot && root.ControlCount > 0),
            UsedCachedUiAutomationControls = filter.RequiresUiAutomation() && rootDiagnostics.Any(root => root.UsedCachedControls),
            UsedPreferredUiAutomationRoot = filter.RequiresUiAutomation() && preferredRootHandle != IntPtr.Zero && rootDiagnostics.Any(root => root.IsPreferredRoot && root.ControlCount > 0),
            PreferredUiAutomationRootHandle = preferredRootHandle,
            EffectiveSource = GetEffectiveSource(filter, uiAutomationControls),
            Win32ControlCount = win32Controls.Count,
            UiAutomationControlCount = uiAutomationControls.Count,
            EffectiveControlCount = effectiveControls.Count,
            MatchedControlCount = matchedControls.Count,
            SampleControls = effectiveControls.Take(sampleLimit).ToArray(),
            UiAutomationRoots = rootDiagnostics,
            UiAutomationActionProbe = actionProbe
        };
    }

    private UiAutomationPreparationResult PrepareWindowForUiAutomation(WindowInfo window, WindowControlQueryOptions filter) {
        if (!filter.RequiresUiAutomation() || !filter.EnsureForegroundWindow) {
            return UiAutomationPreparationResult.None;
        }

        if (WindowActivationService.TryPrepareWindowForAutomation(window.Handle)) {
            Thread.Sleep(200);
            return UiAutomationPreparationResult.Success;
        }

        return UiAutomationPreparationResult.Failed;
    }

    private List<WindowControlInfo> GetControlsInternal(
        IntPtr windowHandle,
        WindowControlQueryOptions filter,
        int? maxTextLength,
        Func<int>? getUiAutomationTimeoutMilliseconds) {
        var enumerator = new ControlEnumerator();
        List<WindowControlInfo> win32Controls = filter.RequiresUiAutomation()
            ? enumerator.EnumerateControlMetadata(windowHandle)
            : maxTextLength.HasValue
                ? enumerator.EnumerateControls(windowHandle, maxTextLength.Value)
                : enumerator.EnumerateControls(windowHandle);

        if (!filter.RequiresUiAutomation()) {
            return win32Controls;
        }

        var uiAutomation = new UiAutomationControlService();
        IReadOnlyList<IntPtr> fallbackRootHandles = UiAutomationControlService.GetFallbackRootHandles(windowHandle, win32Controls);
        int providerTimeout = getUiAutomationTimeoutMilliseconds?.Invoke() ?? UiAutomationStaDispatcher.DefaultInvocationTimeoutMilliseconds;
        List<WindowControlInfo> uiAutomationControls = providerTimeout <= 0
            ? new List<WindowControlInfo>()
            : uiAutomation.EnumerateControls(
                windowHandle,
                fallbackRootHandles,
                readValues: !maxTextLength.HasValue,
                invocationTimeoutMilliseconds: providerTimeout);
        ApplyUiAutomationPasswordMetadata(win32Controls, uiAutomationControls);
        ControlEnumerator.PopulateControlValues(win32Controls, maxTextLength, getUiAutomationTimeoutMilliseconds);

        return SelectDiscoveredControls(filter, win32Controls, uiAutomationControls);
    }

    private static void ApplyUiAutomationPasswordMetadata(List<WindowControlInfo> win32Controls, List<WindowControlInfo> uiAutomationControls) {
        foreach (WindowControlInfo uiAutomationControl in uiAutomationControls) {
            if (uiAutomationControl.IsPassword != true || uiAutomationControl.Handle == IntPtr.Zero) {
                continue;
            }

            WindowControlInfo? nativeControl = win32Controls.FirstOrDefault(candidate => candidate.Handle == uiAutomationControl.Handle);
            if (nativeControl != null) {
                nativeControl.IsPassword = true;
                nativeControl.Text = string.Empty;
                nativeControl.Value = string.Empty;
                nativeControl.ValueIsTruncated = false;
            }
        }
    }

    private static void PopulateBoundedUiAutomationValues(
        WindowInfo window,
        IEnumerable<WindowControlInfo> controls,
        string valuePattern,
        int maxTextLength,
        Func<int>? getUiAutomationTimeoutMilliseconds) {
        string? providerExpectedText = GetProviderContainsLiteral(valuePattern);
        var uiAutomation = new UiAutomationControlService();
        var options = new DesktopControlObservationOptions {
            MaxTextLength = maxTextLength,
            ExpectedText = providerExpectedText,
            IgnoreCase = true,
            IncludeTextRanges = false,
            IncludeSemanticState = false
        };
        foreach (WindowControlInfo control in controls) {
            if (!control.HasUiAutomationIdentity && control.Source != WindowControlSource.UiAutomation) {
                continue;
            }

            int providerTimeout = getUiAutomationTimeoutMilliseconds?.Invoke() ?? UiAutomationStaDispatcher.DefaultInvocationTimeoutMilliseconds;
            if (providerTimeout <= 0) {
                break;
            }

            DesktopControlObservation? observation = uiAutomation.TryObserveControl(window, control, options, providerTimeout);
            if (observation?.IsPassword != false) {
                control.Value = string.Empty;
                control.ValueIsTruncated = false;
                continue;
            }

            control.Value = observation.Text.Value;
            control.ValueIsTruncated = observation.Text.IsTruncated;
            control.ValueMatchPattern = providerExpectedText == null ? string.Empty : valuePattern;
            control.ValueMatchIgnoreCase = true;
            control.ValuePatternMatched = providerExpectedText == null ? null : observation.Text.ContainsExpected;
        }
    }

    private static bool IsWildcardFilter(string? value) {
        return string.IsNullOrWhiteSpace(value) || value == "*";
    }

    private static List<WindowControlInfo> SelectDiscoveredControls(WindowControlQueryOptions filter, List<WindowControlInfo> win32Controls, List<WindowControlInfo> uiAutomationControls) {
        if (filter.UseUiAutomation && !filter.IncludeUiAutomation) {
            return uiAutomationControls;
        }

        if (!filter.IncludeUiAutomation) {
            return uiAutomationControls.Count > 0 ? uiAutomationControls : win32Controls;
        }

        return MergeControls(win32Controls, uiAutomationControls);
    }

    private static string GetEffectiveSource(WindowControlQueryOptions filter, List<WindowControlInfo> uiAutomationControls) {
        if (!filter.RequiresUiAutomation()) {
            return "Win32";
        }

        if (filter.UseUiAutomation && !filter.IncludeUiAutomation) {
            return "UiAutomation";
        }

        if (filter.IncludeUiAutomation) {
            return "Merged";
        }

        return uiAutomationControls.Count > 0 ? "UiAutomationFallback" : "Win32Fallback";
    }

    private static List<WindowControlInfo> MergeControls(List<WindowControlInfo> win32Controls, List<WindowControlInfo> uiAutomationControls) {
        var merged = new List<WindowControlInfo>(win32Controls.Count + uiAutomationControls.Count);
        merged.AddRange(win32Controls);

        foreach (WindowControlInfo uiAutomationControl in uiAutomationControls) {
            WindowControlInfo? existing = merged.FirstOrDefault(candidate => AreEquivalentControls(candidate, uiAutomationControl));
            if (existing == null) {
                merged.Add(uiAutomationControl);
                continue;
            }

            MergeControlMetadata(existing, uiAutomationControl);
        }

        return merged;
    }

    internal static bool AreEquivalentControls(WindowControlInfo existing, WindowControlInfo candidate) {
        bool bothHandlesKnown = existing.Handle != IntPtr.Zero && candidate.Handle != IntPtr.Zero;
        if (bothHandlesKnown && existing.Handle != candidate.Handle) {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(existing.RuntimeId) &&
            !string.IsNullOrWhiteSpace(candidate.RuntimeId)) {
            return string.Equals(existing.RuntimeId, candidate.RuntimeId, StringComparison.Ordinal);
        }

        if (bothHandlesKnown) {
            return true;
        }

        bool metadataMatches = string.Equals(existing.AutomationId, candidate.AutomationId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(existing.ControlType, candidate.ControlType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(existing.Text, candidate.Text, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(existing.ClassName, candidate.ClassName, StringComparison.OrdinalIgnoreCase);
        if (!metadataMatches) {
            return false;
        }

        if (existing.IsPassword == true || candidate.IsPassword == true) {
            return existing.IsPassword == true &&
                candidate.IsPassword == true &&
                existing.Handle == IntPtr.Zero &&
                candidate.Handle == IntPtr.Zero &&
                HasUsableBounds(existing) &&
                HasUsableBounds(candidate) &&
                existing.Left == candidate.Left &&
                existing.Top == candidate.Top &&
                existing.Width == candidate.Width &&
                existing.Height == candidate.Height;
        }

        return true;
    }

    private static bool HasUsableBounds(WindowControlInfo control) {
        return control.Width > 0 && control.Height > 0;
    }

    internal static void MergeControlMetadata(WindowControlInfo target, WindowControlInfo source) {
        if (target == null || source == null) {
            return;
        }

        if (string.IsNullOrWhiteSpace(target.AutomationId)) {
            target.AutomationId = source.AutomationId;
        }

        if (string.IsNullOrWhiteSpace(target.RuntimeId)) {
            target.RuntimeId = source.RuntimeId;
        }

        if (string.IsNullOrWhiteSpace(target.ControlType)) {
            target.ControlType = source.ControlType;
        }

        if (string.IsNullOrWhiteSpace(target.FrameworkId)) {
            target.FrameworkId = source.FrameworkId;
        }

        target.HasUiAutomationIdentity = target.HasUiAutomationIdentity ||
            source.HasUiAutomationIdentity ||
            source.Source == WindowControlSource.UiAutomation;

        if (!target.IsKeyboardFocusable.HasValue) {
            target.IsKeyboardFocusable = source.IsKeyboardFocusable;
        }

        if (!target.IsEnabled.HasValue) {
            target.IsEnabled = source.IsEnabled;
        }

        if (!target.IsOffscreen.HasValue) {
            target.IsOffscreen = source.IsOffscreen;
        }

        if (!target.IsPassword.HasValue) {
            target.IsPassword = source.IsPassword;
        }

        if (target.IsPassword == true || source.IsPassword == true) {
            target.IsPassword = true;
            target.Text = string.Empty;
            target.Value = string.Empty;
            target.ValueIsTruncated = false;
        } else if (string.IsNullOrWhiteSpace(target.Value)) {
            target.Value = source.Value;
        }

        if (target.IsPassword != true) {
            target.ValueIsTruncated = target.ValueIsTruncated || source.ValueIsTruncated;
        }

        if (target.Width <= 0 || target.Height <= 0) {
            target.Left = source.Left;
            target.Top = source.Top;
            target.Width = source.Width;
            target.Height = source.Height;
        }

        target.SupportsBackgroundClick = target.SupportsBackgroundClick || source.SupportsBackgroundClick;
        target.SupportsBackgroundText = target.SupportsBackgroundText || source.SupportsBackgroundText;
        target.SupportsBackgroundKeys = target.SupportsBackgroundKeys || source.SupportsBackgroundKeys;
        target.SupportsForegroundInputFallback = target.SupportsForegroundInputFallback || source.SupportsForegroundInputFallback;
    }

    private bool MatchesControl(WindowControlInfo control, WindowControlQueryOptions filter) {
        if (filter.Handle.HasValue && filter.Handle.Value != IntPtr.Zero) {
            if (control.Handle != filter.Handle.Value) {
                return false;
            }
        }

        if (filter.Id.HasValue) {
            if (control.Id != filter.Id.Value) {
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.ClassNamePattern) && filter.ClassNamePattern != "*") {
            if (!MatchesWildcard(control.ClassName, filter.ClassNamePattern)) {
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.TextPattern) && filter.TextPattern != "*") {
            if (!MatchesWildcard(control.Text ?? string.Empty, filter.TextPattern)) {
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.AutomationIdPattern) && filter.AutomationIdPattern != "*") {
            if (!MatchesWildcard(control.AutomationId, filter.AutomationIdPattern)) {
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.ControlTypePattern) && filter.ControlTypePattern != "*") {
            if (!MatchesWildcard(control.ControlType, filter.ControlTypePattern)) {
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.FrameworkIdPattern) && filter.FrameworkIdPattern != "*") {
            if (!MatchesWildcard(control.FrameworkId, filter.FrameworkIdPattern)) {
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.ValuePattern) && filter.ValuePattern != "*") {
            if (!MatchesValuePattern(control, filter.ValuePattern)) {
                return false;
            }
        }

        if (filter.IsEnabled.HasValue) {
            if (!control.IsEnabled.HasValue || control.IsEnabled.Value != filter.IsEnabled.Value) {
                return false;
            }
        }

        if (filter.IsKeyboardFocusable.HasValue) {
            if (!control.IsKeyboardFocusable.HasValue || control.IsKeyboardFocusable.Value != filter.IsKeyboardFocusable.Value) {
                return false;
            }
        }

        if (filter.SupportsBackgroundClick.HasValue && control.SupportsBackgroundClick != filter.SupportsBackgroundClick.Value) {
            return false;
        }

        if (filter.SupportsBackgroundText.HasValue && control.SupportsBackgroundText != filter.SupportsBackgroundText.Value) {
            return false;
        }

        if (filter.SupportsBackgroundKeys.HasValue && control.SupportsBackgroundKeys != filter.SupportsBackgroundKeys.Value) {
            return false;
        }

        if (filter.SupportsForegroundInputFallback.HasValue && control.SupportsForegroundInputFallback != filter.SupportsForegroundInputFallback.Value) {
            return false;
        }

        return true;
    }

    internal bool MatchesValuePattern(WindowControlInfo control, string valuePattern) {
        bool matchingProviderEvidence = control.ValuePatternMatched == true &&
            control.ValueMatchIgnoreCase &&
            string.Equals(control.ValueMatchPattern, valuePattern, StringComparison.OrdinalIgnoreCase);
        return matchingProviderEvidence || MatchesWildcard(control.Value ?? string.Empty, valuePattern);
    }

    internal static string? GetProviderContainsLiteral(string valuePattern) {
        if (string.IsNullOrEmpty(valuePattern) || valuePattern.IndexOf('?') >= 0) {
            return null;
        }

        int firstWildcard = valuePattern.IndexOf('*');
        if (firstWildcard < 0) {
            return valuePattern;
        }

        if (firstWildcard != 0 || valuePattern.Length < 3 || valuePattern[valuePattern.Length - 1] != '*') {
            return null;
        }

        string literal = valuePattern.Substring(1, valuePattern.Length - 2);
        return literal.IndexOf('*') < 0 && literal.Length > 0 ? literal : null;
    }

    /// <summary>
    /// Gets child controls for one or more windows matched by the supplied window query.
    /// </summary>
    /// <param name="windowOptions">Window query options.</param>
    /// <param name="controlOptions">Optional control filter options.</param>
    /// <param name="allWindows">Whether to enumerate controls for all matching windows.</param>
    /// <returns>A list of matching control targets.</returns>
    public List<WindowControlTargetInfo> GetControls(WindowQueryOptions windowOptions, WindowControlQueryOptions? controlOptions = null, bool allWindows = false) {
        if (windowOptions == null) {
            throw new ArgumentNullException(nameof(windowOptions));
        }

        List<WindowInfo> windows = GetWindows(windowOptions);
        if (!allWindows && windows.Count > 1) {
            windows = new List<WindowInfo> { windows[0] };
        }

        var results = new List<WindowControlTargetInfo>();
        foreach (WindowInfo window in windows) {
            List<WindowControlInfo> controls = GetControls(window, controlOptions);
            foreach (WindowControlInfo control in controls) {
                results.Add(new WindowControlTargetInfo {
                    Window = window,
                    Control = control
                });
            }
        }

        return results;
    }

    private readonly struct UiAutomationPreparationResult {
        public static UiAutomationPreparationResult None => new(false, false);
        public static UiAutomationPreparationResult Success => new(true, true);
        public static UiAutomationPreparationResult Failed => new(true, false);

        public UiAutomationPreparationResult(bool attempted, bool succeeded) {
            Attempted = attempted;
            Succeeded = succeeded;
        }

        public bool Attempted { get; }
        public bool Succeeded { get; }
    }
}
