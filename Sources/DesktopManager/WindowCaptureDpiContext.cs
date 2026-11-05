namespace DesktopManager;

/// <summary>Keeps window capture geometry and pixels in the same physical coordinate space.</summary>
internal static class WindowCaptureDpiContext {
    internal static T Execute<T>(Func<T> capture) where T : class, IDisposable {
        // A failure after capture must not leak the image that can no longer be returned.
        return WindowDpiContext.Execute(capture, result => result.Dispose());
    }
}
