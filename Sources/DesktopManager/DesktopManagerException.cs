using System;

namespace DesktopManager;

/// <summary>
/// Exception thrown when a COM call from <see cref="IDesktopManager"/> fails.
/// </summary>
public class DesktopManagerException : Exception {
    /// <summary>
    /// Initializes a new instance of the <see cref="DesktopManagerException"/> class.
    /// </summary>
    /// <param name="operation">Name of the operation being executed.</param>
    /// <param name="innerException">Original exception thrown by the COM call.</param>
    public DesktopManagerException(string operation, Exception innerException)
        : base($"Operation '{operation}' failed: {innerException.Message}", innerException) {
    }
}

