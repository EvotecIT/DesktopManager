using System;

namespace DesktopManager;

/// <summary>
/// Indicates that a native operation timed out and may already have changed its target.
/// Callers should inspect the target before deciding whether it is safe to retry.
/// </summary>
public class NativeOperationOutcomeUnknownException : TimeoutException {
    /// <summary>Creates an exception describing an uncertain native operation.</summary>
    public NativeOperationOutcomeUnknownException(string operation, long timeoutMilliseconds)
        : base($"Native operation {operation} did not complete within {timeoutMilliseconds}ms; its outcome is unknown.") {
    }

    /// <summary>Creates an exception with a detailed description of an uncertain outcome.</summary>
    public NativeOperationOutcomeUnknownException(string message) : base(message) {
    }
}
