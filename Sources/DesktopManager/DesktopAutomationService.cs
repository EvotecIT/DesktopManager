using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.Versioning;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace DesktopManager;

/// <summary>
/// Provides higher-level desktop automation orchestration on top of WindowManager and related services.
/// </summary>
public sealed class DesktopAutomationService {
    private const int PreferredWindowRediscoveryMilliseconds = 1000;
    private readonly WindowManager _windowManager;
    private readonly Monitors _monitors;
    private static readonly JsonSerializerOptions TargetSerializerOptions = new() {
        WriteIndented = true
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="DesktopAutomationService"/> class.
    /// </summary>
    public DesktopAutomationService() {
        _windowManager = new WindowManager();
        _monitors = new Monitors();
    }

    /// <summary>
    /// Gets matching monitors.
    /// </summary>
    public IReadOnlyList<Monitor> GetMonitors(bool? connectedOnly = null, bool? primaryOnly = null, int? index = null, string? deviceId = null, string? deviceName = null, bool refresh = false) {
        return _monitors.GetMonitors(connectedOnly: connectedOnly, primaryOnly: primaryOnly, index: index, deviceId: deviceId, deviceName: deviceName, refresh: refresh);
    }

    /// <summary>
    /// Gets a single matching monitor when it can be resolved.
    /// </summary>
    /// <param name="connectedOnly">Whether to limit matches to connected monitors.</param>
    /// <param name="primaryOnly">Whether to limit matches to the primary monitor.</param>
    /// <param name="index">Monitor index.</param>
    /// <param name="deviceId">Monitor device identifier.</param>
    /// <param name="deviceName">Monitor device name.</param>
    /// <param name="refresh">Whether to refresh the cached monitor snapshot first.</param>
    /// <returns>The matching monitor when found; otherwise null.</returns>
    public Monitor? GetMonitor(bool? connectedOnly = null, bool? primaryOnly = null, int? index = null, string? deviceId = null, string? deviceName = null, bool refresh = false) {
        return _monitors.GetMonitors(
            connectedOnly: connectedOnly,
            primaryOnly: primaryOnly,
            index: index,
            deviceId: deviceId,
            deviceName: deviceName,
            refresh: refresh).FirstOrDefault();
    }

    /// <summary>
    /// Forces monitor enumeration and refreshes the cached monitor snapshot.
    /// </summary>
    public void RefreshMonitors() {
        _monitors.RefreshMonitors();
    }

    /// <summary>
    /// Gets the desktop background color.
    /// </summary>
    /// <returns>The background color as an RGB value.</returns>
    public uint GetDesktopBackgroundColor() {
        return _monitors.GetBackgroundColor();
    }

    /// <summary>
    /// Sets the desktop background color.
    /// </summary>
    /// <param name="color">The background color as an RGB value.</param>
    public void SetDesktopBackgroundColor(uint color) {
        _monitors.SetBackgroundColor(color);
    }

    /// <summary>
    /// Gets the wallpaper path for a monitor.
    /// </summary>
    /// <param name="deviceId">Monitor device identifier.</param>
    /// <returns>The wallpaper path.</returns>
    public string GetMonitorWallpaper(string deviceId) {
        if (string.IsNullOrWhiteSpace(deviceId)) {
            throw new ArgumentException("A monitor device identifier is required.", nameof(deviceId));
        }

        return _monitors.GetWallpaper(deviceId);
    }

    /// <summary>
    /// Sets the wallpaper path for a monitor.
    /// </summary>
    /// <param name="deviceId">Monitor device identifier.</param>
    /// <param name="wallpaperPath">Wallpaper file path.</param>
    public void SetMonitorWallpaper(string deviceId, string wallpaperPath) {
        if (string.IsNullOrWhiteSpace(deviceId)) {
            throw new ArgumentException("A monitor device identifier is required.", nameof(deviceId));
        }

        if (string.IsNullOrWhiteSpace(wallpaperPath)) {
            throw new ArgumentException("A wallpaper path is required.", nameof(wallpaperPath));
        }

        _monitors.SetWallpaper(deviceId, wallpaperPath);
    }

    /// <summary>
    /// Sets the wallpaper for a monitor from a stream.
    /// </summary>
    /// <param name="deviceId">Monitor device identifier.</param>
    /// <param name="imageStream">Wallpaper image stream.</param>
    public void SetMonitorWallpaper(string deviceId, Stream imageStream) {
        if (string.IsNullOrWhiteSpace(deviceId)) {
            throw new ArgumentException("A monitor device identifier is required.", nameof(deviceId));
        }

        if (imageStream == null) {
            throw new ArgumentNullException(nameof(imageStream));
        }

        _monitors.SetWallpaper(deviceId, imageStream);
    }

    /// <summary>
    /// Sets the wallpaper for a monitor from a URL.
    /// </summary>
    /// <param name="deviceId">Monitor device identifier.</param>
    /// <param name="url">Wallpaper URL.</param>
    public void SetMonitorWallpaperFromUrl(string deviceId, string url) {
        if (string.IsNullOrWhiteSpace(deviceId)) {
            throw new ArgumentException("A monitor device identifier is required.", nameof(deviceId));
        }

        if (string.IsNullOrWhiteSpace(url)) {
            throw new ArgumentException("A wallpaper URL is required.", nameof(url));
        }

        _monitors.SetWallpaperFromUrl(deviceId, url);
    }

    /// <summary>
    /// Gets the current desktop wallpaper position.
    /// </summary>
    /// <returns>The current wallpaper position.</returns>
    public DesktopWallpaperPosition GetDesktopWallpaperPosition() {
        return _monitors.GetWallpaperPosition();
    }

    /// <summary>
    /// Sets the current desktop wallpaper position.
    /// </summary>
    /// <param name="position">Wallpaper position to apply.</param>
    public void SetDesktopWallpaperPosition(DesktopWallpaperPosition position) {
        _monitors.SetWallpaperPosition(position);
    }

    /// <summary>
    /// Sets the wallpaper for all user profiles.
    /// </summary>
    /// <param name="wallpaperPath">Wallpaper file path.</param>
    /// <param name="position">Wallpaper position to persist.</param>
    /// <param name="includeDefaultProfile">Whether to include the default profile.</param>
    public void SetDesktopWallpaperForAllUsers(string wallpaperPath, DesktopWallpaperPosition position, bool includeDefaultProfile = true) {
        if (string.IsNullOrWhiteSpace(wallpaperPath)) {
            throw new ArgumentException("A wallpaper path is required.", nameof(wallpaperPath));
        }

        _monitors.SetWallpaperForAllUsers(wallpaperPath, position, includeDefaultProfile);
    }

    /// <summary>
    /// Gets the current logon wallpaper path when it can be resolved.
    /// </summary>
    /// <returns>The logon wallpaper path.</returns>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public string GetLogonWallpaper() {
        return _monitors.GetLogonWallpaper();
    }

    /// <summary>
    /// Sets the logon wallpaper after enforcing elevation requirements.
    /// </summary>
    /// <param name="imagePath">Path to the logon wallpaper image.</param>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public void SetLogonWallpaper(string imagePath) {
        if (string.IsNullOrWhiteSpace(imagePath)) {
            throw new ArgumentException("An image path is required.", nameof(imagePath));
        }

        EnsureElevated();
        _monitors.SetLogonWallpaper(imagePath);
    }

    /// <summary>
    /// Starts a desktop wallpaper slideshow.
    /// </summary>
    /// <param name="imagePaths">Wallpaper image paths.</param>
    public void StartDesktopSlideshow(IEnumerable<string> imagePaths) {
        StartDesktopSlideshow(imagePaths, null, null);
    }

    /// <summary>
    /// Starts a desktop wallpaper slideshow with optional slideshow settings.
    /// </summary>
    /// <param name="imagePaths">Wallpaper image paths.</param>
    /// <param name="options">Optional slideshow options.</param>
    /// <param name="slideshowTick">Optional slideshow tick interval in milliseconds.</param>
    public void StartDesktopSlideshow(IEnumerable<string> imagePaths, DesktopSlideshowOptions? options, uint? slideshowTick) {
        if (imagePaths == null) {
            throw new ArgumentNullException(nameof(imagePaths));
        }

        _monitors.StartWallpaperSlideshow(imagePaths, options, slideshowTick);
    }

    /// <summary>
    /// Stops the desktop wallpaper slideshow.
    /// </summary>
    public void StopDesktopSlideshow() {
        _monitors.StopWallpaperSlideshow();
    }

    /// <summary>
    /// Advances the desktop wallpaper slideshow.
    /// </summary>
    /// <param name="direction">Direction to advance.</param>
    public void AdvanceDesktopSlideshow(DesktopSlideshowDirection direction) {
        _monitors.AdvanceWallpaperSlide(direction);
    }

    /// <summary>
    /// Gets the current desktop wallpaper slideshow configuration and state.
    /// </summary>
    /// <returns>The current wallpaper slideshow details.</returns>
    public DesktopWallpaperSlideshow GetDesktopSlideshow() {
        return _monitors.GetWallpaperSlideshow();
    }

    /// <summary>
    /// Sets desktop wallpaper slideshow options.
    /// </summary>
    /// <param name="options">Slideshow options.</param>
    /// <param name="slideshowTick">Slideshow tick interval in milliseconds.</param>
    public void SetDesktopSlideshowOptions(DesktopSlideshowOptions options, uint slideshowTick) {
        _monitors.SetWallpaperSlideshowOptions(options, slideshowTick);
    }

    /// <summary>
    /// Gets the brightness for a monitor.
    /// </summary>
    /// <param name="deviceId">Monitor device identifier.</param>
    /// <returns>The current brightness level.</returns>
    public int GetMonitorBrightness(string deviceId) {
        if (string.IsNullOrWhiteSpace(deviceId)) {
            throw new ArgumentException("A monitor device identifier is required.", nameof(deviceId));
        }

        return _monitors.GetMonitorBrightness(deviceId);
    }

    /// <summary>
    /// Sets the brightness for a monitor.
    /// </summary>
    /// <param name="deviceId">Monitor device identifier.</param>
    /// <param name="brightness">Brightness level.</param>
    public void SetMonitorBrightness(string deviceId, int brightness) {
        if (string.IsNullOrWhiteSpace(deviceId)) {
            throw new ArgumentException("A monitor device identifier is required.", nameof(deviceId));
        }

        _monitors.SetMonitorBrightness(deviceId, brightness);
    }

    /// <summary>
    /// Gets Advanced Color and HDR state for a monitor.
    /// </summary>
    /// <param name="deviceId">Monitor device identifier.</param>
    /// <returns>The Advanced Color and HDR state.</returns>
    public MonitorAdvancedColorInfo GetMonitorAdvancedColor(string deviceId) {
        if (string.IsNullOrWhiteSpace(deviceId)) {
            throw new ArgumentException("A monitor device identifier is required.", nameof(deviceId));
        }

        return _monitors.GetMonitorAdvancedColor(deviceId);
    }

    /// <summary>
    /// Enables or disables HDR for a monitor.
    /// </summary>
    /// <param name="deviceId">Monitor device identifier.</param>
    /// <param name="enabled">Whether HDR should be enabled.</param>
    public void SetMonitorHdr(string deviceId, bool enabled) {
        if (string.IsNullOrWhiteSpace(deviceId)) {
            throw new ArgumentException("A monitor device identifier is required.", nameof(deviceId));
        }

        _monitors.SetMonitorHdr(deviceId, enabled);
    }

    /// <summary>
    /// Gets the current position for a monitor.
    /// </summary>
    /// <param name="deviceId">Monitor device identifier.</param>
    /// <returns>The current monitor position.</returns>
    public MonitorPosition GetMonitorPosition(string deviceId) {
        if (string.IsNullOrWhiteSpace(deviceId)) {
            throw new ArgumentException("A monitor device identifier is required.", nameof(deviceId));
        }

        return _monitors.GetMonitorPosition(deviceId);
    }

    /// <summary>
    /// Sets the current position for a monitor.
    /// </summary>
    /// <param name="deviceId">Monitor device identifier.</param>
    /// <param name="position">Target monitor position.</param>
    public void SetMonitorPosition(string deviceId, MonitorPosition position) {
        if (string.IsNullOrWhiteSpace(deviceId)) {
            throw new ArgumentException("A monitor device identifier is required.", nameof(deviceId));
        }

        if (position == null) {
            throw new ArgumentNullException(nameof(position));
        }

        _monitors.SetMonitorPosition(deviceId, position);
    }

    /// <summary>
    /// Sets the resolution for a monitor.
    /// </summary>
    /// <param name="deviceId">Monitor device identifier.</param>
    /// <param name="width">Target width.</param>
    /// <param name="height">Target height.</param>
    public void SetMonitorResolution(string deviceId, int width, int height) {
        if (string.IsNullOrWhiteSpace(deviceId)) {
            throw new ArgumentException("A monitor device identifier is required.", nameof(deviceId));
        }

        _monitors.SetMonitorResolution(deviceId, width, height);
    }

    /// <summary>
    /// Sets the orientation for a monitor.
    /// </summary>
    /// <param name="deviceId">Monitor device identifier.</param>
    /// <param name="orientation">Target orientation.</param>
    public void SetMonitorOrientation(string deviceId, DisplayOrientation orientation) {
        if (string.IsNullOrWhiteSpace(deviceId)) {
            throw new ArgumentException("A monitor device identifier is required.", nameof(deviceId));
        }

        _monitors.SetMonitorOrientation(deviceId, orientation);
    }

    /// <summary>
    /// Sets the DPI scaling for a monitor.
    /// </summary>
    /// <param name="deviceId">Monitor device identifier.</param>
    /// <param name="scalingPercent">Target scaling percentage.</param>
    public void SetMonitorDpiScaling(string deviceId, int scalingPercent) {
        if (string.IsNullOrWhiteSpace(deviceId)) {
            throw new ArgumentException("A monitor device identifier is required.", nameof(deviceId));
        }

        _monitors.SetMonitorDpiScaling(deviceId, scalingPercent);
    }

    /// <summary>
    /// Sets the taskbar position for a monitor.
    /// </summary>
    /// <param name="monitorIndex">Monitor index.</param>
    /// <param name="position">Target taskbar position.</param>
    public void SetTaskbarPosition(int monitorIndex, TaskbarPosition position) {
        if (monitorIndex < 0) {
            throw new ArgumentOutOfRangeException(nameof(monitorIndex), "Monitor index must be zero or greater.");
        }

        new TaskbarService().SetTaskbarPosition(monitorIndex, position);
    }

    /// <summary>
    /// Sets taskbar visibility for a monitor.
    /// </summary>
    /// <param name="monitorIndex">Monitor index.</param>
    /// <param name="visible">Whether the taskbar should be visible.</param>
    public void SetTaskbarVisibility(int monitorIndex, bool visible) {
        if (monitorIndex < 0) {
            throw new ArgumentOutOfRangeException(nameof(monitorIndex), "Monitor index must be zero or greater.");
        }

        new TaskbarService().SetTaskbarVisibility(monitorIndex, visible);
    }

    /// <summary>
    /// Gets matching windows.
    /// </summary>
    public List<WindowInfo> GetWindows(WindowQueryOptions options) {
        return _windowManager.GetWindows(options);
    }

    /// <summary>
    /// Gets a single window by its handle when it can be resolved.
    /// </summary>
    /// <param name="handle">Window handle.</param>
    /// <param name="includeHidden">Whether to include hidden windows.</param>
    /// <param name="includeCloaked">Whether to include cloaked windows.</param>
    /// <param name="includeOwned">Whether to include owned windows.</param>
    /// <param name="includeEmptyTitles">Whether to include windows with empty titles.</param>
    /// <returns>The matching window when found; otherwise null.</returns>
    public WindowInfo? GetWindow(IntPtr handle, bool includeHidden = true, bool includeCloaked = true, bool includeOwned = true, bool includeEmptyTitles = true) {
        if (handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid window handle.", nameof(handle));
        }

        return _windowManager.GetWindow(handle, includeHidden, includeCloaked, includeOwned, includeEmptyTitles);
    }

    /// <summary>
    /// Gets process information for a resolved window.
    /// </summary>
    /// <param name="window">Window to inspect.</param>
    /// <returns>The process information.</returns>
    public WindowProcessInfo GetWindowProcessInfo(WindowInfo window) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        return _windowManager.GetWindowProcessInfo(window);
    }

    /// <summary>
    /// Gets process information for a specific window handle when it can be resolved.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    /// <returns>The process information.</returns>
    public WindowProcessInfo GetWindowProcessInfo(IntPtr windowHandle) {
        WindowInfo window = GetWindow(windowHandle, includeHidden: true, includeCloaked: true, includeOwned: true, includeEmptyTitles: true)
            ?? throw new InvalidOperationException("The requested window could not be resolved.");
        return GetWindowProcessInfo(window);
    }

    /// <summary>
    /// Gets process information for the owner of a resolved window when available.
    /// </summary>
    /// <param name="window">Window to inspect.</param>
    /// <returns>The owner process information when available; otherwise null.</returns>
    public WindowProcessInfo? GetOwnerWindowProcessInfo(WindowInfo window) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        return _windowManager.GetOwnerProcessInfo(window);
    }

    /// <summary>
    /// Gets process information for the owner of a specific window handle when available.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    /// <returns>The owner process information when available; otherwise null.</returns>
    public WindowProcessInfo? GetOwnerWindowProcessInfo(IntPtr windowHandle) {
        WindowInfo window = GetWindow(windowHandle, includeHidden: true, includeCloaked: true, includeOwned: true, includeEmptyTitles: true)
            ?? throw new InvalidOperationException("The requested window could not be resolved.");
        return GetOwnerWindowProcessInfo(window);
    }

    /// <summary>
    /// Gets a single control by its handle for a specific parent window when it can be resolved.
    /// </summary>
    /// <param name="windowHandle">Parent window handle.</param>
    /// <param name="controlHandle">Control handle.</param>
    /// <param name="useUiAutomation">Whether to request UI Automation discovery.</param>
    /// <param name="includeUiAutomation">Whether to combine Win32 and UI Automation discovery.</param>
    /// <returns>The matching control when found; otherwise null.</returns>
    public WindowControlInfo? GetControl(IntPtr windowHandle, IntPtr controlHandle, bool useUiAutomation = true, bool includeUiAutomation = true) {
        if (windowHandle == IntPtr.Zero) {
            throw new ArgumentException("Invalid window handle.", nameof(windowHandle));
        }

        if (controlHandle == IntPtr.Zero) {
            throw new ArgumentException("Invalid control handle.", nameof(controlHandle));
        }

        WindowInfo? window = GetWindow(windowHandle, includeHidden: true, includeCloaked: true, includeOwned: true, includeEmptyTitles: true);
        if (window == null) {
            return null;
        }

        WindowControlInfo? nativeMatch = _windowManager.GetControls(
            window,
            new WindowControlQueryOptions {
                Handle = controlHandle,
                UseUiAutomation = false,
                IncludeUiAutomation = false
            }).FirstOrDefault();
        if (nativeMatch != null || (!useUiAutomation && !includeUiAutomation)) {
            return nativeMatch;
        }

        return _windowManager.GetControls(
            window,
            new WindowControlQueryOptions {
                Handle = controlHandle,
                UseUiAutomation = useUiAutomation,
                IncludeUiAutomation = includeUiAutomation
            }).FirstOrDefault();
    }

    /// <summary>
    /// Gets the observable state for a specific control when it can be resolved.
    /// </summary>
    /// <param name="windowHandle">Parent window handle.</param>
    /// <param name="controlHandle">Control handle.</param>
    /// <param name="useUiAutomation">Whether to request UI Automation discovery.</param>
    /// <param name="includeUiAutomation">Whether to combine Win32 and UI Automation discovery.</param>
    /// <returns>The control state when found; otherwise null.</returns>
    public DesktopControlState? GetControlState(IntPtr windowHandle, IntPtr controlHandle, bool useUiAutomation = true, bool includeUiAutomation = true) {
        WindowControlInfo? control = GetControl(windowHandle, controlHandle, useUiAutomation, includeUiAutomation);
        if (control == null) {
            return null;
        }

        return GetControlState(control);
    }

    /// <summary>
    /// Gets the current foreground window when it can be resolved.
    /// </summary>
    public WindowInfo? GetActiveWindow(bool includeHidden = true, bool includeCloaked = true, bool includeOwned = true, bool includeEmptyTitles = true) {
        return _windowManager.GetActiveWindow(includeHidden, includeCloaked, includeOwned, includeEmptyTitles);
    }

    /// <summary>
    /// Determines whether at least one window matches the supplied query.
    /// </summary>
    public bool WindowExists(WindowQueryOptions options) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        return _windowManager.GetWindows(options).Count > 0;
    }

    /// <summary>
    /// Determines whether the current active window matches the supplied query.
    /// </summary>
    public bool ActiveWindowMatches(WindowQueryOptions options) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        WindowQueryOptions activeWindowOptions = new WindowQueryOptions {
            TitlePattern = options.TitlePattern,
            ProcessNamePattern = options.ProcessNamePattern,
            ClassNamePattern = options.ClassNamePattern,
            TitleRegex = options.TitleRegex,
            ProcessId = options.ProcessId,
            Handle = options.Handle,
            ActiveWindow = true,
            IncludeEmptyTitles = options.IncludeEmptyTitles ?? true,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IsVisible = options.IsVisible,
            State = options.State,
            IsTopMost = options.IsTopMost,
            ZOrderMin = options.ZOrderMin,
            ZOrderMax = options.ZOrderMax
        };

        return _windowManager.GetWindows(activeWindowOptions).Count > 0;
    }

    /// <summary>
    /// Observes the currently focused control for the first matching window when it can be resolved.
    /// </summary>
    public DesktopFocusedControlObservation? GetFocusedControlObservation(WindowQueryOptions options) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        WindowInfo? window = TryResolveSingleWindow(options);
        if (window == null) {
            return null;
        }

        IntPtr focusedHandle = WindowActivationService.GetFocusedControlHandle(window.Handle);
        if (focusedHandle == IntPtr.Zero) {
            return null;
        }

        WindowControlInfo? control = _windowManager.GetControls(
            window,
            new WindowControlQueryOptions {
                Handle = focusedHandle,
                UseUiAutomation = true,
                IncludeUiAutomation = true
            }).FirstOrDefault();

        string liveText = WindowTextHelper.GetWindowText(focusedHandle);
        string controlText = !string.IsNullOrWhiteSpace(liveText)
            ? liveText
            : control?.Text ?? string.Empty;

        return new DesktopFocusedControlObservation {
            WindowHandle = window.Handle,
            WindowTitle = window.Title,
            FocusedHandle = focusedHandle,
            ClassName = control?.ClassName ?? string.Empty,
            AutomationId = control?.AutomationId ?? string.Empty,
            ControlType = control?.ControlType ?? string.Empty,
            Text = controlText,
            Value = control?.Value ?? string.Empty,
            IsKeyboardFocusable = control?.IsKeyboardFocusable,
            IsEnabled = control?.IsEnabled
        };
    }

    /// <summary>
    /// Gets the observable state for a resolved control.
    /// </summary>
    /// <param name="control">Control to inspect.</param>
    /// <returns>The current control state.</returns>
    public DesktopControlState GetControlState(WindowControlInfo control) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        WindowInfo window = ResolveParentWindow(control);
        return CreateControlState(window, control);
    }

    /// <summary>
    /// Observes the currently focused control for a specific window handle when it can be resolved.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    /// <returns>The focused-control observation when available; otherwise null.</returns>
    public DesktopFocusedControlObservation? GetFocusedControlObservation(IntPtr windowHandle) {
        if (windowHandle == IntPtr.Zero) {
            throw new ArgumentException("Invalid window handle.", nameof(windowHandle));
        }

        return GetFocusedControlObservation(new WindowQueryOptions {
            Handle = windowHandle,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        });
    }

    /// <summary>
    /// Activates a resolved window.
    /// </summary>
    /// <param name="window">Window to activate.</param>
    public void ActivateWindow(WindowInfo window) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        _windowManager.ActivateWindow(window);
    }

    /// <summary>
    /// Activates a specific window handle.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    public void ActivateWindow(IntPtr windowHandle) {
        ActivateWindow(ResolveWindowByHandle(windowHandle));
    }

    /// <summary>
    /// Minimizes a resolved window.
    /// </summary>
    /// <param name="window">Window to minimize.</param>
    public void MinimizeWindow(WindowInfo window) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        _windowManager.MinimizeWindow(window);
    }

    /// <summary>
    /// Minimizes a specific window handle.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    public void MinimizeWindow(IntPtr windowHandle) {
        MinimizeWindow(ResolveWindowByHandle(windowHandle));
    }

    /// <summary>
    /// Maximizes a resolved window.
    /// </summary>
    /// <param name="window">Window to maximize.</param>
    public void MaximizeWindow(WindowInfo window) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        _windowManager.MaximizeWindow(window);
    }

    /// <summary>
    /// Maximizes a specific window handle.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    public void MaximizeWindow(IntPtr windowHandle) {
        MaximizeWindow(ResolveWindowByHandle(windowHandle));
    }

    /// <summary>
    /// Restores a resolved window.
    /// </summary>
    /// <param name="window">Window to restore.</param>
    public void RestoreWindow(WindowInfo window) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        _windowManager.RestoreWindow(window);
    }

    /// <summary>
    /// Restores a specific window handle.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    public void RestoreWindow(IntPtr windowHandle) {
        RestoreWindow(ResolveWindowByHandle(windowHandle));
    }

    /// <summary>
    /// Closes a resolved window.
    /// </summary>
    /// <param name="window">Window to close.</param>
    public void CloseWindow(WindowInfo window) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        _windowManager.CloseWindow(window);
    }

    /// <summary>
    /// Closes a specific window handle.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    public void CloseWindow(IntPtr windowHandle) {
        CloseWindow(ResolveWindowByHandle(windowHandle));
    }

    /// <summary>
    /// Snaps a resolved window to a predefined monitor region.
    /// </summary>
    /// <param name="window">Window to snap.</param>
    /// <param name="position">Snap position.</param>
    public void SnapWindow(WindowInfo window, SnapPosition position) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        _windowManager.SnapWindow(window, position);
    }

    /// <summary>
    /// Snaps a specific window handle to a predefined monitor region.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    /// <param name="position">Snap position.</param>
    public void SnapWindow(IntPtr windowHandle, SnapPosition position) {
        SnapWindow(ResolveWindowByHandle(windowHandle), position);
    }

    /// <summary>
    /// Moves a resolved window to a specific monitor.
    /// </summary>
    /// <param name="window">Window to move.</param>
    /// <param name="monitorIndex">Monitor index.</param>
    /// <returns>True when the window moved to a different monitor; otherwise false.</returns>
    public bool MoveWindowToMonitor(WindowInfo window, int monitorIndex) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        Monitor monitor = _monitors.GetMonitors(connectedOnly: true, index: monitorIndex, refresh: true).FirstOrDefault()
            ?? throw new InvalidOperationException($"Monitor '{monitorIndex}' was not found.");
        return _windowManager.MoveWindowToMonitor(window, monitor);
    }

    /// <summary>
    /// Moves a specific window handle to a specific monitor.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    /// <param name="monitorIndex">Monitor index.</param>
    /// <returns>True when the window moved to a different monitor; otherwise false.</returns>
    public bool MoveWindowToMonitor(IntPtr windowHandle, int monitorIndex) {
        return MoveWindowToMonitor(ResolveWindowByHandle(windowHandle), monitorIndex);
    }

    /// <summary>
    /// Applies a reusable reliable placement request to a desktop window.
    /// </summary>
    /// <param name="request">Placement request describing the target window, monitor, geometry, verification, and retry behavior.</param>
    /// <returns>The observed placement result, including final window state and diagnostic snapshots.</returns>
    public WindowPlacementResult ApplyWindowPlacement(WindowPlacementRequest request) {
        return new WindowPlacementService(_windowManager, _monitors).Apply(request);
    }

    /// <summary>
    /// Sets whether a resolved window should stay topmost.
    /// </summary>
    /// <param name="window">Window to update.</param>
    /// <param name="topMost">True to make the window topmost; otherwise false.</param>
    public void SetWindowTopMost(WindowInfo window, bool topMost) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        _windowManager.SetWindowTopMost(window, topMost);
    }

    /// <summary>
    /// Sets whether a specific window handle should stay topmost.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    /// <param name="topMost">True to make the window topmost; otherwise false.</param>
    public void SetWindowTopMost(IntPtr windowHandle, bool topMost) {
        SetWindowTopMost(ResolveWindowByHandle(windowHandle), topMost);
    }

    /// <summary>
    /// Shows or hides a resolved window.
    /// </summary>
    /// <param name="window">Window to update.</param>
    /// <param name="visible">True to show the window; otherwise false.</param>
    public void SetWindowVisibility(WindowInfo window, bool visible) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        _windowManager.ShowWindow(window, visible);
    }

    /// <summary>
    /// Shows or hides a specific window handle.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    /// <param name="visible">True to show the window; otherwise false.</param>
    public void SetWindowVisibility(IntPtr windowHandle, bool visible) {
        SetWindowVisibility(ResolveWindowByHandle(windowHandle), visible);
    }

    /// <summary>
    /// Sets the transparency level of a resolved window.
    /// </summary>
    /// <param name="window">Window to update.</param>
    /// <param name="alpha">Transparency alpha from 0 (transparent) to 255 (opaque).</param>
    public void SetWindowTransparency(WindowInfo window, byte alpha) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        _windowManager.SetWindowTransparency(window, alpha);
    }

    /// <summary>
    /// Sets the transparency level of a specific window handle.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    /// <param name="alpha">Transparency alpha from 0 (transparent) to 255 (opaque).</param>
    public void SetWindowTransparency(IntPtr windowHandle, byte alpha) {
        SetWindowTransparency(ResolveWindowByHandle(windowHandle), alpha);
    }

    /// <summary>
    /// Adds or removes style bits on a resolved window.
    /// </summary>
    /// <param name="window">Window to update.</param>
    /// <param name="flags">Style flags to modify.</param>
    /// <param name="enable">True to set bits; otherwise false.</param>
    /// <param name="extended">True to update extended style bits.</param>
    public void SetWindowStyle(WindowInfo window, long flags, bool enable, bool extended = false) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        _windowManager.SetWindowStyle(window, flags, enable, extended);
    }

    /// <summary>
    /// Adds or removes style bits on a specific window handle.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    /// <param name="flags">Style flags to modify.</param>
    /// <param name="enable">True to set bits; otherwise false.</param>
    /// <param name="extended">True to update extended style bits.</param>
    public void SetWindowStyle(IntPtr windowHandle, long flags, bool enable, bool extended = false) {
        SetWindowStyle(ResolveWindowByHandle(windowHandle), flags, enable, extended);
    }

    /// <summary>
    /// Terminates the process that owns a resolved window.
    /// </summary>
    /// <param name="window">Window whose owning process should be terminated.</param>
    /// <param name="entireProcessTree">Whether to terminate the full process tree.</param>
    /// <param name="waitForExitMilliseconds">How long to wait for the process to exit.</param>
    /// <returns>The termination result.</returns>
    public DesktopProcessTerminationResult TerminateWindowProcess(WindowInfo window, bool entireProcessTree = false, int waitForExitMilliseconds = 5000) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        if (window.ProcessId == 0) {
            throw new InvalidOperationException("The requested window does not expose a valid owning process.");
        }

        if (waitForExitMilliseconds < 0) {
            throw new ArgumentOutOfRangeException(nameof(waitForExitMilliseconds), "waitForExitMilliseconds must be zero or greater.");
        }

        using Process process = Process.GetProcessById((int)window.ProcessId);
        string processName = string.IsNullOrWhiteSpace(process.ProcessName)
            ? window.Title
            : process.ProcessName;

        KillProcess(process, entireProcessTree);
        bool exited = process.WaitForExit(waitForExitMilliseconds);
        if (!exited) {
            throw new TimeoutException($"Timed out waiting for process '{processName}' to exit.");
        }

        return new DesktopProcessTerminationResult {
            ProcessId = (int)window.ProcessId,
            ProcessName = processName,
            WindowTitle = window.Title,
            HasExited = true,
            EntireProcessTree = entireProcessTree,
            WaitForExitMilliseconds = waitForExitMilliseconds
        };
    }

    /// <summary>
    /// Terminates the process that owns a specific window handle.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    /// <param name="entireProcessTree">Whether to terminate the full process tree.</param>
    /// <param name="waitForExitMilliseconds">How long to wait for the process to exit.</param>
    /// <returns>The termination result.</returns>
    public DesktopProcessTerminationResult TerminateWindowProcess(IntPtr windowHandle, bool entireProcessTree = false, int waitForExitMilliseconds = 5000) {
        return TerminateWindowProcess(ResolveWindowByHandle(windowHandle), entireProcessTree, waitForExitMilliseconds);
    }

    private static void KillProcess(Process process, bool entireProcessTree) {
#if NET5_0_OR_GREATER
        process.Kill(entireProcessTree);
#else
        process.Kill();
#endif
    }

    /// <summary>
    /// Observes the best available text source for the first matching window.
    /// </summary>
    /// <param name="options">Window selection options.</param>
    /// <param name="expectedText">Optional text that should be preferred when present.</param>
    /// <param name="observationOptions">Observation configuration.</param>
    /// <returns>The best matching text observation when one is available; otherwise null.</returns>
    public DesktopWindowTextObservation? ObserveWindowText(WindowQueryOptions options, string? expectedText = null, DesktopTextObservationOptions? observationOptions = null) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        DesktopTextObservationOptions settings = observationOptions ?? new DesktopTextObservationOptions();
        ValidateTextObservationOptions(settings);

        WindowInfo? window = TryResolveSingleWindow(options);
        if (window == null) {
            return null;
        }

        DesktopWindowTextObservation? fallbackObservation = null;
        for (int attempt = 0; attempt < settings.RetryCount; attempt++) {
            DesktopWindowTextObservation? observation = TryObserveWindowText(window, expectedText, settings);
            if (observation?.ContainsExpected == true) {
                return observation;
            }

            fallbackObservation ??= observation;
            if (string.IsNullOrEmpty(expectedText) && fallbackObservation != null) {
                return fallbackObservation;
            }

            if (attempt < settings.RetryCount - 1 && settings.RetryDelayMilliseconds > 0) {
                Thread.Sleep(settings.RetryDelayMilliseconds);
            }
        }

        return fallbackObservation;
    }

    /// <summary>
    /// Observes the best available text source for a specific window handle.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    /// <param name="expectedText">Optional text that should be preferred when present.</param>
    /// <param name="observationOptions">Observation configuration.</param>
    /// <returns>The best matching text observation when one is available; otherwise null.</returns>
    public DesktopWindowTextObservation? ObserveWindowText(IntPtr windowHandle, string? expectedText = null, DesktopTextObservationOptions? observationOptions = null) {
        if (windowHandle == IntPtr.Zero) {
            throw new ArgumentException("Invalid window handle.", nameof(windowHandle));
        }

        return ObserveWindowText(new WindowQueryOptions {
            Handle = windowHandle,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        }, expectedText, observationOptions);
    }

    /// <summary>
    /// Focuses a resolved control and returns its updated observable state.
    /// </summary>
    /// <param name="control">Control to focus.</param>
    /// <param name="ensureForegroundWindow">Whether to ensure the parent window is foreground first.</param>
    /// <returns>The updated control state.</returns>
    public DesktopControlState FocusControl(WindowControlInfo control, bool ensureForegroundWindow = false) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        WindowInfo window = ResolveParentWindow(control);
        bool focused;
        if (control.Source == WindowControlSource.UiAutomation && control.Handle == IntPtr.Zero) {
            focused = new UiAutomationControlService().TryFocus(window, control, ensureForegroundWindow);
        } else {
            EnsureControlSupportsNativeStateChange(control, "focused");
            focused = WindowActivationService.TryFocusControl(window.Handle, control.Handle, ensureForegroundWindow);
        }

        if (!focused) {
            throw new InvalidOperationException("Failed to focus the specified control.");
        }

        return CreateControlState(window, control);
    }

    /// <summary>
    /// Focuses a control resolved by window and control handle and returns its updated state.
    /// </summary>
    /// <param name="windowHandle">Parent window handle.</param>
    /// <param name="controlHandle">Control handle.</param>
    /// <param name="ensureForegroundWindow">Whether to ensure the parent window is foreground first.</param>
    /// <param name="useUiAutomation">Whether to request UI Automation discovery.</param>
    /// <param name="includeUiAutomation">Whether to combine Win32 and UI Automation discovery.</param>
    /// <returns>The updated control state.</returns>
    public DesktopControlState FocusControl(IntPtr windowHandle, IntPtr controlHandle, bool ensureForegroundWindow = false, bool useUiAutomation = true, bool includeUiAutomation = true) {
        WindowControlInfo? control = GetControl(windowHandle, controlHandle, useUiAutomation, includeUiAutomation);
        if (control == null) {
            throw new InvalidOperationException("Failed to resolve the requested control.");
        }

        return FocusControl(control, ensureForegroundWindow);
    }

    /// <summary>
    /// Enables or disables a resolved control and returns its updated state.
    /// </summary>
    /// <param name="control">Control to modify.</param>
    /// <param name="enabled">True to enable the control; false to disable it.</param>
    /// <returns>The updated control state.</returns>
    public DesktopControlState SetControlEnabled(WindowControlInfo control, bool enabled) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        EnsureControlSupportsNativeStateChange(control, enabled ? "enabled" : "disabled");
        WindowControlService.SetEnabled(control, enabled);
        return CreateControlState(ResolveParentWindow(control), control);
    }

    /// <summary>
    /// Enables or disables a control resolved by window and control handle and returns its updated state.
    /// </summary>
    /// <param name="windowHandle">Parent window handle.</param>
    /// <param name="controlHandle">Control handle.</param>
    /// <param name="enabled">True to enable the control; false to disable it.</param>
    /// <param name="useUiAutomation">Whether to request UI Automation discovery.</param>
    /// <param name="includeUiAutomation">Whether to combine Win32 and UI Automation discovery.</param>
    /// <returns>The updated control state.</returns>
    public DesktopControlState SetControlEnabled(IntPtr windowHandle, IntPtr controlHandle, bool enabled, bool useUiAutomation = true, bool includeUiAutomation = true) {
        WindowControlInfo? control = GetControl(windowHandle, controlHandle, useUiAutomation, includeUiAutomation);
        if (control == null) {
            throw new InvalidOperationException("Failed to resolve the requested control.");
        }

        return SetControlEnabled(control, enabled);
    }

    /// <summary>
    /// Shows or hides a resolved control and returns its updated state.
    /// </summary>
    /// <param name="control">Control to modify.</param>
    /// <param name="visible">True to show the control; false to hide it.</param>
    /// <returns>The updated control state.</returns>
    public DesktopControlState SetControlVisibility(WindowControlInfo control, bool visible) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        EnsureControlSupportsNativeStateChange(control, visible ? "shown" : "hidden");
        WindowControlService.SetVisibility(control, visible);
        return CreateControlState(ResolveParentWindow(control), control);
    }

    /// <summary>
    /// Shows or hides a control resolved by window and control handle and returns its updated state.
    /// </summary>
    /// <param name="windowHandle">Parent window handle.</param>
    /// <param name="controlHandle">Control handle.</param>
    /// <param name="visible">True to show the control; false to hide it.</param>
    /// <param name="useUiAutomation">Whether to request UI Automation discovery.</param>
    /// <param name="includeUiAutomation">Whether to combine Win32 and UI Automation discovery.</param>
    /// <returns>The updated control state.</returns>
    public DesktopControlState SetControlVisibility(IntPtr windowHandle, IntPtr controlHandle, bool visible, bool useUiAutomation = true, bool includeUiAutomation = true) {
        WindowControlInfo? control = GetControl(windowHandle, controlHandle, useUiAutomation, includeUiAutomation);
        if (control == null) {
            throw new InvalidOperationException("Failed to resolve the requested control.");
        }

        return SetControlVisibility(control, visible);
    }

    /// <summary>
    /// Gets the current check state for a resolved control.
    /// </summary>
    /// <param name="control">Control to inspect.</param>
    /// <returns><c>true</c> when the control is checked; otherwise <c>false</c>.</returns>
    public bool GetControlCheckState(WindowControlInfo control) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        if (control.Source == WindowControlSource.UiAutomation && control.Handle == IntPtr.Zero) {
            WindowInfo window = ResolveParentWindow(control);
            bool? checkState = new UiAutomationControlService().TryReadCheckState(window, control);
            if (!checkState.HasValue) {
                throw new InvalidOperationException("The selected UI Automation control did not report a readable check state.");
            }

            return checkState.Value;
        }

        EnsureControlSupportsNativeCheckState(control, "queried for check state");
        return WindowControlService.GetCheckState(control);
    }

    /// <summary>
    /// Gets the current check state for a control resolved by window and control handle.
    /// </summary>
    /// <param name="windowHandle">Parent window handle.</param>
    /// <param name="controlHandle">Control handle.</param>
    /// <param name="useUiAutomation">Whether to request UI Automation discovery.</param>
    /// <param name="includeUiAutomation">Whether to combine Win32 and UI Automation discovery.</param>
    /// <returns>The current check state when the control can be resolved; otherwise null.</returns>
    public bool? GetControlCheckState(IntPtr windowHandle, IntPtr controlHandle, bool useUiAutomation = true, bool includeUiAutomation = true) {
        WindowControlInfo? control = GetControl(windowHandle, controlHandle, useUiAutomation, includeUiAutomation);
        if (control == null) {
            return null;
        }

        return GetControlCheckState(control);
    }

    /// <summary>
    /// Sets the current check state for a resolved control.
    /// </summary>
    /// <param name="control">Control to update.</param>
    /// <param name="check">Desired check state.</param>
    public void SetControlCheckState(WindowControlInfo control, bool check) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        if (control.Source == WindowControlSource.UiAutomation && control.Handle == IntPtr.Zero) {
            WindowInfo window = ResolveParentWindow(control);
            if (!new UiAutomationControlService().TrySetCheckState(window, control, check)) {
                throw new InvalidOperationException("The UI Automation control check state could not be updated.");
            }

            return;
        }

        EnsureControlSupportsNativeCheckState(control, check ? "checked" : "unchecked");
        WindowControlService.SetCheckState(control, check);
    }

    /// <summary>
    /// Sets the current check state for a control resolved by window and control handle.
    /// </summary>
    /// <param name="windowHandle">Parent window handle.</param>
    /// <param name="controlHandle">Control handle.</param>
    /// <param name="check">Desired check state.</param>
    /// <param name="useUiAutomation">Whether to request UI Automation discovery.</param>
    /// <param name="includeUiAutomation">Whether to combine Win32 and UI Automation discovery.</param>
    public void SetControlCheckState(IntPtr windowHandle, IntPtr controlHandle, bool check, bool useUiAutomation = true, bool includeUiAutomation = true) {
        WindowControlInfo? control = GetControl(windowHandle, controlHandle, useUiAutomation, includeUiAutomation);
        if (control == null) {
            throw new InvalidOperationException("Failed to resolve the requested control.");
        }

        SetControlCheckState(control, check);
    }

    /// <summary>
    /// Selects a combo-box-style item for a resolved control by its displayed text.
    /// </summary>
    /// <param name="control">Control to update.</param>
    /// <param name="selectedValue">Displayed item text to select.</param>
    public void SetControlSelectedValue(WindowControlInfo control, string selectedValue) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        if (selectedValue == null) {
            throw new ArgumentNullException(nameof(selectedValue));
        }

        if (control.Source == WindowControlSource.UiAutomation && control.Handle == IntPtr.Zero) {
            WindowInfo window = ResolveParentWindow(control);
            if (!new UiAutomationControlService().TrySetSelectedValue(window, control, selectedValue)) {
                throw new InvalidOperationException("The UI Automation control selection could not be updated.");
            }

            return;
        }

        EnsureControlSupportsNativeSelection(control, "selected generically");
        WindowControlService.SetSelectedValue(control, selectedValue);
    }

    /// <summary>
    /// Selects a combo-box-style item for a control resolved by window and control handle.
    /// </summary>
    /// <param name="windowHandle">Parent window handle.</param>
    /// <param name="controlHandle">Control handle.</param>
    /// <param name="selectedValue">Displayed item text to select.</param>
    /// <param name="useUiAutomation">Whether to request UI Automation discovery.</param>
    /// <param name="includeUiAutomation">Whether to combine Win32 and UI Automation discovery.</param>
    public void SetControlSelectedValue(IntPtr windowHandle, IntPtr controlHandle, string selectedValue, bool useUiAutomation = true, bool includeUiAutomation = true) {
        WindowControlInfo? control = GetControl(windowHandle, controlHandle, useUiAutomation, includeUiAutomation);
        if (control == null) {
            throw new InvalidOperationException("Failed to resolve the requested control.");
        }

        SetControlSelectedValue(control, selectedValue);
    }

    /// <summary>
    /// Gets the current mouse state.
    /// </summary>
    /// <returns>The current mouse state.</returns>
    public DesktopMouseState GetMouseState() {
        return MouseInputService.GetState();
    }

    /// <summary>
    /// Moves the mouse cursor to the requested screen coordinates.
    /// </summary>
    /// <param name="x">Screen X coordinate.</param>
    /// <param name="y">Screen Y coordinate.</param>
    public void MoveMouse(int x, int y) {
        MouseInputService.MoveCursor(x, y);
    }

    /// <summary>
    /// Clicks the mouse using the requested button.
    /// </summary>
    /// <param name="button">Mouse button to click.</param>
    public void ClickMouse(MouseButton button) {
        MouseInputService.Click(button);
    }

    /// <summary>
    /// Scrolls the mouse wheel by the requested amount.
    /// </summary>
    /// <param name="delta">Wheel delta.</param>
    public void ScrollMouse(int delta) {
        MouseInputService.Scroll(delta);
    }

    /// <summary>
    /// Drags the mouse between two points.
    /// </summary>
    /// <param name="button">Mouse button to hold.</param>
    /// <param name="startX">Starting screen X coordinate.</param>
    /// <param name="startY">Starting screen Y coordinate.</param>
    /// <param name="endX">Ending screen X coordinate.</param>
    /// <param name="endY">Ending screen Y coordinate.</param>
    /// <param name="stepDelayMilliseconds">Delay in milliseconds between drag steps.</param>
    public void DragMouse(MouseButton button, int startX, int startY, int endX, int endY, int stepDelayMilliseconds) {
        MouseInputService.MouseDrag(button, startX, startY, endX, endY, stepDelayMilliseconds);
    }

    /// <summary>
    /// Starts sending keep-alive messages to matching windows.
    /// </summary>
    /// <param name="options">Window selection options.</param>
    /// <param name="interval">Interval between keep-alive messages.</param>
    /// <param name="all">Whether to apply to all matching windows.</param>
    /// <returns>The windows now under keep-alive.</returns>
    public IReadOnlyList<WindowInfo> StartWindowKeepAlive(WindowQueryOptions options, TimeSpan interval, bool all = false) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);
        foreach (WindowInfo window in windows) {
            WindowKeepAlive.Instance.Start(window, interval);
        }

        return RefreshWindows(windows);
    }

    /// <summary>
    /// Starts sending keep-alive messages to a specific window handle.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    /// <param name="interval">Interval between keep-alive messages.</param>
    /// <returns>The refreshed window state.</returns>
    public WindowInfo StartWindowKeepAlive(IntPtr windowHandle, TimeSpan interval) {
        if (windowHandle == IntPtr.Zero) {
            throw new ArgumentException("Invalid window handle.", nameof(windowHandle));
        }

        return StartWindowKeepAlive(new WindowQueryOptions {
            Handle = windowHandle,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        }, interval, all: false)[0];
    }

    /// <summary>
    /// Stops keep-alive messages for matching windows.
    /// </summary>
    /// <param name="options">Window selection options.</param>
    /// <param name="all">Whether to apply to all matching windows.</param>
    /// <returns>The windows that were targeted for keep-alive stop.</returns>
    public IReadOnlyList<WindowInfo> StopWindowKeepAlive(WindowQueryOptions options, bool all = false) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);
        foreach (WindowInfo window in windows) {
            WindowKeepAlive.Instance.Stop(window.Handle);
        }

        return windows;
    }

    /// <summary>
    /// Stops keep-alive messages for a specific window handle.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    public void StopWindowKeepAlive(IntPtr windowHandle) {
        if (windowHandle == IntPtr.Zero) {
            throw new ArgumentException("Invalid window handle.", nameof(windowHandle));
        }

        WindowKeepAlive.Instance.Stop(windowHandle);
    }

    /// <summary>
    /// Stops all keep-alive sessions.
    /// </summary>
    public void StopAllWindowKeepAlive() {
        WindowKeepAlive.Instance.StopAll();
    }

    /// <summary>
    /// Lists windows that currently have active keep-alive sessions.
    /// </summary>
    /// <returns>The windows currently under keep-alive.</returns>
    public IReadOnlyList<WindowInfo> GetWindowKeepAliveWindows() {
        List<WindowInfo> windows = new();
        foreach (IntPtr handle in WindowKeepAlive.Instance.ActiveHandles) {
            WindowInfo? window = GetWindow(handle, includeHidden: true, includeCloaked: true, includeOwned: true, includeEmptyTitles: true);
            if (window != null) {
                windows.Add(window);
            }
        }

        return windows;
    }

    /// <summary>
    /// Moves and optionally resizes matching windows.
    /// </summary>
    public IReadOnlyList<WindowInfo> MoveWindows(WindowQueryOptions options, int? monitorIndex, int? x, int? y, int? width, int? height, bool activate, bool all = false) {
        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);

        Monitor? monitor = null;
        if (monitorIndex.HasValue) {
            monitor = _monitors.GetMonitors(index: monitorIndex.Value).FirstOrDefault();
            if (monitor == null) {
                throw new InvalidOperationException($"Monitor with index {monitorIndex.Value} was not found.");
            }
        }

        foreach (WindowInfo window in windows) {
            if (monitor != null) {
                _windowManager.MoveWindowToMonitor(window, monitor);
            }

            if (x.HasValue || y.HasValue || width.HasValue || height.HasValue) {
                _windowManager.SetWindowPosition(window, x ?? -1, y ?? -1, width ?? -1, height ?? -1);
            }

            if (activate) {
                _windowManager.ActivateWindow(window);
            }
        }

        return RefreshWindows(windows);
    }

    /// <summary>
    /// Focuses matching windows.
    /// </summary>
    public IReadOnlyList<WindowInfo> FocusWindows(WindowQueryOptions options, bool all = false) {
        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);
        foreach (WindowInfo window in windows) {
            _windowManager.ActivateWindow(window);
        }

        return RefreshWindows(windows);
    }

    /// <summary>
    /// Minimizes matching windows.
    /// </summary>
    public IReadOnlyList<WindowInfo> MinimizeWindows(WindowQueryOptions options, bool all = false) {
        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);
        foreach (WindowInfo window in windows) {
            _windowManager.MinimizeWindow(window);
        }

        return RefreshWindows(windows);
    }

    /// <summary>
    /// Snaps matching windows to a predefined position.
    /// </summary>
    public IReadOnlyList<WindowInfo> SnapWindows(WindowQueryOptions options, SnapPosition position, bool all = false) {
        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);
        foreach (WindowInfo window in windows) {
            _windowManager.SnapWindow(window, position);
        }

        return RefreshWindows(windows);
    }

    /// <summary>
    /// Gets window and client-area geometry for one or more matching windows.
    /// </summary>
    public IReadOnlyList<DesktopWindowGeometry> GetWindowGeometry(WindowQueryOptions options, bool all = false) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        return ResolveWindows(options, all)
            .Select(DescribeWindowGeometry)
            .ToArray();
    }

    /// <summary>
    /// Gets window and client-area geometry for a specific window handle.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    /// <returns>The matching window geometry.</returns>
    public DesktopWindowGeometry GetWindowGeometry(IntPtr windowHandle) {
        return DescribeWindowGeometry(ResolveWindowByHandle(windowHandle));
    }

    /// <summary>
    /// Pastes text to matching windows.
    /// </summary>
    public IReadOnlyList<WindowInfo> PasteWindowText(WindowQueryOptions options, string text, WindowInputOptions? inputOptions = null, bool all = false) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }
        if (text == null) {
            throw new ArgumentNullException(nameof(text));
        }

        WindowInputOptions settings = inputOptions ?? new WindowInputOptions();
        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);
        foreach (WindowInfo window in windows) {
            _windowManager.PasteText(window, text, settings);
        }

        return RefreshWindows(windows);
    }

    /// <summary>
    /// Pastes text to a specific window handle.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    /// <param name="text">Text to paste.</param>
    /// <param name="inputOptions">Input behavior overrides.</param>
    /// <returns>The refreshed window state.</returns>
    public WindowInfo PasteWindowText(IntPtr windowHandle, string text, WindowInputOptions? inputOptions = null) {
        if (windowHandle == IntPtr.Zero) {
            throw new ArgumentException("Invalid window handle.", nameof(windowHandle));
        }

        return PasteWindowText(new WindowQueryOptions {
            Handle = windowHandle,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        }, text, inputOptions, all: false)[0];
    }

    /// <summary>
    /// Sends text to matching windows.
    /// </summary>
    public IReadOnlyList<WindowInfo> TypeWindowText(WindowQueryOptions options, string text, bool paste, int delayMilliseconds, bool foregroundInput, bool physicalKeys, bool hostedSession, bool script, int scriptChunkLength, int scriptLineDelayMilliseconds, bool all = false) {
        if (text == null) {
            throw new ArgumentNullException(nameof(text));
        }

        WindowInputOptions settings = new WindowInputOptions {
            KeyDelayMilliseconds = delayMilliseconds,
            RequireForegroundWindowForTyping = foregroundInput,
            UsePhysicalKeyboardLayout = physicalKeys,
            UseHostedSessionScanCodes = hostedSession,
            TypeTextAsScript = script,
            ScriptChunkLength = scriptChunkLength,
            ScriptLineDelayMilliseconds = scriptLineDelayMilliseconds
        };

        return paste
            ? PasteWindowText(options, text, settings, all)
            : TypeWindowText(options, text, settings, all);
    }

    /// <summary>
    /// Sends text to matching windows using explicit input options.
    /// </summary>
    public IReadOnlyList<WindowInfo> TypeWindowText(WindowQueryOptions options, string text, WindowInputOptions? inputOptions = null, bool all = false) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }
        if (text == null) {
            throw new ArgumentNullException(nameof(text));
        }

        WindowInputOptions settings = inputOptions ?? new WindowInputOptions();
        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);
        foreach (WindowInfo window in windows) {
            _windowManager.TypeText(window, text, settings);
        }

        return RefreshWindows(windows);
    }

    /// <summary>
    /// Sends text to a specific window handle.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    /// <param name="text">Text to send.</param>
    /// <param name="paste">Whether to paste clipboard text instead of typing.</param>
    /// <param name="delayMilliseconds">Delay between key presses when typing.</param>
    /// <param name="foregroundInput">Whether the target must own the foreground during typing.</param>
    /// <param name="physicalKeys">Whether to use the physical keyboard layout for typing.</param>
    /// <param name="hostedSession">Whether to send hosted-session scan codes.</param>
    /// <param name="script">Whether to send the text as script chunks.</param>
    /// <param name="scriptChunkLength">Maximum script chunk length.</param>
    /// <param name="scriptLineDelayMilliseconds">Delay between script lines.</param>
    /// <returns>The refreshed window state.</returns>
    public WindowInfo TypeWindowText(IntPtr windowHandle, string text, bool paste, int delayMilliseconds, bool foregroundInput, bool physicalKeys, bool hostedSession, bool script, int scriptChunkLength, int scriptLineDelayMilliseconds) {
        if (windowHandle == IntPtr.Zero) {
            throw new ArgumentException("Invalid window handle.", nameof(windowHandle));
        }

        return TypeWindowText(new WindowQueryOptions {
            Handle = windowHandle,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        }, text, paste, delayMilliseconds, foregroundInput, physicalKeys, hostedSession, script, scriptChunkLength, scriptLineDelayMilliseconds, all: false)[0];
    }

    /// <summary>
    /// Sends text to a specific window handle using explicit input options.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    /// <param name="text">Text to send.</param>
    /// <param name="inputOptions">Input behavior overrides.</param>
    /// <returns>The refreshed window state.</returns>
    public WindowInfo TypeWindowText(IntPtr windowHandle, string text, WindowInputOptions? inputOptions = null) {
        if (windowHandle == IntPtr.Zero) {
            throw new ArgumentException("Invalid window handle.", nameof(windowHandle));
        }

        return TypeWindowText(new WindowQueryOptions {
            Handle = windowHandle,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        }, text, inputOptions, all: false)[0];
    }

    /// <summary>
    /// Sends keys to matching windows.
    /// </summary>
    public IReadOnlyList<WindowInfo> SendWindowKeys(WindowQueryOptions options, IReadOnlyList<VirtualKey> keys, bool activate, bool all = false) {
        if (keys == null) {
            throw new ArgumentNullException(nameof(keys));
        }
        if (keys.Count == 0) {
            throw new ArgumentException("No keys specified.", nameof(keys));
        }

        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);
        foreach (WindowInfo window in windows) {
            _windowManager.SendKeys(window, keys, new WindowInputOptions {
                ActivateWindow = activate
            });
        }

        return RefreshWindows(windows);
    }

    /// <summary>
    /// Sends keys to a specific window handle.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    /// <param name="keys">Keys to send.</param>
    /// <param name="activate">Whether to activate the window before sending keys.</param>
    /// <returns>The refreshed window state.</returns>
    public WindowInfo SendWindowKeys(IntPtr windowHandle, IReadOnlyList<VirtualKey> keys, bool activate) {
        if (windowHandle == IntPtr.Zero) {
            throw new ArgumentException("Invalid window handle.", nameof(windowHandle));
        }

        return SendWindowKeys(new WindowQueryOptions {
            Handle = windowHandle,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        }, keys, activate, all: false)[0];
    }

    /// <summary>
    /// Clicks a point relative to each matching window.
    /// </summary>
    public IReadOnlyList<WindowInfo> ClickWindowPoint(WindowQueryOptions options, int x, int y, MouseButton button, bool activate, bool all = false) {
        return ClickWindowPoint(options, x, y, null, null, button, activate, clientArea: false, all);
    }

    /// <summary>
    /// Clicks a point relative to each matching window.
    /// </summary>
    public IReadOnlyList<WindowInfo> ClickWindowPoint(WindowQueryOptions options, int x, int y, MouseButton button, bool activate, bool clientArea, bool all = false) {
        return ClickWindowPoint(options, x, y, null, null, button, activate, clientArea, all);
    }

    /// <summary>
    /// Clicks a point relative to each matching window using either pixels or normalized ratios.
    /// </summary>
    public IReadOnlyList<WindowInfo> ClickWindowPoint(WindowQueryOptions options, int? x, int? y, double? xRatio, double? yRatio, MouseButton button, bool activate, bool clientArea, bool all = false) {
        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);
        foreach (WindowInfo window in windows) {
            if (activate) {
                _windowManager.ActivateWindow(window);
                Thread.Sleep(100);
            }

            (int screenX, int screenY) = ResolveWindowPoint(window, x, y, xRatio, yRatio, clientArea);
            Thread.Sleep(50);
            _windowManager.MoveMouse(screenX, screenY);
            _windowManager.ClickMouse(button);
        }

        return RefreshWindows(windows);
    }

    /// <summary>
    /// Drags between two points relative to each matching window.
    /// </summary>
    public IReadOnlyList<WindowInfo> DragWindowPoints(WindowQueryOptions options, int startX, int startY, int endX, int endY, MouseButton button, int stepDelayMilliseconds, bool activate, bool clientArea, bool all = false) {
        return DragWindowPoints(options, startX, startY, null, null, endX, endY, null, null, button, stepDelayMilliseconds, activate, clientArea, all);
    }

    /// <summary>
    /// Drags between two points relative to each matching window using either pixels or normalized ratios.
    /// </summary>
    public IReadOnlyList<WindowInfo> DragWindowPoints(WindowQueryOptions options, int? startX, int? startY, double? startXRatio, double? startYRatio, int? endX, int? endY, double? endXRatio, double? endYRatio, MouseButton button, int stepDelayMilliseconds, bool activate, bool clientArea, bool all = false) {
        if (stepDelayMilliseconds < 0) {
            throw new ArgumentOutOfRangeException(nameof(stepDelayMilliseconds), "stepDelayMilliseconds must be zero or greater.");
        }

        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);
        foreach (WindowInfo window in windows) {
            if (activate) {
                _windowManager.ActivateWindow(window);
                Thread.Sleep(100);
            }

            (int startScreenX, int startScreenY) = ResolveWindowPoint(window, startX, startY, startXRatio, startYRatio, clientArea);
            (int endScreenX, int endScreenY) = ResolveWindowPoint(window, endX, endY, endXRatio, endYRatio, clientArea);
            _windowManager.DragMouse(button, startScreenX, startScreenY, endScreenX, endScreenY, stepDelayMilliseconds);
        }

        return RefreshWindows(windows);
    }

    /// <summary>
    /// Scrolls the mouse wheel at a point relative to each matching window.
    /// </summary>
    public IReadOnlyList<WindowInfo> ScrollWindowPoint(WindowQueryOptions options, int x, int y, int delta, bool activate, bool clientArea, bool all = false) {
        return ScrollWindowPoint(options, x, y, null, null, delta, activate, clientArea, all);
    }

    /// <summary>
    /// Scrolls the mouse wheel at a point relative to each matching window using either pixels or normalized ratios.
    /// </summary>
    public IReadOnlyList<WindowInfo> ScrollWindowPoint(WindowQueryOptions options, int? x, int? y, double? xRatio, double? yRatio, int delta, bool activate, bool clientArea, bool all = false) {
        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);
        foreach (WindowInfo window in windows) {
            if (activate) {
                _windowManager.ActivateWindow(window);
                Thread.Sleep(100);
            }

            (int screenX, int screenY) = ResolveWindowPoint(window, x, y, xRatio, yRatio, clientArea);
            Thread.Sleep(50);
            _windowManager.MoveMouse(screenX, screenY);
            _windowManager.ScrollMouse(delta);
        }

        return RefreshWindows(windows);
    }

    /// <summary>
    /// Saves a reusable window-relative target definition.
    /// </summary>
    public DesktopWindowTargetDefinition SaveWindowTarget(string name, DesktopWindowTargetDefinition definition) {
        if (definition == null) {
            throw new ArgumentNullException(nameof(definition));
        }

        ValidateWindowTargetDefinition(definition);

        string path = DesktopStateStore.GetTargetPath(name);
        File.WriteAllText(path, JsonSerializer.Serialize(definition, TargetSerializerOptions));
        return definition;
    }

    /// <summary>
    /// Gets a previously saved reusable window-relative target definition.
    /// </summary>
    public DesktopWindowTargetDefinition GetWindowTarget(string name) {
        string path = DesktopStateStore.GetTargetPath(name);
        if (!File.Exists(path)) {
            throw new InvalidOperationException($"Named target '{name}' was not found.");
        }

        DesktopWindowTargetDefinition? definition = JsonSerializer.Deserialize<DesktopWindowTargetDefinition>(File.ReadAllText(path));
        if (definition == null) {
            throw new InvalidOperationException($"Named target '{name}' could not be read.");
        }

        ValidateWindowTargetDefinition(definition);
        return definition;
    }

    /// <summary>
    /// Lists saved reusable window-relative target names.
    /// </summary>
    public IReadOnlyList<string> ListWindowTargets() {
        return DesktopStateStore.ListNames("targets");
    }

    /// <summary>
    /// Saves a reusable control target definition.
    /// </summary>
    public DesktopControlTargetDefinition SaveControlTarget(string name, DesktopControlTargetDefinition definition) {
        if (definition == null) {
            throw new ArgumentNullException(nameof(definition));
        }

        ValidateControlTargetDefinition(definition);

        string path = DesktopStateStore.GetControlTargetPath(name);
        File.WriteAllText(path, JsonSerializer.Serialize(definition, TargetSerializerOptions));
        return definition;
    }

    /// <summary>
    /// Gets a previously saved reusable control target definition.
    /// </summary>
    public DesktopControlTargetDefinition GetControlTarget(string name) {
        string path = DesktopStateStore.GetControlTargetPath(name);
        if (!File.Exists(path)) {
            throw new InvalidOperationException($"Named control target '{name}' was not found.");
        }

        DesktopControlTargetDefinition? definition = JsonSerializer.Deserialize<DesktopControlTargetDefinition>(File.ReadAllText(path));
        if (definition == null) {
            throw new InvalidOperationException($"Named control target '{name}' could not be read.");
        }

        ValidateControlTargetDefinition(definition);
        return definition;
    }

    /// <summary>
    /// Lists saved reusable control target names.
    /// </summary>
    public IReadOnlyList<string> ListControlTargets() {
        return DesktopStateStore.ListNames("control-targets");
    }

    /// <summary>
    /// Saves a reusable visual baseline image and metadata definition.
    /// </summary>
    public DesktopVisualBaselineDefinition SaveVisualBaseline(string name, WindowQueryOptions options, string? targetName, bool clientArea, string? description = null) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        WindowInfo window = ResolveSingleWindow(options);
        string? normalizedTargetName = string.IsNullOrWhiteSpace(targetName) ? null : targetName!.Trim();
        using DesktopCapture capture = CaptureWindowForVisualObservation(window.Handle, normalizedTargetName, clientArea);

        string metadataPath = DesktopStateStore.GetVisualBaselinePath(name);
        string imagePath = DesktopStateStore.GetVisualBaselineImagePath(name);
        capture.Bitmap.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);

        var definition = new DesktopVisualBaselineDefinition {
            Description = description,
            TargetName = normalizedTargetName,
            ClientArea = normalizedTargetName == null && clientArea,
            Width = capture.Bitmap.Width,
            Height = capture.Bitmap.Height,
            CreatedUtc = DateTime.UtcNow.ToString("O")
        };

        File.WriteAllText(metadataPath, JsonSerializer.Serialize(definition, TargetSerializerOptions));
        return definition;
    }

    /// <summary>
    /// Gets a previously saved visual baseline definition.
    /// </summary>
    public DesktopVisualBaselineDefinition GetVisualBaseline(string name) {
        string path = DesktopStateStore.GetVisualBaselinePath(name);
        if (!File.Exists(path)) {
            throw new InvalidOperationException($"Named visual baseline '{name}' was not found.");
        }

        DesktopVisualBaselineDefinition? definition = JsonSerializer.Deserialize<DesktopVisualBaselineDefinition>(File.ReadAllText(path));
        if (definition == null) {
            throw new InvalidOperationException($"Named visual baseline '{name}' could not be read.");
        }

        return definition;
    }

    /// <summary>
    /// Lists saved visual baseline names.
    /// </summary>
    public IReadOnlyList<string> ListVisualBaselines() {
        return DesktopStateStore.ListNames("visual-baselines");
    }

    /// <summary>
    /// Compares a live window capture to a saved visual baseline.
    /// </summary>
    public DesktopVisualBaselineAssertionResult AssertVisualBaseline(string name, WindowQueryOptions options, string? targetName = null, bool? clientArea = null, double maxChangedRatio = 0.01, int differenceThreshold = 24) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        if (maxChangedRatio < 0 || maxChangedRatio > 1) {
            throw new ArgumentOutOfRangeException(nameof(maxChangedRatio), "The maximum changed ratio must be between 0 and 1.");
        }

        if (differenceThreshold < 0 || differenceThreshold > 255) {
            throw new ArgumentOutOfRangeException(nameof(differenceThreshold), "The difference threshold must be between 0 and 255.");
        }

        DesktopVisualBaselineDefinition definition = GetVisualBaseline(name);
        string imagePath = DesktopStateStore.GetVisualBaselineImagePath(name);
        if (!File.Exists(imagePath)) {
            throw new InvalidOperationException($"Named visual baseline '{name}' does not have a saved image.");
        }

        WindowInfo window = ResolveSingleWindow(options);
        string? effectiveTargetName = string.IsNullOrWhiteSpace(targetName) ? definition.TargetName : targetName!.Trim();
        bool effectiveClientArea = string.IsNullOrWhiteSpace(effectiveTargetName) && (clientArea ?? definition.ClientArea);
        using Bitmap baselineBitmap = new(imagePath);
        using DesktopCapture currentCapture = CaptureWindowForVisualObservation(window.Handle, effectiveTargetName, effectiveClientArea);
        DesktopVisualDifferenceMetrics metrics = ScreenshotService.CompareBitmaps(
            baselineBitmap,
            currentCapture.Bitmap,
            differenceThreshold);

        return new DesktopVisualBaselineAssertionResult {
            Matched = !metrics.SizeChanged && metrics.ChangedSampleRatio <= maxChangedRatio,
            Baseline = definition,
            Window = currentCapture.Window ?? window,
            Geometry = currentCapture.Geometry ?? DescribeWindowGeometry(window),
            TargetName = effectiveTargetName,
            ClientArea = string.IsNullOrWhiteSpace(effectiveTargetName) && effectiveClientArea,
            MaxChangedRatio = maxChangedRatio,
            DifferenceThreshold = differenceThreshold,
            Metrics = metrics
        };
    }

    /// <summary>
    /// Searches a live window capture for the best location of a saved visual baseline image.
    /// </summary>
    public DesktopVisualBaselineResolveResult ResolveVisualBaseline(string name, WindowQueryOptions options, bool clientArea = true, double maxAverageDifference = 12.0, int differenceThreshold = 24, int scanStep = 8) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        if (maxAverageDifference < 0 || maxAverageDifference > 255) {
            throw new ArgumentOutOfRangeException(nameof(maxAverageDifference), "The maximum average difference must be between 0 and 255.");
        }

        DesktopVisualBaselineDefinition definition = GetVisualBaseline(name);
        string imagePath = DesktopStateStore.GetVisualBaselineImagePath(name);
        if (!File.Exists(imagePath)) {
            throw new InvalidOperationException($"Named visual baseline '{name}' does not have a saved image.");
        }

        WindowInfo window = ResolveSingleWindow(options);
        using Bitmap baselineBitmap = new(imagePath);
        using DesktopCapture searchCapture = CaptureWindowForVisualObservation(window.Handle, targetName: null, clientArea);
        DesktopVisualBitmapMatch match = ScreenshotService.FindBestBitmapMatch(
            baselineBitmap,
            searchCapture.Bitmap,
            differenceThreshold,
            scanStep);

        DesktopWindowGeometry geometry = searchCapture.Geometry ?? DescribeWindowGeometry(window);
        int screenLeft = clientArea ? geometry.ClientLeft + match.RelativeX : geometry.WindowLeft + match.RelativeX;
        int screenTop = clientArea ? geometry.ClientTop + match.RelativeY : geometry.WindowTop + match.RelativeY;

        return new DesktopVisualBaselineResolveResult {
            Matched = !match.Metrics.SizeChanged && match.Metrics.AverageDifference <= maxAverageDifference,
            Baseline = definition,
            Window = searchCapture.Window ?? window,
            Geometry = geometry,
            ClientArea = clientArea,
            MaxAverageDifference = maxAverageDifference,
            DifferenceThreshold = differenceThreshold,
            ScanStep = match.ScanStep,
            EvaluatedPositionCount = match.EvaluatedPositionCount,
            RelativeX = match.RelativeX,
            RelativeY = match.RelativeY,
            Width = match.Width,
            Height = match.Height,
            ScreenX = screenLeft,
            ScreenY = screenTop,
            Metrics = match.Metrics
        };
    }

    /// <summary>
    /// Reads OCR text from a live window, client area, or named target region.
    /// </summary>
    public DesktopWindowTextReadResult ReadWindowText(WindowQueryOptions options, string? targetName = null, bool clientArea = true, string? languageTag = null) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        WindowInfo window = ResolveSingleWindow(options);
        DesktopWindowGeometry geometry = DescribeWindowGeometry(window);
        string? normalizedTargetName = string.IsNullOrWhiteSpace(targetName) ? null : targetName!.Trim();
        DesktopOcrReadResult ocrResult;
        int captureScreenX;
        int captureScreenY;

        if (!string.IsNullOrWhiteSpace(normalizedTargetName)) {
            DesktopResolvedWindowTarget target = ResolveWindowTargets(CreatePinnedWindowQuery(window), normalizedTargetName!, all: false).FirstOrDefault()
                ?? throw new InvalidOperationException($"Named target '{normalizedTargetName}' could not be resolved against the requested window.");
            if (!target.ScreenWidth.HasValue || !target.ScreenHeight.HasValue) {
                throw new InvalidOperationException($"Named target '{normalizedTargetName}' does not define a capture area.");
            }

            using Bitmap bitmap = ScreenshotService.CaptureRegion(target.ScreenX, target.ScreenY, target.ScreenWidth.Value, target.ScreenHeight.Value);
            ocrResult = ScreenshotService.ReadText(bitmap, languageTag);
            geometry = target.Geometry;
            captureScreenX = target.ScreenX;
            captureScreenY = target.ScreenY;
        } else if (clientArea) {
            using DesktopCapture capture = CaptureWindowClientArea(window.Handle);
            ocrResult = ScreenshotService.ReadText(capture.Bitmap, languageTag);
            geometry = capture.Geometry ?? geometry;
            captureScreenX = geometry.ClientLeft;
            captureScreenY = geometry.ClientTop;
        } else {
            using DesktopCapture capture = CaptureWindow(options);
            ocrResult = ScreenshotService.ReadText(capture.Bitmap, languageTag);
            geometry = capture.Geometry ?? geometry;
            captureScreenX = geometry.WindowLeft;
            captureScreenY = geometry.WindowTop;
        }

        return new DesktopWindowTextReadResult {
            Window = window,
            Geometry = geometry,
            TargetName = normalizedTargetName,
            ClientArea = normalizedTargetName == null && clientArea,
            CaptureScreenX = captureScreenX,
            CaptureScreenY = captureScreenY,
            LanguageTag = ocrResult.LanguageTag,
            Text = ocrResult.Text,
            Lines = CloneOcrLines(ocrResult.Lines)
        };
    }

    /// <summary>
    /// Resolves the best OCR text match inside a live window, client area, or named target region.
    /// </summary>
    public DesktopWindowTextResolveResult ResolveWindowText(WindowQueryOptions options, string queryText, bool contains = false, string? targetName = null, bool clientArea = true, string? languageTag = null) {
        DesktopWindowTextReadResult readResult = ReadWindowText(options, targetName, clientArea, languageTag);
        return ResolveWindowTextMatch(readResult, queryText, contains);
    }

    /// <summary>
    /// Resolves the best OCR text match inside a previously captured OCR result.
    /// </summary>
    internal static DesktopWindowTextResolveResult ResolveWindowTextMatch(DesktopWindowTextReadResult source, string queryText, bool contains = false) {
        if (source == null) {
            throw new ArgumentNullException(nameof(source));
        }

        if (string.IsNullOrWhiteSpace(queryText)) {
            throw new ArgumentException("A non-empty OCR query is required.", nameof(queryText));
        }

        string normalizedQuery = NormalizeOcrSearchText(queryText);
        var candidates = new List<(int Priority, int LengthDelta, int Area, string MatchKind, string MatchedText, int X, int Y, int Width, int Height, IReadOnlyList<DesktopOcrWord> Words)>();

        foreach (DesktopOcrLine line in source.Lines) {
            TryAddTextMatchCandidate(candidates, line.Text, "line", line.X, line.Y, line.Width, line.Height, line.Words, normalizedQuery, contains);
            foreach (DesktopOcrWord word in line.Words) {
                TryAddTextMatchCandidate(candidates, word.Text, "word", word.X, word.Y, word.Width, word.Height, new[] { word }, normalizedQuery, contains);
            }
        }

        var unmatched = new DesktopWindowTextResolveResult {
            Matched = false,
            QueryText = queryText.Trim(),
            ContainsMatch = contains,
            Window = source.Window,
            Geometry = source.Geometry,
            TargetName = source.TargetName,
            ClientArea = source.ClientArea,
            LanguageTag = source.LanguageTag,
            CandidateCount = 0
        };

        if (candidates.Count == 0) {
            return unmatched;
        }

        var best = candidates
            .OrderBy(candidate => candidate.Priority)
            .ThenBy(candidate => candidate.LengthDelta)
            .ThenBy(candidate => candidate.Area)
            .ThenBy(candidate => candidate.Y)
            .ThenBy(candidate => candidate.X)
            .First();

        int screenX = source.CaptureScreenX + best.X;
        int screenY = source.CaptureScreenY + best.Y;
        return new DesktopWindowTextResolveResult {
            Matched = true,
            QueryText = queryText.Trim(),
            ContainsMatch = contains,
            Window = source.Window,
            Geometry = source.Geometry,
            TargetName = source.TargetName,
            ClientArea = source.ClientArea,
            LanguageTag = source.LanguageTag,
            MatchKind = best.MatchKind,
            MatchedText = best.MatchedText,
            CandidateCount = candidates.Count,
            RelativeX = best.X,
            RelativeY = best.Y,
            Width = best.Width,
            Height = best.Height,
            ScreenX = screenX,
            ScreenY = screenY,
            ActionX = screenX + Math.Max(0, best.Width / 2),
            ActionY = screenY + Math.Max(0, best.Height / 2),
            Words = CloneOcrWords(best.Words)
        };
    }

    /// <summary>
    /// Resolves a saved control target against one or more matching windows.
    /// </summary>
    public IReadOnlyList<DesktopResolvedControlTarget> ResolveControlTargets(WindowQueryOptions options, string targetName, bool allWindows = false, bool allControls = false) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        DesktopControlTargetDefinition definition = GetControlTarget(targetName);
        WindowControlQueryOptions controlOptions = CreateControlQuery(definition);
        IReadOnlyList<WindowControlTargetInfo> matches = RequireControls(options, controlOptions, allWindows, allControls);
        return matches
            .Select(match => new DesktopResolvedControlTarget {
                Name = targetName,
                Definition = CloneControlTargetDefinition(definition),
                Window = match.Window,
                Control = match.Control
            })
            .ToArray();
    }

    /// <summary>
    /// Gets a saved control target against one or more matching windows.
    /// </summary>
    public IReadOnlyList<WindowControlTargetInfo> GetControlTargets(WindowQueryOptions options, string targetName, bool allWindows = false, bool allControls = true) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        DesktopControlTargetDefinition definition = GetControlTarget(targetName);
        return GetControls(options, CreateControlQuery(definition), allWindows, allControls);
    }

    /// <summary>
    /// Collects control discovery diagnostics for a saved control target.
    /// </summary>
    public IReadOnlyList<DesktopControlDiscoveryDiagnostics> GetControlTargetDiagnostics(WindowQueryOptions options, string targetName, bool allWindows = false, int sampleLimit = 10, bool includeActionProbe = false) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        DesktopControlTargetDefinition definition = GetControlTarget(targetName);
        return GetControlDiagnostics(options, CreateControlQuery(definition), allWindows, sampleLimit, includeActionProbe);
    }

    /// <summary>
    /// Determines whether a saved control target resolves against at least one matching window.
    /// </summary>
    public bool ControlTargetExists(WindowQueryOptions options, string targetName, bool allWindows = false) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        DesktopControlTargetDefinition definition = GetControlTarget(targetName);
        return ControlExists(options, CreateControlQuery(definition), allWindows);
    }

    /// <summary>
    /// Waits for a saved control target to resolve against one or more matching windows.
    /// </summary>
    public DesktopControlWaitResult WaitForControlTarget(WindowQueryOptions options, string targetName, int timeoutMilliseconds, int intervalMilliseconds, bool allWindows = false, bool allControls = false) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        DesktopControlTargetDefinition definition = GetControlTarget(targetName);
        return WaitForControls(options, CreateControlQuery(definition), timeoutMilliseconds, intervalMilliseconds, allWindows, allControls);
    }

    /// <summary>
    /// Clicks a saved control target against one or more matching windows.
    /// </summary>
    public IReadOnlyList<WindowControlTargetInfo> ClickControlTarget(WindowQueryOptions options, string targetName, MouseButton button, bool allWindows = false, bool allControls = false) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        DesktopControlTargetDefinition definition = GetControlTarget(targetName);
        return ClickControls(options, CreateControlQuery(definition), button, allWindows, allControls);
    }

    /// <summary>
    /// Sets text on a saved control target against one or more matching windows.
    /// </summary>
    public IReadOnlyList<WindowControlTargetInfo> SetControlTargetText(WindowQueryOptions options, string targetName, string text, bool ensureForegroundWindow = false, bool allowForegroundInputFallback = false, bool allWindows = false, bool allControls = false) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        DesktopControlTargetDefinition definition = GetControlTarget(targetName);
        WindowControlQueryOptions controlOptions = CreateControlQuery(definition);
        controlOptions.EnsureForegroundWindow = controlOptions.EnsureForegroundWindow || ensureForegroundWindow;
        controlOptions.AllowForegroundInputFallback = allowForegroundInputFallback;
        return SetControlText(options, controlOptions, text, allWindows, allControls);
    }

    /// <summary>
    /// Sends keys to a saved control target against one or more matching windows.
    /// </summary>
    public IReadOnlyList<WindowControlTargetInfo> SendControlTargetKeys(WindowQueryOptions options, string targetName, IReadOnlyList<VirtualKey> keys, bool ensureForegroundWindow = false, bool allowForegroundInputFallback = false, bool allWindows = false, bool allControls = false) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        DesktopControlTargetDefinition definition = GetControlTarget(targetName);
        WindowControlQueryOptions controlOptions = CreateControlQuery(definition);
        controlOptions.EnsureForegroundWindow = controlOptions.EnsureForegroundWindow || ensureForegroundWindow;
        controlOptions.AllowForegroundInputFallback = allowForegroundInputFallback;
        return SendControlKeys(options, controlOptions, keys, allWindows, allControls);
    }

    /// <summary>
    /// Resolves a saved target against one or more matching windows.
    /// </summary>
    public IReadOnlyList<DesktopResolvedWindowTarget> ResolveWindowTargets(WindowQueryOptions options, string targetName, bool all = false) {
        DesktopWindowTargetDefinition definition = GetWindowTarget(targetName);
        return ResolveWindowTargets(options, targetName, definition, all);
    }

    /// <summary>
    /// Captures the area described by a named window target against a matching window.
    /// </summary>
    public DesktopCapture CaptureWindowTarget(WindowQueryOptions options, string targetName) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        DesktopResolvedWindowTarget target = ResolveWindowTargets(options, targetName, all: false).FirstOrDefault()
            ?? throw new InvalidOperationException($"Named target '{targetName}' could not be resolved against a matching window.");
        if (!target.ScreenWidth.HasValue || !target.ScreenHeight.HasValue) {
            throw new InvalidOperationException($"Named target '{targetName}' does not define a capture area. Save it with width/height or widthRatio/heightRatio.");
        }

        return new DesktopCapture {
            Kind = "window-target",
            Bitmap = ScreenshotService.CaptureRegion(target.ScreenX, target.ScreenY, target.ScreenWidth.Value, target.ScreenHeight.Value),
            Window = target.Geometry.Window,
            Geometry = target.Geometry
        };
    }

    /// <summary>
    /// Clicks a saved target relative to each matching window.
    /// </summary>
    public IReadOnlyList<WindowInfo> ClickWindowTarget(WindowQueryOptions options, string targetName, MouseButton button, bool activate, bool all = false) {
        IReadOnlyList<DesktopResolvedWindowTarget> targets = ResolveWindowTargets(options, targetName, all);
        foreach (DesktopResolvedWindowTarget target in targets) {
            if (activate) {
                _windowManager.ActivateWindow(target.Geometry.Window);
                Thread.Sleep(100);
            }

            Thread.Sleep(50);
            _windowManager.MoveMouse(target.ScreenX, target.ScreenY);
            _windowManager.ClickMouse(button);
        }

        return RefreshWindows(targets.Select(target => target.Geometry.Window).ToArray());
    }

    /// <summary>
    /// Clicks the center of the best live match for a saved visual baseline inside each matching window.
    /// </summary>
    public IReadOnlyList<WindowInfo> ClickWindowVisualBaseline(WindowQueryOptions options, string baselineName, MouseButton button, bool activate, bool clientArea = true, double maxAverageDifference = 12.0, int differenceThreshold = 24, int scanStep = 8, bool all = false) {
        if (string.IsNullOrWhiteSpace(baselineName)) {
            throw new ArgumentException("A visual baseline name is required.", nameof(baselineName));
        }

        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);
        foreach (WindowInfo window in windows) {
            if (activate) {
                _windowManager.ActivateWindow(window);
                Thread.Sleep(100);
            }

            DesktopVisualBaselineResolveResult resolution = ResolveVisualBaseline(
                baselineName,
                new WindowQueryOptions {
                    Handle = window.Handle,
                    ProcessId = unchecked((int)window.ProcessId),
                    IncludeHidden = true,
                    IncludeCloaked = true,
                    IncludeOwned = true,
                    IncludeEmptyTitles = true
                },
                clientArea,
                maxAverageDifference,
                differenceThreshold,
                scanStep);

            if (!resolution.Matched) {
                throw new InvalidOperationException(
                    $"Visual baseline '{baselineName}' could not be matched reliably in window '{window.Title}'. " +
                    $"Average difference {resolution.Metrics.AverageDifference:F1} exceeded tolerance {maxAverageDifference:F1}.");
            }

            int screenCenterX = resolution.ScreenX + Math.Max(0, resolution.Width / 2);
            int screenCenterY = resolution.ScreenY + Math.Max(0, resolution.Height / 2);
            Thread.Sleep(50);
            _windowManager.MoveMouse(screenCenterX, screenCenterY);
            _windowManager.ClickMouse(button);
        }

        return RefreshWindows(windows);
    }

    /// <summary>
    /// Clicks the best OCR text match inside each matching window.
    /// </summary>
    public IReadOnlyList<WindowInfo> ClickWindowText(WindowQueryOptions options, string queryText, MouseButton button, bool activate, bool contains = false, string? targetName = null, bool clientArea = true, string? languageTag = null, bool all = false) {
        if (string.IsNullOrWhiteSpace(queryText)) {
            throw new ArgumentException("A non-empty OCR query is required.", nameof(queryText));
        }

        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);
        foreach (WindowInfo window in windows) {
            if (activate) {
                _windowManager.ActivateWindow(window);
                Thread.Sleep(100);
            }

            DesktopWindowTextResolveResult resolution = ResolveWindowText(
                CreatePinnedWindowQuery(window),
                queryText,
                contains,
                targetName,
                clientArea,
                languageTag);

            if (!resolution.Matched) {
                throw new InvalidOperationException(
                    $"Visible text '{queryText.Trim()}' could not be resolved in window '{window.Title}'.");
            }

            Thread.Sleep(50);
            _windowManager.MoveMouse(resolution.ActionX, resolution.ActionY);
            _windowManager.ClickMouse(button);
        }

        return RefreshWindows(windows);
    }

    /// <summary>
    /// Scrolls at the best OCR text match inside each matching window.
    /// </summary>
    public IReadOnlyList<WindowInfo> ScrollWindowText(WindowQueryOptions options, string queryText, int delta, bool activate, bool contains = false, string? targetName = null, bool clientArea = true, string? languageTag = null, bool all = false) {
        if (string.IsNullOrWhiteSpace(queryText)) {
            throw new ArgumentException("A non-empty OCR query is required.", nameof(queryText));
        }

        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);
        foreach (WindowInfo window in windows) {
            if (activate) {
                _windowManager.ActivateWindow(window);
                Thread.Sleep(100);
            }

            DesktopWindowTextResolveResult resolution = ResolveWindowText(
                CreatePinnedWindowQuery(window),
                queryText,
                contains,
                targetName,
                clientArea,
                languageTag);

            if (!resolution.Matched) {
                throw new InvalidOperationException(
                    $"Visible text '{queryText.Trim()}' could not be resolved in window '{window.Title}'.");
            }

            Thread.Sleep(50);
            _windowManager.MoveMouse(resolution.ActionX, resolution.ActionY);
            _windowManager.ScrollMouse(delta);
        }

        return RefreshWindows(windows);
    }

    /// <summary>
    /// Scrolls at a saved target relative to each matching window.
    /// </summary>
    public IReadOnlyList<WindowInfo> ScrollWindowTarget(WindowQueryOptions options, string targetName, int delta, bool activate, bool all = false) {
        IReadOnlyList<DesktopResolvedWindowTarget> targets = ResolveWindowTargets(options, targetName, all);
        foreach (DesktopResolvedWindowTarget target in targets) {
            if (activate) {
                _windowManager.ActivateWindow(target.Geometry.Window);
                Thread.Sleep(100);
            }

            Thread.Sleep(50);
            _windowManager.MoveMouse(target.ScreenX, target.ScreenY);
            _windowManager.ScrollMouse(delta);
        }

        return RefreshWindows(targets.Select(target => target.Geometry.Window).ToArray());
    }

    /// <summary>
    /// Scrolls at the center of the best live match for a saved visual baseline inside each matching window.
    /// </summary>
    public IReadOnlyList<WindowInfo> ScrollWindowVisualBaseline(WindowQueryOptions options, string baselineName, int delta, bool activate, bool clientArea = true, double maxAverageDifference = 12.0, int differenceThreshold = 24, int scanStep = 8, bool all = false) {
        if (string.IsNullOrWhiteSpace(baselineName)) {
            throw new ArgumentException("A visual baseline name is required.", nameof(baselineName));
        }

        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);
        foreach (WindowInfo window in windows) {
            if (activate) {
                _windowManager.ActivateWindow(window);
                Thread.Sleep(100);
            }

            DesktopVisualBaselineResolveResult resolution = ResolveVisualBaseline(
                baselineName,
                new WindowQueryOptions {
                    Handle = window.Handle,
                    ProcessId = unchecked((int)window.ProcessId),
                    IncludeHidden = true,
                    IncludeCloaked = true,
                    IncludeOwned = true,
                    IncludeEmptyTitles = true
                },
                clientArea,
                maxAverageDifference,
                differenceThreshold,
                scanStep);

            if (!resolution.Matched) {
                throw new InvalidOperationException(
                    $"Visual baseline '{baselineName}' could not be matched reliably in window '{window.Title}'. " +
                    $"Average difference {resolution.Metrics.AverageDifference:F1} exceeded tolerance {maxAverageDifference:F1}.");
            }

            int screenCenterX = resolution.ScreenX + Math.Max(0, resolution.Width / 2);
            int screenCenterY = resolution.ScreenY + Math.Max(0, resolution.Height / 2);
            Thread.Sleep(50);
            _windowManager.MoveMouse(screenCenterX, screenCenterY);
            _windowManager.ScrollMouse(delta);
        }

        return RefreshWindows(windows);
    }

    /// <summary>
    /// Drags between two saved targets relative to each matching window.
    /// </summary>
    public IReadOnlyList<WindowInfo> DragWindowTargets(WindowQueryOptions options, string startTargetName, string endTargetName, MouseButton button, int stepDelayMilliseconds, bool activate, bool all = false) {
        if (stepDelayMilliseconds < 0) {
            throw new ArgumentOutOfRangeException(nameof(stepDelayMilliseconds), "stepDelayMilliseconds must be zero or greater.");
        }

        DesktopWindowTargetDefinition startDefinition = GetWindowTarget(startTargetName);
        DesktopWindowTargetDefinition endDefinition = GetWindowTarget(endTargetName);
        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);
        foreach (WindowInfo window in windows) {
            if (activate) {
                _windowManager.ActivateWindow(window);
                Thread.Sleep(100);
            }

            DesktopWindowGeometry geometry = DescribeWindowGeometry(window);
            DesktopResolvedWindowTarget startTarget = ResolveWindowTarget(startTargetName, startDefinition, geometry);
            DesktopResolvedWindowTarget endTarget = ResolveWindowTarget(endTargetName, endDefinition, geometry);
            _windowManager.DragMouse(button, startTarget.ScreenX, startTarget.ScreenY, endTarget.ScreenX, endTarget.ScreenY, stepDelayMilliseconds);
        }

        return RefreshWindows(windows);
    }

    /// <summary>
    /// Drags between the centers of two saved visual-baseline matches inside each matching window.
    /// </summary>
    public IReadOnlyList<WindowInfo> DragWindowVisualBaselines(WindowQueryOptions options, string startBaselineName, string endBaselineName, MouseButton button, int stepDelayMilliseconds, bool activate, bool clientArea = true, double maxAverageDifference = 12.0, int differenceThreshold = 24, int scanStep = 8, bool all = false) {
        if (stepDelayMilliseconds < 0) {
            throw new ArgumentOutOfRangeException(nameof(stepDelayMilliseconds), "stepDelayMilliseconds must be zero or greater.");
        }

        if (string.IsNullOrWhiteSpace(startBaselineName)) {
            throw new ArgumentException("A starting visual baseline name is required.", nameof(startBaselineName));
        }

        if (string.IsNullOrWhiteSpace(endBaselineName)) {
            throw new ArgumentException("An ending visual baseline name is required.", nameof(endBaselineName));
        }

        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);
        foreach (WindowInfo window in windows) {
            if (activate) {
                _windowManager.ActivateWindow(window);
                Thread.Sleep(100);
            }

            WindowQueryOptions windowQuery = new() {
                Handle = window.Handle,
                ProcessId = unchecked((int)window.ProcessId),
                IncludeHidden = true,
                IncludeCloaked = true,
                IncludeOwned = true,
                IncludeEmptyTitles = true
            };
            DesktopVisualBaselineResolveResult startResolution = ResolveVisualBaseline(startBaselineName, windowQuery, clientArea, maxAverageDifference, differenceThreshold, scanStep);
            DesktopVisualBaselineResolveResult endResolution = ResolveVisualBaseline(endBaselineName, windowQuery, clientArea, maxAverageDifference, differenceThreshold, scanStep);

            if (!startResolution.Matched) {
                throw new InvalidOperationException(
                    $"Visual baseline '{startBaselineName}' could not be matched reliably in window '{window.Title}'. " +
                    $"Average difference {startResolution.Metrics.AverageDifference:F1} exceeded tolerance {maxAverageDifference:F1}.");
            }

            if (!endResolution.Matched) {
                throw new InvalidOperationException(
                    $"Visual baseline '{endBaselineName}' could not be matched reliably in window '{window.Title}'. " +
                    $"Average difference {endResolution.Metrics.AverageDifference:F1} exceeded tolerance {maxAverageDifference:F1}.");
            }

            int startCenterX = startResolution.ScreenX + Math.Max(0, startResolution.Width / 2);
            int startCenterY = startResolution.ScreenY + Math.Max(0, startResolution.Height / 2);
            int endCenterX = endResolution.ScreenX + Math.Max(0, endResolution.Width / 2);
            int endCenterY = endResolution.ScreenY + Math.Max(0, endResolution.Height / 2);
            _windowManager.DragMouse(button, startCenterX, startCenterY, endCenterX, endCenterY, stepDelayMilliseconds);
        }

        return RefreshWindows(windows);
    }

    /// <summary>
    /// Drags between two OCR-resolved visible text anchors inside each matching window.
    /// </summary>
    public IReadOnlyList<WindowInfo> DragWindowText(WindowQueryOptions options, string startQueryText, string endQueryText, MouseButton button, int stepDelayMilliseconds, bool activate, bool contains = false, string? startTargetName = null, string? endTargetName = null, bool clientArea = true, string? languageTag = null, bool all = false) {
        if (stepDelayMilliseconds < 0) {
            throw new ArgumentOutOfRangeException(nameof(stepDelayMilliseconds), "stepDelayMilliseconds must be zero or greater.");
        }

        if (string.IsNullOrWhiteSpace(startQueryText)) {
            throw new ArgumentException("A non-empty starting OCR query is required.", nameof(startQueryText));
        }

        if (string.IsNullOrWhiteSpace(endQueryText)) {
            throw new ArgumentException("A non-empty ending OCR query is required.", nameof(endQueryText));
        }

        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);
        foreach (WindowInfo window in windows) {
            if (activate) {
                _windowManager.ActivateWindow(window);
                Thread.Sleep(100);
            }

            WindowQueryOptions pinnedWindow = CreatePinnedWindowQuery(window);
            DesktopWindowTextResolveResult startResolution = ResolveWindowText(pinnedWindow, startQueryText, contains, startTargetName, clientArea, languageTag);
            DesktopWindowTextResolveResult endResolution = ResolveWindowText(pinnedWindow, endQueryText, contains, endTargetName, clientArea, languageTag);
            if (!startResolution.Matched) {
                throw new InvalidOperationException($"Visible text '{startQueryText.Trim()}' could not be resolved in window '{window.Title}'.");
            }

            if (!endResolution.Matched) {
                throw new InvalidOperationException($"Visible text '{endQueryText.Trim()}' could not be resolved in window '{window.Title}'.");
            }

            _windowManager.DragMouse(button, startResolution.ActionX, startResolution.ActionY, endResolution.ActionX, endResolution.ActionY, stepDelayMilliseconds);
        }

        return RefreshWindows(windows);
    }

    /// <summary>
    /// Gets matching controls for one or more windows.
    /// </summary>
    public IReadOnlyList<WindowControlTargetInfo> GetControls(WindowQueryOptions windowOptions, WindowControlQueryOptions? controlOptions = null, bool allWindows = false, bool allControls = true) {
        if (windowOptions == null) {
            throw new ArgumentNullException(nameof(windowOptions));
        }

        IReadOnlyList<WindowInfo> windows = GetMatchingWindows(windowOptions, allWindows);
        IReadOnlyList<WindowControlTargetInfo> controls = GetControls(windows, controlOptions, allControls: true);
        if (controls.Count == 0) {
            return controls;
        }

        if (allControls) {
            return controls;
        }

        return new[] { controls[0] };
    }

    /// <summary>
    /// Determines whether at least one control matches the supplied query.
    /// </summary>
    public bool ControlExists(WindowQueryOptions windowOptions, WindowControlQueryOptions? controlOptions = null, bool allWindows = false) {
        if (windowOptions == null) {
            throw new ArgumentNullException(nameof(windowOptions));
        }

        return GetControls(windowOptions, controlOptions, allWindows, allControls: true).Count > 0;
    }

    /// <summary>
    /// Collects control discovery diagnostics for one or more matching windows.
    /// </summary>
    public IReadOnlyList<DesktopControlDiscoveryDiagnostics> GetControlDiagnostics(WindowQueryOptions windowOptions, WindowControlQueryOptions? controlOptions = null, bool allWindows = false, int sampleLimit = 10, bool includeActionProbe = false) {
        if (windowOptions == null) {
            throw new ArgumentNullException(nameof(windowOptions));
        }

        if (sampleLimit < 0) {
            throw new ArgumentOutOfRangeException(nameof(sampleLimit), "sampleLimit must be zero or greater.");
        }

        List<WindowInfo> windows = _windowManager.GetWindows(windowOptions);
        if (!allWindows && windows.Count > 1) {
            windows = new List<WindowInfo> { windows[0] };
        }

        return windows
            .Select(window => _windowManager.DiagnoseControls(window, controlOptions, sampleLimit, includeActionProbe))
            .ToArray();
    }

    /// <summary>
    /// Clicks matching controls.
    /// </summary>
    public IReadOnlyList<WindowControlTargetInfo> ClickControls(WindowQueryOptions windowOptions, WindowControlQueryOptions? controlOptions, MouseButton button, bool allWindows = false, bool allControls = false) {
        IReadOnlyList<WindowControlTargetInfo> controls = RequireControls(windowOptions, controlOptions, allWindows, allControls);
        foreach (WindowControlTargetInfo control in controls) {
            ClickControl(control.Window, control.Control, button);
        }

        return controls;
    }

    /// <summary>
    /// Sets text on matching controls.
    /// </summary>
    public IReadOnlyList<WindowControlTargetInfo> SetControlText(WindowQueryOptions windowOptions, WindowControlQueryOptions? controlOptions, string text, bool allWindows = false, bool allControls = false) {
        if (text == null) {
            throw new ArgumentNullException(nameof(text));
        }

        IReadOnlyList<WindowControlTargetInfo> controls = RequireControls(windowOptions, controlOptions, allWindows, allControls);
        foreach (WindowControlTargetInfo control in controls) {
            SetControlText(control.Window, control.Control, text, controlOptions?.EnsureForegroundWindow ?? false, controlOptions?.AllowForegroundInputFallback ?? false);
        }

        return controls;
    }

    /// <summary>
    /// Sets the checked state on matching controls.
    /// </summary>
    public IReadOnlyList<WindowControlTargetInfo> SetControlCheckState(WindowQueryOptions windowOptions, WindowControlQueryOptions? controlOptions, bool check, bool allWindows = false, bool allControls = false) {
        IReadOnlyList<WindowControlTargetInfo> controls = RequireControls(windowOptions, controlOptions, allWindows, allControls);
        foreach (WindowControlTargetInfo control in controls) {
            SetControlCheckState(control.Control, check);
        }

        return controls;
    }

    /// <summary>
    /// Selects a displayed value on matching controls.
    /// </summary>
    public IReadOnlyList<WindowControlTargetInfo> SetControlSelectedValue(WindowQueryOptions windowOptions, WindowControlQueryOptions? controlOptions, string selectedValue, bool allWindows = false, bool allControls = false) {
        if (selectedValue == null) {
            throw new ArgumentNullException(nameof(selectedValue));
        }

        IReadOnlyList<WindowControlTargetInfo> controls = RequireControls(windowOptions, controlOptions, allWindows, allControls);
        foreach (WindowControlTargetInfo control in controls) {
            SetControlSelectedValue(control.Control, selectedValue);
        }

        return controls;
    }

    /// <summary>
    /// Sends keys to matching controls.
    /// </summary>
    public IReadOnlyList<WindowControlTargetInfo> SendControlKeys(WindowQueryOptions windowOptions, WindowControlQueryOptions? controlOptions, IReadOnlyList<VirtualKey> keys, bool allWindows = false, bool allControls = false) {
        if (keys == null || keys.Count == 0) {
            throw new ArgumentException("At least one key is required.", nameof(keys));
        }

        IReadOnlyList<WindowControlTargetInfo> controls = RequireControls(windowOptions, controlOptions, allWindows, allControls);
        foreach (WindowControlTargetInfo control in controls) {
            SendControlKeys(control.Window, control.Control, keys, controlOptions?.EnsureForegroundWindow ?? false, controlOptions?.AllowForegroundInputFallback ?? false);
        }

        return controls;
    }

    /// <summary>
    /// Clicks the supplied control using either Win32 or UI Automation routing.
    /// </summary>
    public void ClickControl(WindowControlInfo control, MouseButton button = MouseButton.Left) {
        ClickControl(ResolveParentWindow(control), control, button);
    }

    /// <summary>
    /// Sets text on the supplied control using either Win32 or UI Automation routing.
    /// </summary>
    public void SetControlText(WindowControlInfo control, string text, bool ensureForegroundWindow = false, bool allowForegroundInputFallback = false) {
        SetControlText(ResolveParentWindow(control), control, text, ensureForegroundWindow, allowForegroundInputFallback);
    }

    /// <summary>
    /// Sends keys to the supplied control using either Win32 or UI Automation routing.
    /// </summary>
    public void SendControlKeys(WindowControlInfo control, IReadOnlyList<VirtualKey> keys, bool ensureForegroundWindow = false, bool allowForegroundInputFallback = false) {
        SendControlKeys(ResolveParentWindow(control), control, keys, ensureForegroundWindow, allowForegroundInputFallback);
    }

    private void ClickControl(WindowInfo window, WindowControlInfo control, MouseButton button) {
        var uiAutomation = new UiAutomationControlService();
        if (control.Source == WindowControlSource.UiAutomation && control.Handle == IntPtr.Zero) {
            if (!uiAutomation.TryInvoke(window, control)) {
                throw new InvalidOperationException("The UI Automation control could not be invoked.");
            }
            return;
        }

        _windowManager.ClickControl(control, button);
    }

    private void SetControlText(WindowInfo window, WindowControlInfo control, string text, bool ensureForegroundWindow, bool allowForegroundInputFallback) {
        var uiAutomation = new UiAutomationControlService();
        if (control.Source == WindowControlSource.UiAutomation && control.Handle == IntPtr.Zero) {
            if (uiAutomation.TrySetValue(window, control, text)) {
                return;
            }

            string? validationError = ValidateUiAutomationTextFallback(control, allowForegroundInputFallback);
            if (validationError != null) {
                throw new InvalidOperationException(validationError);
            }

            if (!uiAutomation.TrySetText(window, control, text, ensureForegroundWindow)) {
                throw new InvalidOperationException("The UI Automation control text could not be set even with foreground input fallback enabled.");
            }

            return;
        }

        _windowManager.SetControlText(control, text);
    }

    private void SendControlKeys(WindowInfo window, WindowControlInfo control, IReadOnlyList<VirtualKey> keys, bool ensureForegroundWindow, bool allowForegroundInputFallback) {
        var uiAutomation = new UiAutomationControlService();
        if (control.Source == WindowControlSource.UiAutomation && control.Handle == IntPtr.Zero) {
            string? validationError = ValidateUiAutomationKeyFallback(control, allowForegroundInputFallback);
            if (validationError != null) {
                throw new InvalidOperationException(validationError);
            }

            if (!uiAutomation.TrySendKeys(window, control, keys, ensureForegroundWindow)) {
                throw new InvalidOperationException("The UI Automation control could not receive keys even with foreground input fallback enabled.");
            }

            return;
        }

        _windowManager.SendControlKeys(control, keys.ToArray());
    }

    internal static string? ValidateUiAutomationTextFallback(WindowControlInfo control, bool allowForegroundInputFallback) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        if (control.Source != WindowControlSource.UiAutomation || control.Handle != IntPtr.Zero) {
            return null;
        }

        if (!control.SupportsForegroundInputFallback) {
            return "The selected UI Automation control does not expose direct value setting and is not keyboard-focusable for foreground input fallback.";
        }

        if (!allowForegroundInputFallback) {
            return "The UI Automation control does not support direct value setting. Enable foreground input fallback only when you intentionally allow focused input for modern app controls.";
        }

        return null;
    }

    internal static string? ValidateUiAutomationKeyFallback(WindowControlInfo control, bool allowForegroundInputFallback) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        if (control.Source != WindowControlSource.UiAutomation || control.Handle != IntPtr.Zero) {
            return null;
        }

        if (!control.SupportsForegroundInputFallback) {
            return "The selected UI Automation control is not keyboard-focusable and cannot receive foreground fallback key input.";
        }

        if (!allowForegroundInputFallback) {
            return "The selected UI Automation control does not expose a Win32 handle. Enable foreground input fallback only when you intentionally allow focused input for modern app controls.";
        }

        return null;
    }

    /// <summary>
    /// Waits for matching windows to appear.
    /// </summary>
    public DesktopWindowWaitResult WaitForWindows(WindowQueryOptions options, int timeoutMilliseconds, int intervalMilliseconds, bool all = false) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        ValidateWaitArguments(timeoutMilliseconds, intervalMilliseconds);
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (timeoutMilliseconds == 0 || stopwatch.ElapsedMilliseconds < timeoutMilliseconds) {
            List<WindowInfo> windows = _windowManager.GetWindows(options);
            if (windows.Count > 0) {
                IReadOnlyList<WindowInfo> selected = all ? windows : new[] { windows[0] };
                return new DesktopWindowWaitResult {
                    ElapsedMilliseconds = (int)stopwatch.ElapsedMilliseconds,
                    Windows = selected
                };
            }

            Thread.Sleep(intervalMilliseconds);
        }

        throw new TimeoutException($"Timed out after {timeoutMilliseconds}ms waiting for a matching window.");
    }

    /// <summary>
    /// Waits for matching windows to close.
    /// </summary>
    public DesktopWaitResult WaitForWindowToClose(WindowQueryOptions options, int timeoutMilliseconds, int intervalMilliseconds, bool all = false) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        ValidateWaitArguments(timeoutMilliseconds, intervalMilliseconds);

        Stopwatch stopwatch = Stopwatch.StartNew();
        var trackedHandles = new HashSet<IntPtr>();
        while (timeoutMilliseconds == 0 || stopwatch.ElapsedMilliseconds < timeoutMilliseconds) {
            if (trackedHandles.Count == 0) {
                IReadOnlyList<WindowInfo> initialWindows = GetMatchingWindows(options, all);
                RememberWindowHandles(trackedHandles, initialWindows);
                if (trackedHandles.Count == 0) {
                    return new DesktopWaitResult {
                        ElapsedMilliseconds = (int)stopwatch.ElapsedMilliseconds
                    };
                }
            }

            if (GetWindowsByHandle(trackedHandles, all: true).Count == 0) {
                return new DesktopWaitResult {
                    ElapsedMilliseconds = (int)stopwatch.ElapsedMilliseconds
                };
            }

            Thread.Sleep(intervalMilliseconds);
        }

        throw new TimeoutException($"Timed out after {timeoutMilliseconds}ms waiting for a matching window to close.");
    }

    /// <summary>
    /// Waits for matching windows to no longer own the foreground.
    /// </summary>
    public DesktopWaitResult WaitForWindowToLoseFocus(WindowQueryOptions options, int timeoutMilliseconds, int intervalMilliseconds, bool all = false) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        ValidateWaitArguments(timeoutMilliseconds, intervalMilliseconds);

        Stopwatch stopwatch = Stopwatch.StartNew();
        var trackedHandles = new HashSet<IntPtr>();
        while (timeoutMilliseconds == 0 || stopwatch.ElapsedMilliseconds < timeoutMilliseconds) {
            if (trackedHandles.Count == 0) {
                IReadOnlyList<WindowInfo> initialWindows = GetMatchingWindows(options, all);
                RememberWindowHandles(trackedHandles, initialWindows);
                if (trackedHandles.Count == 0) {
                    return new DesktopWaitResult {
                        ElapsedMilliseconds = (int)stopwatch.ElapsedMilliseconds
                    };
                }
            }

            WindowInfo? activeWindow = _windowManager.GetActiveWindow(includeHidden: true, includeCloaked: true, includeOwned: true, includeEmptyTitles: true);
            if (activeWindow == null || !trackedHandles.Contains(activeWindow.Handle)) {
                return new DesktopWaitResult {
                    ElapsedMilliseconds = (int)stopwatch.ElapsedMilliseconds
                };
            }

            Thread.Sleep(intervalMilliseconds);
        }

        throw new TimeoutException($"Timed out after {timeoutMilliseconds}ms waiting for a matching window to lose focus.");
    }

    /// <summary>
    /// Waits for matching controls to appear.
    /// </summary>
    public DesktopControlWaitResult WaitForControls(WindowQueryOptions windowOptions, WindowControlQueryOptions? controlOptions, int timeoutMilliseconds, int intervalMilliseconds, bool allWindows = false, bool allControls = false) {
        if (windowOptions == null) {
            throw new ArgumentNullException(nameof(windowOptions));
        }

        ValidateWaitArguments(timeoutMilliseconds, intervalMilliseconds);

        Stopwatch stopwatch = Stopwatch.StartNew();
        var preferredWindowHandles = new HashSet<IntPtr>();
        long nextWindowRediscoveryAtMilliseconds = 0;
        while (timeoutMilliseconds == 0 || stopwatch.ElapsedMilliseconds < timeoutMilliseconds) {
            IReadOnlyList<WindowControlTargetInfo> controls = Array.Empty<WindowControlTargetInfo>();
            if (preferredWindowHandles.Count > 0) {
                IReadOnlyList<WindowInfo> preferredWindows = GetWindowsByHandle(preferredWindowHandles, allWindows);
                if (preferredWindows.Count > 0) {
                    controls = GetControls(preferredWindows, controlOptions, allControls: true);
                    if (controls.Count > 0) {
                        IReadOnlyList<WindowControlTargetInfo> preferredSelected = allControls ? controls : new[] { controls[0] };
                        return new DesktopControlWaitResult {
                            ElapsedMilliseconds = (int)stopwatch.ElapsedMilliseconds,
                            Controls = preferredSelected
                        };
                    }

                    if (stopwatch.ElapsedMilliseconds < nextWindowRediscoveryAtMilliseconds) {
                        Thread.Sleep(intervalMilliseconds);
                        continue;
                    }
                } else {
                    preferredWindowHandles.Clear();
                }
            }

            IReadOnlyList<WindowInfo> windows = GetMatchingWindows(windowOptions, allWindows);
            if (windows.Count > 0) {
                RememberWindowHandles(preferredWindowHandles, windows);
                nextWindowRediscoveryAtMilliseconds = stopwatch.ElapsedMilliseconds + PreferredWindowRediscoveryMilliseconds;
                controls = GetControls(windows, controlOptions, allControls: true);
            }

            if (controls.Count > 0) {
                IReadOnlyList<WindowControlTargetInfo> selected = allControls ? controls : new[] { controls[0] };
                return new DesktopControlWaitResult {
                    ElapsedMilliseconds = (int)stopwatch.ElapsedMilliseconds,
                    Controls = selected
                };
            }

            Thread.Sleep(intervalMilliseconds);
        }

        throw new TimeoutException($"Timed out after {timeoutMilliseconds}ms waiting for a matching control.");
    }

    /// <summary>
    /// Waits until observed window text contains the requested value.
    /// </summary>
    /// <param name="options">Window selection options.</param>
    /// <param name="expectedText">Expected text to observe.</param>
    /// <param name="timeoutMilliseconds">Timeout in milliseconds. Zero waits indefinitely.</param>
    /// <param name="intervalMilliseconds">Polling interval in milliseconds.</param>
    /// <param name="observationOptions">Observation configuration.</param>
    /// <returns>The matching text observation.</returns>
    public DesktopWindowTextObservation WaitForObservedText(WindowQueryOptions options, string expectedText, int timeoutMilliseconds, int intervalMilliseconds, DesktopTextObservationOptions? observationOptions = null) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        if (expectedText == null) {
            throw new ArgumentNullException(nameof(expectedText));
        }

        if (expectedText.Length == 0) {
            throw new ArgumentException("expectedText must not be empty.", nameof(expectedText));
        }

        DesktopTextObservationOptions settings = observationOptions ?? new DesktopTextObservationOptions();
        ValidateTextObservationOptions(settings);
        ValidateWaitArguments(timeoutMilliseconds, intervalMilliseconds);

        Stopwatch stopwatch = Stopwatch.StartNew();
        while (timeoutMilliseconds == 0 || stopwatch.ElapsedMilliseconds < timeoutMilliseconds) {
            DesktopWindowTextObservation? observation = ObserveWindowText(options, expectedText, settings);
            if (observation?.ContainsExpected == true) {
                return observation;
            }

            Thread.Sleep(intervalMilliseconds);
        }

        throw new TimeoutException($"Timed out after {timeoutMilliseconds}ms waiting for observed text '{expectedText}'.");
    }

    /// <summary>
    /// Waits until observed window text contains the requested value for a specific window handle.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    /// <param name="expectedText">Expected text to observe.</param>
    /// <param name="timeoutMilliseconds">Timeout in milliseconds. Zero waits indefinitely.</param>
    /// <param name="intervalMilliseconds">Polling interval in milliseconds.</param>
    /// <param name="observationOptions">Observation configuration.</param>
    /// <returns>The matching text observation.</returns>
    public DesktopWindowTextObservation WaitForObservedText(IntPtr windowHandle, string expectedText, int timeoutMilliseconds, int intervalMilliseconds, DesktopTextObservationOptions? observationOptions = null) {
        if (windowHandle == IntPtr.Zero) {
            throw new ArgumentException("Invalid window handle.", nameof(windowHandle));
        }

        return WaitForObservedText(new WindowQueryOptions {
            Handle = windowHandle,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        }, expectedText, timeoutMilliseconds, intervalMilliseconds, observationOptions);
    }

    /// <summary>
    /// Waits until a matching window exposes a focused control observation.
    /// </summary>
    /// <param name="options">Window selection options.</param>
    /// <param name="timeoutMilliseconds">Timeout in milliseconds. Zero waits indefinitely.</param>
    /// <param name="intervalMilliseconds">Polling interval in milliseconds.</param>
    /// <returns>The focused-control observation.</returns>
    public DesktopFocusedControlObservation WaitForFocusedControlObservation(WindowQueryOptions options, int timeoutMilliseconds, int intervalMilliseconds) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        ValidateWaitArguments(timeoutMilliseconds, intervalMilliseconds);

        Stopwatch stopwatch = Stopwatch.StartNew();
        while (timeoutMilliseconds == 0 || stopwatch.ElapsedMilliseconds < timeoutMilliseconds) {
            DesktopFocusedControlObservation? observation = GetFocusedControlObservation(options);
            if (observation != null && observation.FocusedHandle != IntPtr.Zero) {
                return observation;
            }

            Thread.Sleep(intervalMilliseconds);
        }

        throw new TimeoutException($"Timed out after {timeoutMilliseconds}ms waiting for a focused control.");
    }

    /// <summary>
    /// Waits until a specific window handle exposes a focused control observation.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    /// <param name="timeoutMilliseconds">Timeout in milliseconds. Zero waits indefinitely.</param>
    /// <param name="intervalMilliseconds">Polling interval in milliseconds.</param>
    /// <returns>The focused-control observation.</returns>
    public DesktopFocusedControlObservation WaitForFocusedControlObservation(IntPtr windowHandle, int timeoutMilliseconds, int intervalMilliseconds) {
        if (windowHandle == IntPtr.Zero) {
            throw new ArgumentException("Invalid window handle.", nameof(windowHandle));
        }

        return WaitForFocusedControlObservation(new WindowQueryOptions {
            Handle = windowHandle,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        }, timeoutMilliseconds, intervalMilliseconds);
    }

    /// <summary>
    /// Waits until a matching window, client area, or named target changes visually.
    /// </summary>
    public DesktopWindowVisualChangeObservation WaitForWindowVisualChange(
        WindowQueryOptions options,
        string? targetName,
        bool clientArea,
        int timeoutMilliseconds,
        int intervalMilliseconds,
        double minimumChangedRatio = 0.01,
        int differenceThreshold = 24) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        if (minimumChangedRatio < 0 || minimumChangedRatio > 1) {
            throw new ArgumentOutOfRangeException(nameof(minimumChangedRatio), "The minimum changed ratio must be between 0 and 1.");
        }

        if (differenceThreshold < 0 || differenceThreshold > 255) {
            throw new ArgumentOutOfRangeException(nameof(differenceThreshold), "The difference threshold must be between 0 and 255.");
        }

        ValidateWaitArguments(timeoutMilliseconds, intervalMilliseconds);

        WindowInfo baselineWindow = ResolveSingleWindow(options);
        string? normalizedTargetName = string.IsNullOrWhiteSpace(targetName) ? null : targetName!.Trim();
        using DesktopCapture baselineCapture = CaptureWindowForVisualObservation(baselineWindow.Handle, normalizedTargetName, clientArea);
        return WaitForWindowVisualChange(
            baselineWindow,
            baselineCapture,
            normalizedTargetName,
            clientArea,
            timeoutMilliseconds,
            intervalMilliseconds,
            minimumChangedRatio,
            differenceThreshold);
    }

    internal DesktopWindowVisualChangeObservation WaitForWindowVisualChange(
        WindowInfo baselineWindow,
        DesktopCapture baselineCapture,
        string? targetName,
        bool clientArea,
        int timeoutMilliseconds,
        int intervalMilliseconds,
        double minimumChangedRatio = 0.01,
        int differenceThreshold = 24) {
        if (baselineWindow == null) {
            throw new ArgumentNullException(nameof(baselineWindow));
        }

        if (baselineCapture == null) {
            throw new ArgumentNullException(nameof(baselineCapture));
        }

        if (minimumChangedRatio < 0 || minimumChangedRatio > 1) {
            throw new ArgumentOutOfRangeException(nameof(minimumChangedRatio), "The minimum changed ratio must be between 0 and 1.");
        }

        if (differenceThreshold < 0 || differenceThreshold > 255) {
            throw new ArgumentOutOfRangeException(nameof(differenceThreshold), "The difference threshold must be between 0 and 255.");
        }

        ValidateWaitArguments(timeoutMilliseconds, intervalMilliseconds);

        string? normalizedTargetName = string.IsNullOrWhiteSpace(targetName) ? null : targetName!.Trim();
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (timeoutMilliseconds == 0 || stopwatch.ElapsedMilliseconds < timeoutMilliseconds) {
            using DesktopCapture currentCapture = CaptureWindowForVisualObservation(baselineWindow.Handle, normalizedTargetName, clientArea);
            DesktopVisualDifferenceMetrics metrics = ScreenshotService.CompareBitmaps(
                baselineCapture.Bitmap,
                currentCapture.Bitmap,
                differenceThreshold);
            if (metrics.ChangedSampleRatio >= minimumChangedRatio) {
                return new DesktopWindowVisualChangeObservation {
                    ElapsedMilliseconds = (int)stopwatch.ElapsedMilliseconds,
                    Window = currentCapture.Window ?? ResolveWindowByHandle(baselineWindow.Handle),
                    Geometry = currentCapture.Geometry ?? DescribeWindowGeometry(ResolveWindowByHandle(baselineWindow.Handle)),
                    TargetName = normalizedTargetName,
                    ClientArea = normalizedTargetName == null && clientArea,
                    MinimumChangedRatio = minimumChangedRatio,
                    Metrics = metrics
                };
            }

            Thread.Sleep(intervalMilliseconds);
        }

        throw new TimeoutException($"Timed out after {timeoutMilliseconds}ms waiting for a visible change in the matching window.");
    }

    /// <summary>
    /// Gets clipboard text when Unicode text is currently available.
    /// </summary>
    public string? GetClipboardText(int retryCount = 5, int retryDelayMilliseconds = 50) {
        return ClipboardHelper.TryGetText(out string text, retryCount, retryDelayMilliseconds)
            ? text
            : null;
    }

    /// <summary>
    /// Sets clipboard text.
    /// </summary>
    public void SetClipboardText(string text, int retryCount = 5, int retryDelayMilliseconds = 50) {
        ClipboardHelper.SetText(text, retryCount, retryDelayMilliseconds);
    }

    /// <summary>
    /// Returns whether the current process is elevated.
    /// </summary>
    public bool IsElevated() {
        return PrivilegeChecker.IsElevated;
    }

    /// <summary>
    /// Throws if the current process is not elevated.
    /// </summary>
    public void EnsureElevated() {
        PrivilegeChecker.EnsureElevated();
    }

    /// <summary>
    /// Launches a desktop process and waits for a correlated final window.
    /// </summary>
    public DesktopProcessLaunchAndWaitResult LaunchAndWaitForWindow(DesktopProcessLaunchAndWaitOptions options) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.FilePath)) {
            throw new ArgumentException("A process path or command is required.", nameof(options));
        }

        if (options.TimeoutMilliseconds <= 0) {
            throw new ArgumentOutOfRangeException(nameof(options.TimeoutMilliseconds), "timeoutMilliseconds must be greater than zero.");
        }

        if (options.IntervalMilliseconds <= 0) {
            throw new ArgumentOutOfRangeException(nameof(options.IntervalMilliseconds), "intervalMilliseconds must be greater than zero.");
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        DesktopProcessLaunchInfo launch = LaunchProcess(new DesktopProcessStartOptions {
            FilePath = options.FilePath,
            Arguments = options.Arguments,
            WorkingDirectory = options.WorkingDirectory,
            WaitForInputIdleMilliseconds = options.WaitForInputIdleMilliseconds,
            WaitForWindowMilliseconds = options.LaunchWaitForWindowMilliseconds,
            WaitForWindowIntervalMilliseconds = options.LaunchWaitForWindowIntervalMilliseconds,
            WindowTitlePattern = options.LaunchWindowTitlePattern,
            WindowClassNamePattern = options.LaunchWindowClassNamePattern,
            RequireWindow = false
        });

        DesktopLaunchWaitBindingPlan waitPlan = CreateLaunchWaitBindingPlan(
            launch,
            options.LaunchWindowTitlePattern,
            options.LaunchWindowClassNamePattern,
            options.WindowTitlePattern,
            options.WindowClassNamePattern,
            options.IncludeHidden,
            options.IncludeEmptyTitles,
            options.All,
            options.FollowProcessFamily);
        DesktopWindowWaitResult waitResult = WaitForWindows(waitPlan.Criteria, options.TimeoutMilliseconds, options.IntervalMilliseconds, options.All);

        stopwatch.Stop();
        return new DesktopProcessLaunchAndWaitResult {
            Success = waitResult.Windows.Count > 0,
            ElapsedMilliseconds = (int)stopwatch.ElapsedMilliseconds,
            Launch = launch,
            WaitPlan = waitPlan,
            WindowWait = waitResult
        };
    }

    /// <summary>
    /// Creates the final wait binding plan for a launch-and-wait workflow.
    /// </summary>
    /// <param name="launch">Launch metadata.</param>
    /// <param name="launchWindowTitlePattern">Optional launch-time window title filter.</param>
    /// <param name="launchWindowClassNamePattern">Optional launch-time window class filter.</param>
    /// <param name="windowTitlePattern">Optional final window title filter.</param>
    /// <param name="windowClassNamePattern">Optional final window class filter.</param>
    /// <param name="includeHidden">Whether hidden windows are included during the final wait.</param>
    /// <param name="includeEmptyTitles">Whether windows with empty titles are included during the final wait.</param>
    /// <param name="all">Whether all matching windows should be returned instead of the first match.</param>
    /// <param name="followProcessFamily">Whether the final wait may follow the launched app's same-name process family.</param>
    /// <returns>The final wait binding plan.</returns>
    public static DesktopLaunchWaitBindingPlan CreateLaunchWaitBindingPlan(DesktopProcessLaunchInfo launch, string? launchWindowTitlePattern, string? launchWindowClassNamePattern, string? windowTitlePattern, string? windowClassNamePattern, bool includeHidden, bool includeEmptyTitles, bool all, bool followProcessFamily) {
        if (launch == null) {
            throw new ArgumentNullException(nameof(launch));
        }

        WindowQueryOptions criteria = new() {
            TitlePattern = windowTitlePattern ?? launchWindowTitlePattern ?? "*",
            ClassNamePattern = windowClassNamePattern ?? launchWindowClassNamePattern ?? "*",
            IncludeHidden = includeHidden,
            IncludeCloaked = false,
            IncludeOwned = true,
            IncludeEmptyTitles = includeEmptyTitles
        };

        if (launch.ResolvedProcessId.HasValue) {
            criteria.ProcessId = launch.ResolvedProcessId.Value;
            return new DesktopLaunchWaitBindingPlan {
                Criteria = criteria,
                WaitBinding = "resolved-process-id",
                BoundProcessId = launch.ResolvedProcessId.Value
            };
        }

        if (!followProcessFamily) {
            criteria.ProcessId = launch.ProcessId;
            return new DesktopLaunchWaitBindingPlan {
                Criteria = criteria,
                WaitBinding = "launcher-process-id",
                BoundProcessId = launch.ProcessId
            };
        }

        string? processNameHint = GetProcessNameHint(launch.FilePath);
        if (string.IsNullOrWhiteSpace(processNameHint)) {
            criteria.ProcessId = launch.ProcessId;
            return new DesktopLaunchWaitBindingPlan {
                Criteria = criteria,
                WaitBinding = "launcher-process-id",
                BoundProcessId = launch.ProcessId
            };
        }

        criteria.ProcessNamePattern = processNameHint!;
        return new DesktopLaunchWaitBindingPlan {
            Criteria = criteria,
            WaitBinding = "process-name-family",
            BoundProcessName = processNameHint
        };
    }

    /// <summary>
    /// Launches a desktop process.
    /// </summary>
    public DesktopProcessLaunchInfo LaunchProcess(DesktopProcessStartOptions options) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.FilePath)) {
            throw new ArgumentException("A process path or command is required.", nameof(options));
        }

        if (options.WaitForInputIdleMilliseconds.HasValue && options.WaitForInputIdleMilliseconds.Value < 0) {
            throw new ArgumentOutOfRangeException(nameof(options.WaitForInputIdleMilliseconds), "waitForInputIdleMilliseconds must be zero or greater.");
        }

        if (options.WaitForWindowMilliseconds.HasValue && options.WaitForWindowMilliseconds.Value < 0) {
            throw new ArgumentOutOfRangeException(nameof(options.WaitForWindowMilliseconds), "waitForWindowMilliseconds must be zero or greater.");
        }

        if (options.WaitForWindowIntervalMilliseconds.HasValue && options.WaitForWindowIntervalMilliseconds.Value <= 0) {
            throw new ArgumentOutOfRangeException(nameof(options.WaitForWindowIntervalMilliseconds), "waitForWindowIntervalMilliseconds must be greater than zero.");
        }

        if (!string.IsNullOrWhiteSpace(options.WorkingDirectory) && !Directory.Exists(options.WorkingDirectory)) {
            throw new DirectoryNotFoundException($"The working directory '{options.WorkingDirectory}' does not exist.");
        }

        bool useShellExecute = ShouldUseShellExecute(options.FilePath);
        var startInfo = new ProcessStartInfo(options.FilePath) {
            UseShellExecute = useShellExecute
        };
        if (!string.IsNullOrWhiteSpace(options.Arguments)) {
            startInfo.Arguments = options.Arguments;
        }
        if (!string.IsNullOrWhiteSpace(options.WorkingDirectory)) {
            startInfo.WorkingDirectory = options.WorkingDirectory;
        }

        string? primaryProcessNameHint = GetProcessNameHint(options.FilePath);
        IReadOnlyList<string> initialProcessNameHints = CollectProcessNameHints(primaryProcessNameHint);
        ISet<IntPtr> preLaunchWindowHandles = useShellExecute
            ? CaptureWindowHandlesForProcesses(initialProcessNameHints)
            : new HashSet<IntPtr>();
        DateTime launchStartedUtc = DateTime.UtcNow;
        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Failed to start process '{options.FilePath}'.");

        if (options.WaitForInputIdleMilliseconds.HasValue && options.WaitForInputIdleMilliseconds.Value > 0) {
            try {
                process.WaitForInputIdle(options.WaitForInputIdleMilliseconds.Value);
            } catch (InvalidOperationException) {
            }
        }

        process.Refresh();
        IReadOnlyList<string> processNameHints = CollectProcessNameHints(primaryProcessNameHint, GetProcessNameHint(process, options.FilePath));
        int waitForWindowMilliseconds = options.WaitForWindowMilliseconds ?? 2000;
        int waitForWindowIntervalMilliseconds = options.WaitForWindowIntervalMilliseconds ?? 200;
        WindowInfo? mainWindow = waitForWindowMilliseconds == 0
            ? TryResolveImmediateLaunchedWindow(process, options.WindowTitlePattern, options.WindowClassNamePattern)
            : TryResolveLaunchedWindow(process.Id, processNameHints, launchStartedUtc, preLaunchWindowHandles, options.WindowTitlePattern, options.WindowClassNamePattern, waitForWindowMilliseconds, waitForWindowIntervalMilliseconds);
        if (options.RequireWindow && mainWindow == null) {
            throw new TimeoutException(BuildMissingLaunchedWindowMessage(processNameHints, options.WindowTitlePattern, options.WindowClassNamePattern, waitForWindowMilliseconds));
        }

        return new DesktopProcessLaunchInfo {
            FilePath = options.FilePath,
            Arguments = options.Arguments,
            WorkingDirectory = string.IsNullOrWhiteSpace(options.WorkingDirectory) ? null : Path.GetFullPath(options.WorkingDirectory),
            ProcessId = process.Id,
            ResolvedProcessId = mainWindow == null ? null : (int?)mainWindow.ProcessId,
            HasExited = process.HasExited,
            MainWindow = mainWindow
        };
    }

    /// <summary>
    /// Captures the entire desktop.
    /// </summary>
    public DesktopCapture CaptureDesktop() {
        return new DesktopCapture {
            Kind = "desktop",
            Bitmap = ScreenshotService.CaptureScreen()
        };
    }

    /// <summary>
    /// Captures a monitor.
    /// </summary>
    public DesktopCapture CaptureMonitor(int? monitorIndex = null, string? deviceId = null, string? deviceName = null) {
        Monitor monitor = GetMonitor(index: monitorIndex, deviceId: deviceId, deviceName: deviceName)
            ?? throw new InvalidOperationException("No matching monitor was found.");

        return new DesktopCapture {
            Kind = "monitor",
            Bitmap = ScreenshotService.CaptureMonitor(index: monitor.Index, deviceId: monitor.DeviceId, deviceName: monitor.DeviceName),
            MonitorIndex = monitor.Index,
            MonitorDeviceName = monitor.DeviceName
        };
    }

    /// <summary>
    /// Captures a desktop region.
    /// </summary>
    public DesktopCapture CaptureRegion(int left, int top, int width, int height) {
        return new DesktopCapture {
            Kind = "region",
            Bitmap = ScreenshotService.CaptureRegion(left, top, width, height)
        };
    }

    /// <summary>
    /// Captures a single matching window.
    /// </summary>
    public DesktopCapture CaptureWindow(WindowQueryOptions options) {
        WindowInfo window = ResolveSingleWindow(options);
        return new DesktopCapture {
            Kind = "window",
            Bitmap = ScreenshotService.CaptureWindow(window.Handle),
            Window = window,
            Geometry = DescribeWindowGeometry(window)
        };
    }

    /// <summary>
    /// Captures a specific window handle.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    /// <returns>The captured window image.</returns>
    public DesktopCapture CaptureWindow(IntPtr windowHandle) {
        WindowInfo window = ResolveWindowByHandle(windowHandle);
        return new DesktopCapture {
            Kind = "window",
            Bitmap = ScreenshotService.CaptureWindow(window.Handle),
            Window = window,
            Geometry = DescribeWindowGeometry(window)
        };
    }

    /// <summary>
    /// Captures a resolved control.
    /// </summary>
    /// <param name="control">Control to capture.</param>
    /// <returns>The captured control image.</returns>
    public DesktopCapture CaptureControl(WindowControlInfo control) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        EnsureControlSupportsNativeStateChange(control, "captured");
        WindowInfo window = ResolveParentWindow(control);
        return new DesktopCapture {
            Kind = "control",
            Bitmap = ScreenshotService.CaptureControl(control.Handle),
            Window = window,
            Control = control,
            Geometry = DescribeWindowGeometry(window)
        };
    }

    /// <summary>
    /// Captures a control resolved by window and control handle.
    /// </summary>
    /// <param name="windowHandle">Parent window handle.</param>
    /// <param name="controlHandle">Control handle.</param>
    /// <param name="useUiAutomation">Whether to request UI Automation discovery.</param>
    /// <param name="includeUiAutomation">Whether to combine Win32 and UI Automation discovery.</param>
    /// <returns>The captured control image.</returns>
    public DesktopCapture CaptureControl(IntPtr windowHandle, IntPtr controlHandle, bool useUiAutomation = true, bool includeUiAutomation = true) {
        WindowControlInfo? control = GetControl(windowHandle, controlHandle, useUiAutomation, includeUiAutomation);
        if (control == null) {
            throw new InvalidOperationException("Failed to resolve the requested control.");
        }

        return CaptureControl(control);
    }

    /// <summary>
    /// Captures the client area of a single matching window, falling back to the full window when client bounds cannot be cropped safely.
    /// </summary>
    /// <param name="options">Window selection options.</param>
    /// <returns>The captured client-area image.</returns>
    public DesktopCapture CaptureWindowClientArea(WindowQueryOptions options) {
        WindowInfo window = ResolveSingleWindow(options);
        DesktopWindowGeometry geometry = DescribeWindowGeometry(window);
        using Bitmap windowBitmap = ScreenshotService.CaptureWindow(window.Handle);
        Bitmap clientBitmap = CreateClientAreaBitmap(windowBitmap, geometry) ?? (Bitmap)windowBitmap.Clone();
        return new DesktopCapture {
            Kind = "window-client",
            Bitmap = clientBitmap,
            Window = window,
            Geometry = geometry
        };
    }

    /// <summary>
    /// Captures the client area of a specific window handle, falling back to the full window when client bounds cannot be cropped safely.
    /// </summary>
    /// <param name="windowHandle">Window handle.</param>
    /// <returns>The captured client-area image.</returns>
    public DesktopCapture CaptureWindowClientArea(IntPtr windowHandle) {
        WindowInfo window = ResolveWindowByHandle(windowHandle);
        DesktopWindowGeometry geometry = DescribeWindowGeometry(window);
        using Bitmap windowBitmap = ScreenshotService.CaptureWindow(window.Handle);
        Bitmap clientBitmap = CreateClientAreaBitmap(windowBitmap, geometry) ?? (Bitmap)windowBitmap.Clone();
        return new DesktopCapture {
            Kind = "window-client",
            Bitmap = clientBitmap,
            Window = window,
            Geometry = geometry
        };
    }

    /// <summary>
    /// Saves the current layout to the specified path.
    /// </summary>
    public void SaveLayout(string path) {
        _windowManager.SaveLayout(path);
    }

    /// <summary>
    /// Loads a layout from the specified path.
    /// </summary>
    public void LoadLayout(string path, bool validate = false) {
        _windowManager.LoadLayout(path, validate);
    }

    private IReadOnlyList<WindowInfo> ResolveWindows(WindowQueryOptions options, bool all) {
        IReadOnlyList<WindowInfo> windows = GetMatchingWindows(options, all);
        if (windows.Count == 0) {
            throw new InvalidOperationException("No matching windows were found.");
        }
        return windows;
    }

    private WindowInfo ResolveSingleWindow(WindowQueryOptions options) {
        return ResolveWindows(options, all: false)[0];
    }

    private static WindowQueryOptions CreatePinnedWindowQuery(WindowInfo window) {
        return new WindowQueryOptions {
            Handle = window.Handle,
            ProcessId = unchecked((int)window.ProcessId),
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        };
    }

    private WindowInfo? TryResolveSingleWindow(WindowQueryOptions options) {
        IReadOnlyList<WindowInfo> windows = GetMatchingWindows(options, all: false);
        return windows.Count > 0 ? windows[0] : null;
    }

    private WindowInfo ResolveParentWindow(WindowControlInfo control) {
        if (control == null) {
            throw new ArgumentNullException(nameof(control));
        }

        if (control.ParentWindowHandle == IntPtr.Zero) {
            throw new InvalidOperationException("The control does not expose parent window metadata.");
        }

        List<WindowInfo> windows = _windowManager.GetWindows(new WindowQueryOptions {
            Handle = control.ParentWindowHandle,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        });
        if (windows.Count == 0) {
            throw new InvalidOperationException("The parent window for the selected control could not be resolved.");
        }

        return windows[0];
    }

    private static void EnsureControlSupportsNativeStateChange(WindowControlInfo control, string actionDescription) {
        if (control.Handle == IntPtr.Zero) {
            throw new InvalidOperationException($"The selected control does not expose a Win32 handle and cannot be {actionDescription} generically.");
        }
    }

    private static void EnsureControlSupportsNativeCheckState(WindowControlInfo control, string actionDescription) {
        EnsureControlSupportsNativeStateChange(control, actionDescription);
        if (!WindowControlService.SupportsCheckState(control)) {
            throw new InvalidOperationException($"The selected control does not expose a native check state and cannot be {actionDescription} generically.");
        }
    }

    private static void EnsureControlSupportsNativeSelection(WindowControlInfo control, string actionDescription) {
        EnsureControlSupportsNativeStateChange(control, actionDescription);
        if (!WindowControlService.SupportsSelection(control)) {
            throw new InvalidOperationException($"The selected control does not expose a native selection model and cannot be {actionDescription}.");
        }
    }

    private DesktopCapture CaptureWindowForVisualObservation(IntPtr windowHandle, string? targetName, bool clientArea) {
        if (windowHandle == IntPtr.Zero) {
            throw new ArgumentException("Invalid window handle.", nameof(windowHandle));
        }

        if (!string.IsNullOrWhiteSpace(targetName)) {
            return CaptureWindowTarget(new WindowQueryOptions {
                Handle = windowHandle,
                IncludeHidden = true,
                IncludeCloaked = true,
                IncludeOwned = true,
                IncludeEmptyTitles = true
            }, targetName!);
        }

        return clientArea ? CaptureWindowClientArea(windowHandle) : CaptureWindow(windowHandle);
    }

    private WindowInfo ResolveWindowByHandle(IntPtr windowHandle) {
        if (windowHandle == IntPtr.Zero) {
            throw new ArgumentException("Invalid window handle.", nameof(windowHandle));
        }

        return GetWindow(windowHandle, includeHidden: true, includeCloaked: true, includeOwned: true, includeEmptyTitles: true)
            ?? throw new InvalidOperationException("The requested window could not be resolved.");
    }

    private DesktopControlState CreateControlState(WindowInfo window, WindowControlInfo control) {
        bool? isVisible = control.Handle != IntPtr.Zero
            ? MonitorNativeMethods.IsWindowVisible(control.Handle)
            : control.IsOffscreen.HasValue ? !control.IsOffscreen.Value : null;
        bool? isEnabled = control.Handle != IntPtr.Zero
            ? MonitorNativeMethods.IsWindowEnabled(control.Handle)
            : control.IsEnabled;
        UiAutomationControlService? uiAutomation = control.Source == WindowControlSource.UiAutomation && control.Handle == IntPtr.Zero
            ? new UiAutomationControlService()
            : null;
        string liveText = control.Handle != IntPtr.Zero
            ? WindowTextHelper.GetWindowText(control.Handle)
            : string.Empty;
        string selectedValue = control.Handle != IntPtr.Zero && WindowControlService.SupportsSelection(control)
            ? WindowControlService.GetSelectedValue(control)
            : uiAutomation?.TryReadSelectedValue(window, control) ?? string.Empty;
        string uiAutomationValue = uiAutomation?.TryReadSelectedValue(window, control) ?? string.Empty;
        string resolvedText = !string.IsNullOrWhiteSpace(liveText)
            ? liveText
            : !string.IsNullOrWhiteSpace(selectedValue)
                ? selectedValue
                : !string.IsNullOrWhiteSpace(control.Text)
                    ? control.Text
                    : uiAutomationValue;
        string resolvedValue = !string.IsNullOrWhiteSpace(selectedValue)
            ? selectedValue
            : !string.IsNullOrWhiteSpace(liveText)
                ? liveText
                : !string.IsNullOrWhiteSpace(control.Value)
                    ? control.Value
                    : !string.IsNullOrWhiteSpace(uiAutomationValue)
                        ? uiAutomationValue
                        : resolvedText;
        bool? isFocused = null;
        IntPtr focusedHandle = WindowActivationService.GetFocusedControlHandle(window.Handle);
        if (focusedHandle != IntPtr.Zero && control.Handle != IntPtr.Zero) {
            isFocused = focusedHandle == control.Handle;
        }

        bool? isChecked = null;
        if (control.Handle != IntPtr.Zero && WindowControlService.SupportsCheckState(control)) {
            isChecked = WindowControlService.GetCheckState(control);
        } else if (uiAutomation != null) {
            isChecked = uiAutomation.TryReadCheckState(window, control);
        }

        return new DesktopControlState {
            WindowHandle = window.Handle,
            ControlHandle = control.Handle,
            ClassName = control.ClassName,
            AutomationId = control.AutomationId,
            ControlType = control.ControlType,
            Text = resolvedText,
            Value = resolvedValue,
            IsEnabled = isEnabled,
            IsVisible = isVisible,
            IsFocused = isFocused,
            IsKeyboardFocusable = control.IsKeyboardFocusable,
            IsOffscreen = control.IsOffscreen,
            IsChecked = isChecked,
            SelectedValue = selectedValue,
            SupportsBackgroundClick = control.SupportsBackgroundClick,
            SupportsBackgroundText = control.SupportsBackgroundText,
            SupportsBackgroundKeys = control.SupportsBackgroundKeys,
            SupportsForegroundInputFallback = control.SupportsForegroundInputFallback,
            Left = control.Left,
            Top = control.Top,
            Width = control.Width,
            Height = control.Height
        };
    }

    private static Bitmap? CreateClientAreaBitmap(Bitmap windowBitmap, DesktopWindowGeometry geometry) {
        if (windowBitmap == null) {
            throw new ArgumentNullException(nameof(windowBitmap));
        }

        if (geometry == null) {
            throw new ArgumentNullException(nameof(geometry));
        }

        int cropLeft = geometry.ClientLeft - geometry.WindowLeft;
        int cropTop = geometry.ClientTop - geometry.WindowTop;
        Rectangle cropBounds = Rectangle.Intersect(
            new Rectangle(cropLeft, cropTop, geometry.ClientWidth, geometry.ClientHeight),
            new Rectangle(0, 0, windowBitmap.Width, windowBitmap.Height));
        if (cropBounds.Width <= 0 || cropBounds.Height <= 0) {
            return null;
        }

        Bitmap croppedBitmap = new(cropBounds.Width, cropBounds.Height);
        using Graphics graphics = Graphics.FromImage(croppedBitmap);
        graphics.DrawImage(
            windowBitmap,
            new Rectangle(0, 0, cropBounds.Width, cropBounds.Height),
            cropBounds,
            GraphicsUnit.Pixel);
        return croppedBitmap;
    }

    private IReadOnlyList<WindowInfo> RefreshWindows(IReadOnlyList<WindowInfo> windows) {
        return windows.Select(RefreshWindow).ToArray();
    }

    private WindowInfo RefreshWindow(WindowInfo window) {
        List<WindowInfo> current = _windowManager.GetWindows(new WindowQueryOptions {
            ProcessId = (int)window.ProcessId,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        });

        return current.FirstOrDefault(candidate => candidate.Handle == window.Handle) ?? window;
    }

    private IReadOnlyList<WindowInfo> GetMatchingWindows(WindowQueryOptions options, bool all) {
        List<WindowInfo> windows = _windowManager.GetWindows(options);
        if (windows.Count == 0) {
            return Array.Empty<WindowInfo>();
        }

        if (all) {
            return windows;
        }

        return new[] { windows[0] };
    }

    private IReadOnlyList<WindowInfo> GetWindowsByHandle(IEnumerable<IntPtr> handles, bool all) {
        var windows = new List<WindowInfo>();
        foreach (IntPtr handle in handles) {
            if (handle == IntPtr.Zero) {
                continue;
            }

            List<WindowInfo> matches = _windowManager.GetWindows(new WindowQueryOptions {
                Handle = handle,
                IncludeHidden = true,
                IncludeCloaked = true,
                IncludeOwned = true,
                IncludeEmptyTitles = true
            });
            if (matches.Count == 0) {
                continue;
            }

            windows.Add(matches[0]);
            if (!all) {
                break;
            }
        }

        return windows;
    }

    private IReadOnlyList<WindowControlTargetInfo> GetControls(IReadOnlyList<WindowInfo> windows, WindowControlQueryOptions? controlOptions, bool allControls) {
        if (windows == null || windows.Count == 0) {
            return Array.Empty<WindowControlTargetInfo>();
        }

        var results = new List<WindowControlTargetInfo>();
        foreach (WindowInfo window in windows) {
            List<WindowControlInfo> controls = _windowManager.GetControls(window, controlOptions);
            foreach (WindowControlInfo control in controls) {
                results.Add(new WindowControlTargetInfo {
                    Window = window,
                    Control = control
                });

                if (!allControls) {
                    return results;
                }
            }
        }

        return results;
    }

    private static void ValidateWaitArguments(int timeoutMilliseconds, int intervalMilliseconds) {
        if (timeoutMilliseconds < 0) {
            throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds), "timeoutMilliseconds must be zero or greater.");
        }

        if (intervalMilliseconds <= 0) {
            throw new ArgumentOutOfRangeException(nameof(intervalMilliseconds), "intervalMilliseconds must be greater than zero.");
        }
    }

    private static void ValidateTextObservationOptions(DesktopTextObservationOptions options) {
        if (options == null) {
            throw new ArgumentNullException(nameof(options));
        }

        if (options.MaxObservedTextLength < 1) {
            throw new ArgumentOutOfRangeException(nameof(options.MaxObservedTextLength), "MaxObservedTextLength must be greater than zero.");
        }

        if (options.RetryCount < 1) {
            throw new ArgumentOutOfRangeException(nameof(options.RetryCount), "RetryCount must be greater than zero.");
        }

        if (options.RetryDelayMilliseconds < 0) {
            throw new ArgumentOutOfRangeException(nameof(options.RetryDelayMilliseconds), "RetryDelayMilliseconds must be zero or greater.");
        }
    }

    private DesktopWindowTextObservation? TryObserveWindowText(WindowInfo window, string? expectedText, DesktopTextObservationOptions observationOptions) {
        DesktopWindowTextObservation? focusedObservation = TryObserveFocusedWindowText(window, expectedText, observationOptions.MaxObservedTextLength);
        if (focusedObservation?.ContainsExpected == true || string.IsNullOrEmpty(expectedText)) {
            return focusedObservation;
        }

        List<WindowControlInfo> controls = _windowManager.GetControls(
            window,
            new WindowControlQueryOptions {
                UseUiAutomation = true,
                IncludeUiAutomation = true
            });
        List<(WindowControlInfo Control, DesktopWindowTextObservation Observation)> candidates = controls
            .Where(IsEditableTextCandidate)
            .Select(control => CreateControlTextObservation(window, control, expectedText, observationOptions.MaxObservedTextLength))
            .Where(candidate => candidate.HasValue)
            .Select(candidate => candidate!.Value)
            .ToList();

        DesktopWindowTextObservation? controlObservation = candidates
            .OrderByDescending(candidate => candidate.Observation.ContainsExpected == true)
            .ThenByDescending(candidate => GetEditableTextCandidateScore(candidate.Control))
            .ThenByDescending(candidate => candidate.Observation.Value.Length)
            .Select(candidate => candidate.Observation)
            .FirstOrDefault();
        if (controlObservation != null) {
            return controlObservation;
        }

        if (!string.IsNullOrWhiteSpace(window.Title)) {
            return CreateTextObservation(
                window,
                IntPtr.Zero,
                string.Empty,
                string.Empty,
                string.Empty,
                window.Title,
                "window.title",
                expectedText,
                observationOptions.MaxObservedTextLength);
        }

        return focusedObservation;
    }

    private DesktopWindowTextObservation? TryObserveFocusedWindowText(WindowInfo window, string? expectedText, int maxObservedTextLength) {
        DesktopFocusedControlObservation? focusedControl = GetFocusedControlObservation(new WindowQueryOptions {
            Handle = window.Handle,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        });
        if (focusedControl == null || focusedControl.FocusedHandle == IntPtr.Zero) {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(focusedControl.Value)) {
            return CreateTextObservation(
                window,
                focusedControl.FocusedHandle,
                focusedControl.ClassName,
                focusedControl.AutomationId,
                focusedControl.ControlType,
                focusedControl.Value,
                "focused.controlValue",
                expectedText,
                maxObservedTextLength);
        }

        if (!string.IsNullOrWhiteSpace(focusedControl.Text)) {
            return CreateTextObservation(
                window,
                focusedControl.FocusedHandle,
                focusedControl.ClassName,
                focusedControl.AutomationId,
                focusedControl.ControlType,
                focusedControl.Text,
                "focused.controlText",
                expectedText,
                maxObservedTextLength);
        }

        return null;
    }

    private static (WindowControlInfo Control, DesktopWindowTextObservation Observation)? CreateControlTextObservation(WindowInfo window, WindowControlInfo control, string? expectedText, int maxObservedTextLength) {
        string? rawValue = !string.IsNullOrWhiteSpace(control.Value)
            ? control.Value
            : !string.IsNullOrWhiteSpace(control.Text)
                ? control.Text
                : control.Handle != IntPtr.Zero
                    ? WindowTextHelper.GetWindowText(control.Handle)
                    : null;
        if (rawValue == null) {
            return null;
        }

        string source = !string.IsNullOrWhiteSpace(control.Value)
            ? "control.value"
            : !string.IsNullOrWhiteSpace(control.Text)
                ? "control.text"
                : "control.liveText";

        DesktopWindowTextObservation observation = CreateTextObservation(
            window,
            control.Handle,
            control.ClassName,
            control.AutomationId,
            control.ControlType,
            rawValue,
            source,
            expectedText,
            maxObservedTextLength);
        return (control, observation);
    }

    internal static bool IsEditableTextCandidate(WindowControlInfo control) {
        if (control == null) {
            return false;
        }

        string className = control.ClassName ?? string.Empty;
        string controlType = control.ControlType ?? string.Empty;

        return className.Equals("RichEditD2DPT", StringComparison.OrdinalIgnoreCase) ||
            className.Equals("NotepadTextBox", StringComparison.OrdinalIgnoreCase) ||
            className.IndexOf("RichEdit", StringComparison.OrdinalIgnoreCase) >= 0 ||
            className.IndexOf("Edit", StringComparison.OrdinalIgnoreCase) >= 0 ||
            controlType.Equals("Edit", StringComparison.OrdinalIgnoreCase) ||
            controlType.Equals("Document", StringComparison.OrdinalIgnoreCase) ||
            (control.SupportsBackgroundText && control.IsKeyboardFocusable != false);
    }

    internal static int GetEditableTextCandidateScore(WindowControlInfo control) {
        if (control == null) {
            return 0;
        }

        int score = 0;
        string className = control.ClassName ?? string.Empty;
        string controlType = control.ControlType ?? string.Empty;

        if (className.Equals("RichEditD2DPT", StringComparison.OrdinalIgnoreCase)) {
            score += 200;
        } else if (className.Equals("NotepadTextBox", StringComparison.OrdinalIgnoreCase)) {
            score += 180;
        } else if (className.IndexOf("RichEdit", StringComparison.OrdinalIgnoreCase) >= 0) {
            score += 160;
        } else if (className.IndexOf("Edit", StringComparison.OrdinalIgnoreCase) >= 0) {
            score += 140;
        }

        if (controlType.Equals("Edit", StringComparison.OrdinalIgnoreCase)) {
            score += 120;
        } else if (controlType.Equals("Document", StringComparison.OrdinalIgnoreCase)) {
            score += 80;
        }

        if (control.Handle != IntPtr.Zero) {
            score += 40;
        }

        if (control.SupportsBackgroundText) {
            score += 30;
        }

        if (control.IsKeyboardFocusable != false) {
            score += 20;
        }

        if (control.IsEnabled != false) {
            score += 10;
        }

        if (!string.IsNullOrWhiteSpace(control.Value)) {
            score += 20;
        }

        if (!string.IsNullOrWhiteSpace(control.Text)) {
            score += 10;
        }

        return score;
    }

    internal static DesktopWindowTextObservation CreateTextObservation(WindowInfo window, IntPtr controlHandle, string? controlClassName, string? controlAutomationId, string? controlType, string? value, string source, string? expectedText, int maxObservedTextLength) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        if (value == null) {
            throw new ArgumentNullException(nameof(value));
        }

        if (string.IsNullOrWhiteSpace(source)) {
            throw new ArgumentException("source is required.", nameof(source));
        }

        if (maxObservedTextLength < 1) {
            throw new ArgumentOutOfRangeException(nameof(maxObservedTextLength), "maxObservedTextLength must be greater than zero.");
        }

        bool isTruncated = value.Length > maxObservedTextLength;
        string normalizedValue = isTruncated
            ? value.Substring(0, maxObservedTextLength)
            : value;
        bool? containsExpected = string.IsNullOrEmpty(expectedText)
            ? null
            : value.IndexOf(expectedText, StringComparison.Ordinal) >= 0 ? true : null;

        return new DesktopWindowTextObservation {
            WindowHandle = window.Handle,
            WindowTitle = window.Title,
            ControlHandle = controlHandle,
            ControlClassName = controlClassName ?? string.Empty,
            ControlAutomationId = controlAutomationId ?? string.Empty,
            ControlType = controlType ?? string.Empty,
            Value = normalizedValue,
            Source = source,
            ContainsExpected = containsExpected,
            IsTruncated = isTruncated
        };
    }

    private static void RememberWindowHandles(ISet<IntPtr> handles, IEnumerable<WindowInfo> windows) {
        if (handles == null) {
            throw new ArgumentNullException(nameof(handles));
        }

        handles.Clear();
        foreach (WindowInfo window in windows) {
            if (window != null && window.Handle != IntPtr.Zero) {
                handles.Add(window.Handle);
            }
        }
    }

    private IReadOnlyList<WindowControlTargetInfo> RequireControls(WindowQueryOptions windowOptions, WindowControlQueryOptions? controlOptions, bool allWindows, bool allControls) {
        IReadOnlyList<WindowControlTargetInfo> controls = GetControls(windowOptions, controlOptions, allWindows, allControls);
        if (controls.Count == 0) {
            throw new InvalidOperationException("No matching controls were found.");
        }

        return controls;
    }

    private WindowInfo? TryGetPreferredWindowForProcess(int processId, string filePath) {
        List<WindowInfo> windows = _windowManager.GetWindows(new WindowQueryOptions {
            ProcessId = processId,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        });

        WindowInfo? preferred = windows.FirstOrDefault(window => !string.IsNullOrWhiteSpace(window.Title))
            ?? windows.FirstOrDefault();
        if (preferred != null) {
            return preferred;
        }

        string processName = Path.GetFileNameWithoutExtension(filePath);
        if (string.IsNullOrWhiteSpace(processName)) {
            return null;
        }

        windows = _windowManager.GetWindows(new WindowQueryOptions {
            ProcessNamePattern = processName,
            IncludeHidden = true,
            IncludeCloaked = true,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        });

        return windows.FirstOrDefault(window => !string.IsNullOrWhiteSpace(window.Title))
            ?? windows.FirstOrDefault();
    }

    private WindowInfo? TryResolveLaunchedWindow(int launcherProcessId, IReadOnlyList<string> processNameHints, DateTime launchedAtUtc, ISet<IntPtr> preLaunchWindowHandles, string? windowTitlePattern, string? windowClassNamePattern, int waitForWindowMilliseconds, int waitForWindowIntervalMilliseconds) {
        Stopwatch stopwatch = Stopwatch.StartNew();
        do {
            WindowInfo? candidate = FindBestLaunchedWindowCandidate(launcherProcessId, processNameHints, launchedAtUtc, preLaunchWindowHandles, windowTitlePattern, windowClassNamePattern);
            if (candidate != null) {
                return candidate;
            }

            if (waitForWindowMilliseconds == 0 || stopwatch.ElapsedMilliseconds >= waitForWindowMilliseconds) {
                return null;
            }

            Thread.Sleep(waitForWindowIntervalMilliseconds);
        } while (true);
    }

    private WindowInfo? TryResolveImmediateLaunchedWindow(Process process, string? windowTitlePattern, string? windowClassNamePattern) {
        try {
            process.Refresh();
            IntPtr mainWindowHandle = process.MainWindowHandle;
            if (mainWindowHandle == IntPtr.Zero) {
                return null;
            }

            WindowInfo? window = _windowManager.GetWindow(mainWindowHandle, includeHidden: true, includeCloaked: true, includeOwned: true, includeEmptyTitles: true);
            if (window == null || !MatchesLaunchedWindow(window.Title ?? string.Empty, mainWindowHandle, windowTitlePattern, windowClassNamePattern)) {
                return null;
            }

            return window;
        } catch {
            return null;
        }
    }

    private WindowInfo? FindBestLaunchedWindowCandidate(int launcherProcessId, IReadOnlyList<string> processNameHints, DateTime launchedAtUtc, ISet<IntPtr> preLaunchWindowHandles, string? windowTitlePattern, string? windowClassNamePattern) {
        List<LaunchWindowCandidate> candidates = new();

        IntPtr shellWindow = MonitorNativeMethods.GetShellWindow();
        if (!MonitorNativeMethods.EnumWindows((handle, lParam) => {
            if (handle == shellWindow || !MonitorNativeMethods.IsWindowVisible(handle)) {
                return true;
            }

            IntPtr ownerHandle = MonitorNativeMethods.GetWindow(handle, MonitorNativeMethods.GW_OWNER);
            if (ownerHandle != IntPtr.Zero) {
                return true;
            }

            MonitorNativeMethods.TryGetWindowCloaked(handle, out bool isCloaked);
            if (isCloaked) {
                return true;
            }

            uint windowProcessId = 0;
            uint windowThreadId = MonitorNativeMethods.GetWindowThreadProcessId(handle, out windowProcessId);
            bool exactProcessMatch = windowProcessId == launcherProcessId;
            int hintPriority = int.MaxValue;

            if (!exactProcessMatch) {
                string? processName = TryGetProcessName((int)windowProcessId);
                if (string.IsNullOrWhiteSpace(processName)) {
                    return true;
                }

                hintPriority = GetProcessNameHintPriority(processNameHints, processName!);
                if (hintPriority < 0) {
                    return true;
                }
            }

            DateTime? startedUtc = TryGetProcessStartTimeUtc((int)windowProcessId, out DateTime processStartedUtc)
                ? processStartedUtc
                : null;
            bool newHandleAfterLaunch = IsNewLaunchedWindowCandidate(handle, preLaunchWindowHandles, startedUtc, launchedAtUtc);
            if (!exactProcessMatch && !newHandleAfterLaunch && startedUtc.HasValue && startedUtc.Value < launchedAtUtc.AddSeconds(-2)) {
                return true;
            }

            string title = WindowTextHelper.GetWindowText(handle);
            if (string.IsNullOrWhiteSpace(title) || !MatchesLaunchedWindow(title, handle, windowTitlePattern, windowClassNamePattern)) {
                return true;
            }

            WindowInfo window = _windowManager.GetWindow(handle, includeHidden: true, includeCloaked: true, includeOwned: true, includeEmptyTitles: true)
                ?? new WindowInfo {
                    Title = title,
                    Handle = handle,
                    ProcessId = windowProcessId,
                    ThreadId = windowThreadId,
                    IsVisible = true,
                    OwnerHandle = ownerHandle,
                    IsCloaked = false
                };

            candidates.Add(new LaunchWindowCandidate(window, startedUtc, exactProcessMatch, newHandleAfterLaunch, hintPriority));
            return true;
        }, IntPtr.Zero)) {
            throw new InvalidOperationException("Failed to enumerate launch window candidates.");
        }

        return candidates
            .OrderByDescending(candidate => candidate.ExactProcessMatch)
            .ThenBy(candidate => candidate.HintPriority)
            .ThenByDescending(candidate => candidate.NewHandleAfterLaunch)
            .ThenByDescending(candidate => !string.IsNullOrWhiteSpace(candidate.Window.Title))
            .ThenByDescending(candidate => candidate.ProcessStartedUtc ?? DateTime.MinValue)
            .Select(candidate => candidate.Window)
            .FirstOrDefault();
    }

    private bool MatchesLaunchedWindow(string title, IntPtr handle, string? windowTitlePattern, string? windowClassNamePattern) {
        if (!string.IsNullOrWhiteSpace(windowTitlePattern) && !MatchesPattern(title, windowTitlePattern!)) {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(windowClassNamePattern) && !MatchesPattern(GetWindowClassName(handle), windowClassNamePattern!)) {
            return false;
        }

        return true;
    }

    private HashSet<IntPtr> CaptureWindowHandlesForProcesses(IReadOnlyList<string> processNameHints) {
        if (processNameHints.Count == 0) {
            return new HashSet<IntPtr>();
        }

        HashSet<IntPtr> handles = new();
        foreach (string processNameHint in processNameHints) {
            foreach (WindowInfo window in _windowManager.GetWindows(new WindowQueryOptions {
                ProcessNamePattern = processNameHint,
                IncludeHidden = true,
                IncludeCloaked = true,
                IncludeOwned = true,
                IncludeEmptyTitles = true
            })) {
                handles.Add(window.Handle);
            }
        }

        return handles;
    }

    private static IReadOnlyList<DesktopOcrLine> CloneOcrLines(IReadOnlyList<DesktopOcrLine> lines) {
        return lines.Select(line => new DesktopOcrLine {
            Text = line.Text,
            X = line.X,
            Y = line.Y,
            Width = line.Width,
            Height = line.Height,
            Words = CloneOcrWords(line.Words)
        }).ToArray();
    }

    private static IReadOnlyList<DesktopOcrWord> CloneOcrWords(IReadOnlyList<DesktopOcrWord> words) {
        return words.Select(word => new DesktopOcrWord {
            Text = word.Text,
            X = word.X,
            Y = word.Y,
            Width = word.Width,
            Height = word.Height
        }).ToArray();
    }

    private static void TryAddTextMatchCandidate(
        ICollection<(int Priority, int LengthDelta, int Area, string MatchKind, string MatchedText, int X, int Y, int Width, int Height, IReadOnlyList<DesktopOcrWord> Words)> candidates,
        string candidateText,
        string matchKind,
        int x,
        int y,
        int width,
        int height,
        IReadOnlyList<DesktopOcrWord> words,
        string normalizedQuery,
        bool contains) {
        string normalizedCandidate = NormalizeOcrSearchText(candidateText);
        if (string.IsNullOrWhiteSpace(normalizedCandidate)) {
            return;
        }

        bool exactMatch = string.Equals(normalizedCandidate, normalizedQuery, StringComparison.OrdinalIgnoreCase);
        bool matched = exactMatch || contains && normalizedCandidate.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase);
        if (!matched) {
            return;
        }

        int priority = exactMatch
            ? string.Equals(matchKind, "word", StringComparison.Ordinal) ? 0 : 1
            : string.Equals(matchKind, "word", StringComparison.Ordinal) ? 2 : 3;
        candidates.Add((
            priority,
            Math.Abs(normalizedCandidate.Length - normalizedQuery.Length),
            Math.Max(1, width) * Math.Max(1, height),
            matchKind,
            candidateText,
            x,
            y,
            width,
            height,
            CloneOcrWords(words)));
    }

    private static string NormalizeOcrSearchText(string value) {
        return Regex.Replace(value ?? string.Empty, "\\s+", " ").Trim();
    }

    private static string? GetProcessNameHint(string filePath) {
        string executableName = Path.GetFileNameWithoutExtension(filePath.Trim().Trim('"'));
        return string.IsNullOrWhiteSpace(executableName) ? null : executableName;
    }

    private static string? GetProcessNameHint(Process process, string filePath) {
        try {
            if (!process.HasExited && !string.IsNullOrWhiteSpace(process.ProcessName)) {
                return process.ProcessName;
            }
        } catch {
        }

        return GetProcessNameHint(filePath);
    }

    private static IReadOnlyList<string> CollectProcessNameHints(params string?[] values) {
        List<string> hints = new();
        foreach (string? value in values) {
            if (string.IsNullOrWhiteSpace(value)) {
                continue;
            }

            if (hints.Any(existing => string.Equals(existing, value, StringComparison.OrdinalIgnoreCase))) {
                continue;
            }

            hints.Add(value!);
        }

        return hints;
    }

    private static bool ShouldUseShellExecute(string filePath) {
        string trimmedPath = filePath?.Trim().Trim('"') ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedPath)) {
            return true;
        }

        string extension = Path.GetExtension(trimmedPath);
        if (string.IsNullOrWhiteSpace(extension)) {
            return false;
        }

        return !extension.Equals(".exe", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".com", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNewLaunchedWindowCandidate(IntPtr handle, ISet<IntPtr> preLaunchWindowHandles, DateTime? processStartedUtc, DateTime launchedAtUtc) {
        if (preLaunchWindowHandles.Count > 0) {
            return !preLaunchWindowHandles.Contains(handle);
        }

        return !processStartedUtc.HasValue || processStartedUtc.Value >= launchedAtUtc.AddSeconds(-2);
    }

    private static string BuildMissingLaunchedWindowMessage(IReadOnlyList<string> processNameHints, string? windowTitlePattern, string? windowClassNamePattern, int waitForWindowMilliseconds) {
        string processText = processNameHints.Count == 0 ? "the launched application" : string.Join(", ", processNameHints);
        if (!string.IsNullOrWhiteSpace(windowTitlePattern) || !string.IsNullOrWhiteSpace(windowClassNamePattern)) {
            return $"Timed out after {waitForWindowMilliseconds}ms waiting for a launched window that matched process '{processText}', title '{windowTitlePattern ?? "*"}', and class '{windowClassNamePattern ?? "*"}'.";
        }

        return $"Timed out after {waitForWindowMilliseconds}ms waiting for a launched window for '{processText}'.";
    }

    private static string GetWindowClassName(IntPtr handle) {
        if (handle == IntPtr.Zero) {
            return string.Empty;
        }

        System.Text.StringBuilder builder = new(256);
        MonitorNativeMethods.GetClassName(handle, builder, builder.Capacity);
        return builder.ToString();
    }

    private static bool MatchesPattern(string text, string pattern) {
        if (string.IsNullOrEmpty(pattern)) {
            return false;
        }

        if (pattern.Contains('*') || pattern.Contains('?')) {
            string regexPattern = "^" + Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";

            return Regex.IsMatch(text, regexPattern, RegexOptions.IgnoreCase);
        }

        return text.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool TryGetProcessStartTimeUtc(int processId, out DateTime startTimeUtc) {
        try {
            using Process process = Process.GetProcessById(processId);
            startTimeUtc = process.StartTime.ToUniversalTime();
            return true;
        } catch {
            startTimeUtc = default;
            return false;
        }
    }

    private static string? TryGetProcessName(int processId) {
        try {
            using Process process = Process.GetProcessById(processId);
            return process.ProcessName;
        } catch {
            return null;
        }
    }

    private static int GetProcessNameHintPriority(IReadOnlyList<string> processNameHints, string processName) {
        for (int index = 0; index < processNameHints.Count; index++) {
            if (string.Equals(processNameHints[index], processName, StringComparison.OrdinalIgnoreCase)) {
                return index;
            }
        }

        return -1;
    }

    private IReadOnlyList<DesktopResolvedWindowTarget> ResolveWindowTargets(WindowQueryOptions options, string targetName, DesktopWindowTargetDefinition definition, bool all) {
        IReadOnlyList<WindowInfo> windows = ResolveWindows(options, all);
        return windows
            .Select(window => ResolveWindowTarget(targetName, definition, DescribeWindowGeometry(window)))
            .ToArray();
    }

    private static DesktopResolvedWindowTarget ResolveWindowTarget(string targetName, DesktopWindowTargetDefinition definition, DesktopWindowGeometry geometry) {
        (int relativeX, int relativeY) = ResolveRelativePoint(geometry, definition.X, definition.Y, definition.XRatio, definition.YRatio, definition.ClientArea);
        int? relativeWidth = ResolveOptionalAxisSize(geometry, definition.Width, definition.WidthRatio, definition.ClientArea, horizontal: true, nameof(definition.Width), nameof(definition.WidthRatio));
        int? relativeHeight = ResolveOptionalAxisSize(geometry, definition.Height, definition.HeightRatio, definition.ClientArea, horizontal: false, nameof(definition.Height), nameof(definition.HeightRatio));
        int screenX = definition.ClientArea ? geometry.ClientLeft + relativeX : geometry.WindowLeft + relativeX;
        int screenY = definition.ClientArea ? geometry.ClientTop + relativeY : geometry.WindowTop + relativeY;

        return new DesktopResolvedWindowTarget {
            Name = targetName,
            Definition = new DesktopWindowTargetDefinition {
                Description = definition.Description,
                X = definition.X,
                Y = definition.Y,
                XRatio = definition.XRatio,
                YRatio = definition.YRatio,
                Width = definition.Width,
                Height = definition.Height,
                WidthRatio = definition.WidthRatio,
                HeightRatio = definition.HeightRatio,
                ClientArea = definition.ClientArea
            },
            Geometry = geometry,
            RelativeX = relativeX,
            RelativeY = relativeY,
            RelativeWidth = relativeWidth,
            RelativeHeight = relativeHeight,
            ScreenX = screenX,
            ScreenY = screenY,
            ScreenWidth = relativeWidth,
            ScreenHeight = relativeHeight
        };
    }

    private static DesktopControlTargetDefinition CloneControlTargetDefinition(DesktopControlTargetDefinition definition) {
        return new DesktopControlTargetDefinition {
            Description = definition.Description,
            ClassNamePattern = definition.ClassNamePattern,
            TextPattern = definition.TextPattern,
            ValuePattern = definition.ValuePattern,
            Id = definition.Id,
            Handle = definition.Handle,
            AutomationIdPattern = definition.AutomationIdPattern,
            ControlTypePattern = definition.ControlTypePattern,
            FrameworkIdPattern = definition.FrameworkIdPattern,
            IsEnabled = definition.IsEnabled,
            IsKeyboardFocusable = definition.IsKeyboardFocusable,
            SupportsBackgroundClick = definition.SupportsBackgroundClick,
            SupportsBackgroundText = definition.SupportsBackgroundText,
            SupportsBackgroundKeys = definition.SupportsBackgroundKeys,
            SupportsForegroundInputFallback = definition.SupportsForegroundInputFallback,
            UseUiAutomation = definition.UseUiAutomation,
            IncludeUiAutomation = definition.IncludeUiAutomation,
            EnsureForegroundWindow = definition.EnsureForegroundWindow
        };
    }

    private static void ValidateWindowTargetDefinition(DesktopWindowTargetDefinition definition) {
        ValidateTargetAxis(definition.X, definition.XRatio, nameof(definition.X), nameof(definition.XRatio));
        ValidateTargetAxis(definition.Y, definition.YRatio, nameof(definition.Y), nameof(definition.YRatio));
        ValidateOptionalTargetSizeAxis(definition.Width, definition.WidthRatio, nameof(definition.Width), nameof(definition.WidthRatio));
        ValidateOptionalTargetSizeAxis(definition.Height, definition.HeightRatio, nameof(definition.Height), nameof(definition.HeightRatio));
    }

    private static void ValidateControlTargetDefinition(DesktopControlTargetDefinition definition) {
        WindowControlQueryOptions query = CreateControlQuery(definition);
        bool hasSelector =
            query.Id.HasValue ||
            query.Handle.HasValue ||
            !IsWildcard(query.ClassNamePattern) ||
            !IsWildcard(query.TextPattern) ||
            !IsWildcard(query.ValuePattern) ||
            !IsWildcard(query.AutomationIdPattern) ||
            !IsWildcard(query.ControlTypePattern) ||
            !IsWildcard(query.FrameworkIdPattern) ||
            query.IsEnabled.HasValue ||
            query.IsKeyboardFocusable.HasValue ||
            query.SupportsBackgroundClick.HasValue ||
            query.SupportsBackgroundText.HasValue ||
            query.SupportsBackgroundKeys.HasValue ||
            query.SupportsForegroundInputFallback.HasValue;
        if (!hasSelector) {
            throw new ArgumentException("A control target must include at least one selector or capability requirement.", nameof(definition));
        }
    }

    private sealed class LaunchWindowCandidate {
        public LaunchWindowCandidate(WindowInfo window, DateTime? processStartedUtc, bool exactProcessMatch, bool newHandleAfterLaunch, int hintPriority = int.MaxValue) {
            Window = window;
            ProcessStartedUtc = processStartedUtc;
            ExactProcessMatch = exactProcessMatch;
            NewHandleAfterLaunch = newHandleAfterLaunch;
            HintPriority = hintPriority;
        }

        public WindowInfo Window { get; }
        public DateTime? ProcessStartedUtc { get; }
        public bool ExactProcessMatch { get; }
        public bool NewHandleAfterLaunch { get; }
        public int HintPriority { get; }
    }

    private static void ValidateTargetAxis(int? coordinate, double? ratio, string coordinateName, string ratioName) {
        bool hasCoordinate = coordinate.HasValue;
        bool hasRatio = ratio.HasValue;
        if (hasCoordinate == hasRatio) {
            throw new ArgumentException($"Provide either {coordinateName} or {ratioName}, but not both.");
        }

        if (hasCoordinate) {
            if (coordinate!.Value < 0) {
                throw new ArgumentOutOfRangeException(coordinateName, $"{coordinateName} must be zero or greater.");
            }

            return;
        }

        double ratioValue = ratio!.Value;
        if (ratioValue < 0 || ratioValue > 1) {
            throw new ArgumentOutOfRangeException(ratioName, $"{ratioName} must be between 0 and 1.");
        }
    }

    private static void ValidateOptionalTargetSizeAxis(int? size, double? ratio, string sizeName, string ratioName) {
        bool hasSize = size.HasValue;
        bool hasRatio = ratio.HasValue;
        if (!hasSize && !hasRatio) {
            return;
        }

        if (hasSize == hasRatio) {
            throw new ArgumentException($"Provide either {sizeName} or {ratioName}, but not both.");
        }

        if (hasSize) {
            if (size!.Value <= 0) {
                throw new ArgumentOutOfRangeException(sizeName, $"{sizeName} must be greater than zero.");
            }

            return;
        }

        double ratioValue = ratio!.Value;
        if (ratioValue <= 0 || ratioValue > 1) {
            throw new ArgumentOutOfRangeException(ratioName, $"{ratioName} must be greater than 0 and less than or equal to 1.");
        }
    }

    private static WindowControlQueryOptions CreateControlQuery(DesktopControlTargetDefinition definition) {
        return new WindowControlQueryOptions {
            ClassNamePattern = string.IsNullOrWhiteSpace(definition.ClassNamePattern) ? "*" : definition.ClassNamePattern,
            TextPattern = string.IsNullOrWhiteSpace(definition.TextPattern) ? "*" : definition.TextPattern,
            ValuePattern = string.IsNullOrWhiteSpace(definition.ValuePattern) ? "*" : definition.ValuePattern,
            Id = definition.Id,
            Handle = string.IsNullOrWhiteSpace(definition.Handle) ? null : DesktopHandleParser.Parse(definition.Handle!),
            AutomationIdPattern = string.IsNullOrWhiteSpace(definition.AutomationIdPattern) ? "*" : definition.AutomationIdPattern,
            ControlTypePattern = string.IsNullOrWhiteSpace(definition.ControlTypePattern) ? "*" : definition.ControlTypePattern,
            FrameworkIdPattern = string.IsNullOrWhiteSpace(definition.FrameworkIdPattern) ? "*" : definition.FrameworkIdPattern,
            IsEnabled = definition.IsEnabled,
            IsKeyboardFocusable = definition.IsKeyboardFocusable,
            SupportsBackgroundClick = definition.SupportsBackgroundClick,
            SupportsBackgroundText = definition.SupportsBackgroundText,
            SupportsBackgroundKeys = definition.SupportsBackgroundKeys,
            SupportsForegroundInputFallback = definition.SupportsForegroundInputFallback,
            UseUiAutomation = definition.UseUiAutomation,
            IncludeUiAutomation = definition.IncludeUiAutomation,
            EnsureForegroundWindow = definition.EnsureForegroundWindow
        };
    }

    private static bool IsWildcard(string? value) {
        return string.IsNullOrWhiteSpace(value) || value == "*";
    }

    private static (int X, int Y) ResolveWindowPoint(WindowInfo window, int? x, int? y, double? xRatio, double? yRatio, bool clientArea) {
        DesktopWindowGeometry geometry = DescribeWindowGeometry(window);
        (int resolvedX, int resolvedY) = ResolveRelativePoint(geometry, x, y, xRatio, yRatio, clientArea);

        if (!clientArea) {
            return (geometry.WindowLeft + resolvedX, geometry.WindowTop + resolvedY);
        }

        return (geometry.ClientLeft + resolvedX, geometry.ClientTop + resolvedY);
    }

    private static DesktopWindowGeometry DescribeWindowGeometry(WindowInfo window) {
        if (window == null) {
            throw new ArgumentNullException(nameof(window));
        }

        int clientLeft = window.Left;
        int clientTop = window.Top;
        int clientWidth = window.Width;
        int clientHeight = window.Height;

        if (MonitorNativeMethods.GetClientRect(window.Handle, out RECT clientRect)) {
            var clientOrigin = new MonitorNativeMethods.POINT {
                x = 0,
                y = 0
            };
            if (MonitorNativeMethods.ClientToScreen(window.Handle, ref clientOrigin)) {
                clientLeft = clientOrigin.x;
                clientTop = clientOrigin.y;
                clientWidth = Math.Max(0, clientRect.Right - clientRect.Left);
                clientHeight = Math.Max(0, clientRect.Bottom - clientRect.Top);
            }
        }

        return new DesktopWindowGeometry {
            Window = window,
            WindowLeft = window.Left,
            WindowTop = window.Top,
            WindowWidth = window.Width,
            WindowHeight = window.Height,
            ClientLeft = clientLeft,
            ClientTop = clientTop,
            ClientWidth = clientWidth,
            ClientHeight = clientHeight,
            ClientOffsetLeft = clientLeft - window.Left,
            ClientOffsetTop = clientTop - window.Top
        };
    }

    private static (int X, int Y) ResolveRelativePoint(DesktopWindowGeometry geometry, int? x, int? y, double? xRatio, double? yRatio, bool clientArea) {
        int width = clientArea ? geometry.ClientWidth : geometry.WindowWidth;
        int height = clientArea ? geometry.ClientHeight : geometry.WindowHeight;

        return (ResolveAxisCoordinate(x, xRatio, width, nameof(x), nameof(xRatio)), ResolveAxisCoordinate(y, yRatio, height, nameof(y), nameof(yRatio)));
    }

    private static int? ResolveOptionalAxisSize(DesktopWindowGeometry geometry, int? size, double? ratio, bool clientArea, bool horizontal, string sizeName, string ratioName) {
        bool hasSize = size.HasValue;
        bool hasRatio = ratio.HasValue;
        if (!hasSize && !hasRatio) {
            return null;
        }

        int boundsSize = horizontal
            ? clientArea ? geometry.ClientWidth : geometry.WindowWidth
            : clientArea ? geometry.ClientHeight : geometry.WindowHeight;
        if (hasSize) {
            if (size!.Value <= 0) {
                throw new ArgumentOutOfRangeException(sizeName, $"{sizeName} must be greater than zero.");
            }

            return size.Value;
        }

        double ratioValue = ratio!.Value;
        if (ratioValue <= 0 || ratioValue > 1) {
            throw new ArgumentOutOfRangeException(ratioName, $"{ratioName} must be greater than 0 and less than or equal to 1.");
        }

        if (boundsSize <= 0) {
            throw new InvalidOperationException("The target bounds do not expose a usable size.");
        }

        return Math.Max(1, (int)Math.Round(boundsSize * ratioValue, MidpointRounding.AwayFromZero));
    }

    private static int ResolveAxisCoordinate(int? coordinate, double? ratio, int size, string coordinateName, string ratioName) {
        bool hasCoordinate = coordinate.HasValue;
        bool hasRatio = ratio.HasValue;
        if (hasCoordinate == hasRatio) {
            throw new ArgumentException($"Provide either {coordinateName} or {ratioName}, but not both.");
        }

        if (hasCoordinate) {
            if (coordinate!.Value < 0) {
                throw new ArgumentOutOfRangeException(coordinateName, $"{coordinateName} must be zero or greater.");
            }

            return coordinate.Value;
        }

        double ratioValue = ratio!.Value;
        if (ratioValue < 0 || ratioValue > 1) {
            throw new ArgumentOutOfRangeException(ratioName, $"{ratioName} must be between 0 and 1.");
        }

        if (size <= 0) {
            throw new InvalidOperationException("The target bounds do not expose a usable size.");
        }

        return (int)Math.Round((size - 1) * ratioValue, MidpointRounding.AwayFromZero);
    }
}
