using System;

namespace DesktopManager;

internal sealed class NativeTextMutationOutcomeUnknownException : NativeOperationOutcomeUnknownException {
    internal NativeTextMutationOutcomeUnknownException(string message)
        : base(message) {
    }

    internal NativeTextMutationOutcomeUnknownException(string operation, long timeoutMilliseconds)
        : base(operation, timeoutMilliseconds) {
    }
}
