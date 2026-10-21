namespace DesktopManager;

/// <summary>A local image assignment to the current user's desktop.</summary>
public sealed class DesktopWallpaperRequest {
    /// <summary>Gets or sets the existing local image path. Remote URLs are not downloaded.</summary>
    public string FilePath { get; set; } = string.Empty;
    /// <summary>Gets or sets the display identifier, or null to update every supported current desktop.</summary>
    public string? DisplayId { get; set; }
    /// <summary>Gets or sets the image fit mode. Unsupported modes return Unsupported before changing settings.</summary>
    public DesktopWallpaperPosition Position { get; set; } = DesktopWallpaperPosition.Fill;
}

/// <summary>Outcome of a desktop operation; success describes API completion, not policy enforcement or visible acceptance.</summary>
public enum DesktopOperationStatus {
    /// <summary>The native API or desktop command completed.</summary>
    Success,
    /// <summary>The requested capability or target is not supported.</summary>
    Unsupported,
    /// <summary>A required user session or platform facility is unavailable.</summary>
    Unavailable,
    /// <summary>The platform operation failed or its input was invalid.</summary>
    Failed
}

/// <summary>Reports a desktop operation without treating unsupported behavior as success.</summary>
public class DesktopOperationResult {
    internal DesktopOperationResult(DesktopOperationStatus status, string message) {
        Status = status;
        Message = message;
    }

    /// <summary>Gets whether the requested native operation completed successfully.</summary>
    public bool Succeeded => Status == DesktopOperationStatus.Success;
    /// <summary>Gets the operation outcome.</summary>
    public DesktopOperationStatus Status { get; }
    /// <summary>Gets a diagnostic explaining the outcome.</summary>
    public string Message { get; }
}

/// <summary>Reports the current image and fit mode when the backend can read them.</summary>
public sealed class DesktopWallpaperState : DesktopOperationResult {
    internal DesktopWallpaperState(DesktopOperationStatus status, string message, string? filePath = null, DesktopWallpaperPosition? position = null)
        : base(status, message) {
        FilePath = filePath;
        Position = position;
    }

    /// <summary>Gets the current image path, or null when no local image is assigned or it could not be read.</summary>
    public string? FilePath { get; }
    /// <summary>Gets the image fit mode when available.</summary>
    public DesktopWallpaperPosition? Position { get; }
}
