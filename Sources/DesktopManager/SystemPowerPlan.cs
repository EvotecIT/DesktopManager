using System;

namespace DesktopManager;

/// <summary>
/// Describes the active Windows power plan and the power mode applied on top of it.
/// </summary>
public sealed class SystemPowerPlan {
    internal SystemPowerPlan(Guid schemeId, string name, Guid? overlaySchemeId) {
        SchemeId = schemeId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        OverlaySchemeId = overlaySchemeId;
        Mode = overlaySchemeId.HasValue ? SystemPowerService.ToPowerMode(overlaySchemeId.Value) : SystemPowerMode.Unknown;
    }

    /// <summary>Gets the identifier of the active power plan.</summary>
    public Guid SchemeId { get; }

    /// <summary>Gets the localized friendly name of the active power plan, or an empty string when it has none.</summary>
    public string Name { get; }

    /// <summary>
    /// Gets the effective power mode overlay, <see cref="Guid.Empty"/> when none is applied, or <c>null</c> when this
    /// Windows version has no power mode overlays (before Windows 10 version 1709).
    /// </summary>
    public Guid? OverlaySchemeId { get; }

    /// <summary>Gets the power mode chosen in Settings, or <see cref="SystemPowerMode.Unknown"/> when it is unavailable or unrecognized.</summary>
    public SystemPowerMode Mode { get; }
}
