using System;

namespace DesktopManager;

internal sealed class NativeTextMutationOutcomeUnknownException : TimeoutException {
    internal NativeTextMutationOutcomeUnknownException(string operation, long timeoutMilliseconds)
        : base($"Native text operation {operation} did not complete within {timeoutMilliseconds}ms; the mutation outcome is unknown.") {
    }
}
