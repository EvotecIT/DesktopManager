using System;
using System.Runtime.Versioning;
using System.Threading;

namespace DesktopManager;

/// <summary>
/// Types text into remote and locked-down sessions where pasting is blocked, such as RDP, Citrix and VNC clients
/// and password fields.
/// </summary>
/// <remarks>
/// <para>For remote profiles each character becomes the physical key presses that produce it on the session's keyboard
/// layout, sent as scan codes, so the remote side sees ordinary typing. The local profile sends Unicode packets.</para>
/// <para>The service never takes the focus from you: it waits until you make the target window the foreground window,
/// refuses elevated targets it cannot reach, waits for held modifier keys to be released and checks Caps Lock. While
/// typing it stops when the foreground window or focused control changes, when you press a key, click or scroll, or when
/// cancelled, and it always releases every key it pressed. The text is never logged or returned.</para>
/// </remarks>
[SupportedOSPlatform("windows")]
public static class SessionTypingService {
    /// <summary>Types <paramref name="text"/> into <paramref name="target"/>.</summary>
    /// <param name="target">The window that must be in the foreground, such as the remote desktop client's window.</param>
    /// <param name="text">The text to type.</param>
    /// <param name="options">Profile, layout, pacing and safety options; <c>null</c> uses the RDP profile.</param>
    /// <param name="cancellationToken">Stops waiting or typing.</param>
    /// <returns>How much of the text was typed and why typing stopped early, if it did.</returns>
    /// <exception cref="TimeoutException">The target did not become the foreground window, or modifier keys stayed held.</exception>
    /// <exception cref="InvalidOperationException">The target is elevated, Caps Lock is on, or a character cannot be typed, under the chosen policies.</exception>
    /// <exception cref="OperationCanceledException">Cancelled before typing started.</exception>
    public static SessionTypingResult Type(WindowInfo target, string text, SessionTypingOptions? options = null, CancellationToken cancellationToken = default) {
        if (text == null) {
            throw new ArgumentNullException(nameof(text));
        }

        char[] buffer = text.ToCharArray();
        try {
            return Type(target, buffer, options, cancellationToken);
        } finally {
            Array.Clear(buffer, 0, buffer.Length);
        }
    }

    /// <summary>Types the characters in <paramref name="text"/> into <paramref name="target"/>.</summary>
    /// <param name="target">The window that must be in the foreground, such as the remote desktop client's window.</param>
    /// <param name="text">The characters to type. The array is not changed or kept, so the caller can clear it afterwards.</param>
    /// <param name="options">Profile, layout, pacing and safety options; <c>null</c> uses the RDP profile.</param>
    /// <param name="cancellationToken">Stops waiting or typing.</param>
    /// <returns>How much of the text was typed and why typing stopped early, if it did.</returns>
    /// <exception cref="TimeoutException">The target did not become the foreground window, or modifier keys stayed held.</exception>
    /// <exception cref="InvalidOperationException">The target is elevated, Caps Lock is on, or a character cannot be typed, under the chosen policies.</exception>
    /// <exception cref="OperationCanceledException">Cancelled before typing started.</exception>
    public static SessionTypingResult Type(WindowInfo target, char[] text, SessionTypingOptions? options = null, CancellationToken cancellationToken = default) {
        if (target == null) {
            throw new ArgumentNullException(nameof(target));
        }
        if (target.Handle == IntPtr.Zero) {
            throw new ArgumentException("Invalid window handle.", nameof(target));
        }
        if (text == null) {
            throw new ArgumentNullException(nameof(text));
        }

        SessionTypingSettings settings = SessionTypingSettings.Resolve(options);
        var runner = new SessionTypingRunner(SendInputSink.Instance, NativeSessionTypingEnvironment.Instance);
        return runner.Run(target.Handle, text, settings, () => OpenLayout(target, settings), cancellationToken);
    }

    private static IKeyboardLayoutMap OpenLayout(WindowInfo target, SessionTypingSettings settings) {
        if (settings.KeyboardLayout != null) {
            return NativeKeyboardLayoutMap.Load(settings.KeyboardLayout);
        }

        uint threadId = target.ThreadId != 0 ? target.ThreadId : MonitorNativeMethods.GetWindowThreadProcessId(target.Handle, out _);
        return NativeKeyboardLayoutMap.ForThread(threadId);
    }
}
