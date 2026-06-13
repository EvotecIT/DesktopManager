namespace DesktopManager.App;

internal sealed class MonitorAdvancedColorOption {
    public MonitorAdvancedColorOption(MonitorAdvancedColorInfo info) {
        Index = info.Index;
        DeviceId = info.DeviceId;
        DisplayName = $"{info.Index}: {info.DeviceName}";
        HdrState = info.HdrSupported
            ? info.HdrEnabled ? "HDR on" : "HDR off"
            : "HDR unavailable";
        Mode = string.IsNullOrWhiteSpace(info.ActiveColorMode) ? "Mode unknown" : info.ActiveColorMode;
        Details = $"{info.ColorEncoding}, {info.BitsPerColorChannel} bpc";
        SdrWhite = info.SdrWhiteLevelNits.HasValue ? $"{info.SdrWhiteLevelNits.Value:0.##} nits SDR white" : string.Empty;
        CanToggleHdr = info.HdrSupported;
    }

    public int Index { get; }
    public string DeviceId { get; }
    public string DisplayName { get; }
    public string HdrState { get; }
    public string Mode { get; }
    public string Details { get; }
    public string SdrWhite { get; }
    public bool CanToggleHdr { get; }
}
