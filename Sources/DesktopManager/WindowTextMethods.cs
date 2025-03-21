namespace DesktopManager;

/// <summary>
/// Defines methods for sending text to windows
/// </summary>
public enum TextSendMethod {
    /// <summary>
    /// Simulates typing of each character individually
    /// </summary>
    Type,
    
    /// <summary>
    /// Uses clipboard paste operation (Ctrl+V)
    /// </summary>
    Paste
}