using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace DesktopManager;

/// <summary>Receives synthesized input. The production sink calls <c>SendInput</c>; tests record the events.</summary>
internal interface IInputSink {
    /// <summary>Injects the events and returns how many Windows accepted.</summary>
    uint Send(MonitorNativeMethods.INPUT[] inputs);
}

/// <summary>Desktop state the session typing runner reads, waits on and watches.</summary>
internal interface ISessionTypingEnvironment {
    IntPtr GetRootWindow(IntPtr window);

    IntPtr GetForegroundWindow();

    /// <summary>Gets the focused window of the foreground thread, or zero when unknown.</summary>
    IntPtr GetFocusedWindow();

    void TryActivate(IntPtr window);

    /// <summary>Gets whether the window's process is elevated while this process is not, so UIPI would drop the input.</summary>
    bool IsElevatedBeyondCaller(IntPtr window);

    /// <summary>Gets the names of Ctrl, Alt, Shift and Windows keys that are physically held.</summary>
    IReadOnlyList<string> GetHeldModifiers();

    bool IsCapsLockOn();

    /// <summary>Waits, returning <c>false</c> early when <paramref name="cancellationToken"/> is cancelled.</summary>
    bool Delay(int milliseconds, CancellationToken cancellationToken);

    /// <summary>Calls <paramref name="onUserInput"/> when a key, mouse button or wheel is used physically.</summary>
    IDisposable StartUserInputMonitor(Action onUserInput);
}

/// <summary>Production input sink.</summary>
internal sealed class SendInputSink : IInputSink {
    internal static readonly SendInputSink Instance = new();

    private static readonly int InputSize = Marshal.SizeOf<MonitorNativeMethods.INPUT>();

    /// <inheritdoc />
    public uint Send(MonitorNativeMethods.INPUT[] inputs) {
        return MonitorNativeMethods.SendInput((uint)inputs.Length, inputs, InputSize);
    }
}

/// <summary>Session typing settings after profile defaults are applied.</summary>
internal sealed class SessionTypingSettings {
    internal SessionTypingProfile Profile { get; private set; }
    internal string? KeyboardLayout { get; private set; }
    internal KeystrokeMode Mode { get; private set; }
    internal SessionLineBreakMode LineBreak { get; private set; }
    internal bool StripLeadingIndentation { get; private set; }
    internal UnmappedCharacterPolicy UnmappedCharacters { get; private set; }
    internal CapsLockPolicy CapsLock { get; private set; }
    internal TimeSpan ForegroundTimeout { get; private set; }
    internal bool ActivateWindow { get; private set; }
    internal TimeSpan ModifierReleaseTimeout { get; private set; }
    internal bool StopWhenFocusChanges { get; private set; }
    internal bool StopOnUserInput { get; private set; }
    internal int KeyDelayMilliseconds { get; private set; }
    internal int ModifierHoldMilliseconds { get; private set; }
    internal int LineDelayMilliseconds { get; private set; }
    internal int ChunkSize { get; private set; }
    internal int ChunkPauseMilliseconds { get; private set; }

    /// <summary>Validates <paramref name="options"/> and fills unset pacing from the profile, without changing the options.</summary>
    internal static SessionTypingSettings Resolve(SessionTypingOptions? options) {
        SessionTypingOptions source = options ?? new SessionTypingOptions();
        (int key, int hold, int line, int chunk, int pause) = source.Profile switch {
            SessionTypingProfile.Local => (0, 0, 0, 0, 0),
            SessionTypingProfile.Rdp => (5, 10, 50, 64, 50),
            SessionTypingProfile.Citrix => (10, 15, 100, 32, 100),
            SessionTypingProfile.Vnc => (25, 40, 150, 32, 200),
            _ => throw new ArgumentOutOfRangeException(nameof(options), "Unknown session typing profile.")
        };

        if (source.ActivateWindow && source.Profile != SessionTypingProfile.Local) {
            throw new ArgumentException("Activation is only available for the Local profile; click into a remote session yourself so its client has keyboard focus.", nameof(options));
        }
        if (source.ForegroundTimeout < TimeSpan.Zero || source.ModifierReleaseTimeout < TimeSpan.Zero) {
            throw new ArgumentOutOfRangeException(nameof(options), "Timeouts must be zero or greater.");
        }

        return new SessionTypingSettings {
            Profile = source.Profile,
            KeyboardLayout = string.IsNullOrWhiteSpace(source.KeyboardLayout) ? null : source.KeyboardLayout,
            Mode = source.Profile == SessionTypingProfile.Local ? KeystrokeMode.Unicode : KeystrokeMode.ScanCode,
            LineBreak = source.LineBreak,
            StripLeadingIndentation = source.StripLeadingIndentation,
            UnmappedCharacters = source.UnmappedCharacters,
            CapsLock = source.CapsLock,
            ForegroundTimeout = source.ForegroundTimeout,
            ActivateWindow = source.ActivateWindow,
            ModifierReleaseTimeout = source.ModifierReleaseTimeout,
            StopWhenFocusChanges = source.StopWhenFocusChanges,
            StopOnUserInput = source.StopOnUserInput,
            KeyDelayMilliseconds = NonNegative(source.KeyDelayMilliseconds ?? key, nameof(SessionTypingOptions.KeyDelayMilliseconds)),
            ModifierHoldMilliseconds = NonNegative(source.ModifierHoldMilliseconds ?? hold, nameof(SessionTypingOptions.ModifierHoldMilliseconds)),
            LineDelayMilliseconds = NonNegative(source.LineDelayMilliseconds ?? line, nameof(SessionTypingOptions.LineDelayMilliseconds)),
            ChunkSize = NonNegative(source.ChunkSize ?? chunk, nameof(SessionTypingOptions.ChunkSize)),
            ChunkPauseMilliseconds = NonNegative(source.ChunkPauseMilliseconds ?? pause, nameof(SessionTypingOptions.ChunkPauseMilliseconds))
        };
    }

    internal KeystrokePlannerOptions CreatePlannerOptions(bool capsLockOn) {
        return new KeystrokePlannerOptions {
            Mode = Mode,
            LineBreak = LineBreak,
            StripLeadingIndentation = StripLeadingIndentation,
            UnmappedCharacters = UnmappedCharacters,
            CapsLockOn = capsLockOn,
            KeyDelayMilliseconds = KeyDelayMilliseconds,
            ModifierHoldMilliseconds = ModifierHoldMilliseconds,
            LineDelayMilliseconds = LineDelayMilliseconds
        };
    }

    private static int NonNegative(int value, string name) {
        if (value < 0) {
            throw new ArgumentOutOfRangeException(name, $"{name} must be zero or greater.");
        }

        return value;
    }
}

/// <summary>
/// Checks the target and keyboard state, plans the text and replays the plan with pacing, stopping safely when the
/// foreground, focus or user changes anything.
/// </summary>
internal sealed class SessionTypingRunner {
    private const int PollMilliseconds = 50;
    private readonly IInputSink _sink;
    private readonly ISessionTypingEnvironment _environment;
    private readonly MonitorNativeMethods.INPUT[] _input = new MonitorNativeMethods.INPUT[1];

    internal SessionTypingRunner(IInputSink sink, ISessionTypingEnvironment environment) {
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <summary>Types <paramref name="text"/> into <paramref name="target"/>.</summary>
    /// <remarks>Failures before the first key event throw; once typing starts, the result reports how far it got.</remarks>
    internal SessionTypingResult Run(IntPtr target, char[] text, SessionTypingSettings settings, Func<IKeyboardLayoutMap> layoutFactory, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        IntPtr root = _environment.GetRootWindow(target);
        if (root == IntPtr.Zero) {
            root = target;
        }

        if (settings.ActivateWindow) {
            _environment.TryActivate(root);
        }

        WaitForForeground(root, settings.ForegroundTimeout, cancellationToken);
        if (_environment.IsElevatedBeyondCaller(root)) {
            throw new InvalidOperationException("The target window belongs to an elevated process and Windows would discard input from this non-elevated process. Run the caller elevated.");
        }

        WaitForModifierRelease(settings.ModifierReleaseTimeout, cancellationToken);
        bool capsLockOn = settings.Mode == KeystrokeMode.ScanCode && _environment.IsCapsLockOn();
        if (capsLockOn && settings.CapsLock == CapsLockPolicy.Fail) {
            throw new InvalidOperationException("Caps Lock is on. Turn it off, or choose to turn it off while typing or to compensate for it.");
        }

        string layoutName;
        KeystrokePlan plan;
        KeystrokePlannerOptions plannerOptions = settings.CreatePlannerOptions(capsLockOn && settings.CapsLock == CapsLockPolicy.Compensate);
        if (settings.Mode == KeystrokeMode.Unicode) {
            layoutName = "Unicode";
            plan = KeystrokePlanner.Plan(text, null, plannerOptions);
        } else {
            IKeyboardLayoutMap layout = layoutFactory();
            try {
                layoutName = layout.Name;
                plan = KeystrokePlanner.Plan(text, layout, plannerOptions);
            } finally {
                (layout as IDisposable)?.Dispose();
            }
        }

        using (plan) {
            if (_environment.GetForegroundWindow() != root) {
                throw new InvalidOperationException("The target window lost the foreground before typing started.");
            }

            bool turnCapsLockOff = capsLockOn && settings.CapsLock == CapsLockPolicy.TurnOff;
            return Inject(plan, root, settings, layoutName, turnCapsLockOff, text.Length, cancellationToken);
        }
    }

    private SessionTypingResult Inject(KeystrokePlan plan, IntPtr root, SessionTypingSettings settings, string layoutName, bool turnCapsLockOff, int textLength, CancellationToken cancellationToken) {
        int userInput = 0;
        IntPtr focus = settings.StopWhenFocusChanges ? _environment.GetFocusedWindow() : IntPtr.Zero;
        var pressed = new List<Keystroke>(4);
        int sent = 0;
        bool capsLockTurnedOff = false;
        SessionTypingAbortReason reason = SessionTypingAbortReason.None;
        IDisposable? monitor = settings.StopOnUserInput ? _environment.StartUserInputMonitor(() => Interlocked.Exchange(ref userInput, 1)) : null;
        try {
            if (turnCapsLockOff) {
                if (!Tap(KeystrokePlanner.CapsLockScanCode)) {
                    return new SessionTypingResult(settings.Profile, layoutName, textLength, 0, SessionTypingAbortReason.InputRejected);
                }

                capsLockTurnedOff = true;
            }

            bool characterStart = true;
            int charactersInChunk = 0;
            for (int index = 0; index < plan.Count; index++) {
                if (characterStart) {
                    reason = CheckStop(root, focus, Volatile.Read(ref userInput) != 0, cancellationToken);
                    if (reason != SessionTypingAbortReason.None) {
                        break;
                    }
                }

                Keystroke keystroke = plan[index];
                if (!Send(keystroke)) {
                    reason = SessionTypingAbortReason.InputRejected;
                    break;
                }

                Track(pressed, keystroke);
                int delay = keystroke.DelayAfterMilliseconds;
                characterStart = keystroke.CompletesCharacter;
                if (characterStart) {
                    sent = keystroke.SourceEnd;
                    charactersInChunk++;
                    if (settings.ChunkSize > 0 && charactersInChunk >= settings.ChunkSize && index < plan.Count - 1) {
                        delay = Math.Max(delay, settings.ChunkPauseMilliseconds);
                        charactersInChunk = 0;
                    }
                }

                if (delay > 0) {
                    // A cancelled wait ends early; the next character start reports it after this one is complete.
                    _environment.Delay(delay, cancellationToken);
                }
            }
        } finally {
            ReleasePressed(pressed);
            if (capsLockTurnedOff) {
                Tap(KeystrokePlanner.CapsLockScanCode);
            }

            monitor?.Dispose();
            ClearInput();
        }

        if (reason == SessionTypingAbortReason.None) {
            sent = textLength;
        }

        return new SessionTypingResult(settings.Profile, layoutName, textLength, sent, reason);
    }

    private SessionTypingAbortReason CheckStop(IntPtr root, IntPtr focus, bool userInput, CancellationToken cancellationToken) {
        if (cancellationToken.IsCancellationRequested) {
            return SessionTypingAbortReason.Cancelled;
        }
        if (userInput) {
            return SessionTypingAbortReason.UserInput;
        }
        if (_environment.GetForegroundWindow() != root) {
            return SessionTypingAbortReason.ForegroundChanged;
        }
        if (focus != IntPtr.Zero && _environment.GetFocusedWindow() != focus) {
            return SessionTypingAbortReason.FocusChanged;
        }

        return SessionTypingAbortReason.None;
    }

    private void WaitForForeground(IntPtr root, TimeSpan timeout, CancellationToken cancellationToken) {
        // Time is counted in polls so waiting is measured the same way the environment sleeps.
        double waited = 0;
        while (_environment.GetForegroundWindow() != root) {
            if (timeout == TimeSpan.Zero) {
                throw new InvalidOperationException("The target window is not the foreground window. Click into it, or allow time for that with a foreground timeout.");
            }
            if (waited >= timeout.TotalMilliseconds) {
                throw new TimeoutException($"The target window did not become the foreground window within {timeout.TotalSeconds:0.#} seconds.");
            }

            Poll(cancellationToken);
            waited += PollMilliseconds;
        }
    }

    private void WaitForModifierRelease(TimeSpan timeout, CancellationToken cancellationToken) {
        double waited = 0;
        while (true) {
            IReadOnlyList<string> held = _environment.GetHeldModifiers();
            if (held.Count == 0) {
                return;
            }
            if (waited >= timeout.TotalMilliseconds) {
                throw new TimeoutException($"Release {string.Join(", ", held)} before typing starts.");
            }

            Poll(cancellationToken);
            waited += PollMilliseconds;
        }
    }

    private void Poll(CancellationToken cancellationToken) {
        if (!_environment.Delay(PollMilliseconds, cancellationToken)) {
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private static void Track(List<Keystroke> pressed, Keystroke keystroke) {
        if (!keystroke.IsUp) {
            pressed.Add(keystroke);
            return;
        }

        for (int index = pressed.Count - 1; index >= 0; index--) {
            if (IsSameKey(pressed[index], keystroke)) {
                pressed.RemoveAt(index);
                return;
            }
        }
    }

    private void ReleasePressed(List<Keystroke> pressed) {
        // Only keys this run pressed are released; keys the user holds are never touched.
        for (int index = pressed.Count - 1; index >= 0; index--) {
            Keystroke down = pressed[index];
            Send(new Keystroke(down.Code, down.Flags | KeystrokeFlags.Up, 0, -1));
        }

        pressed.Clear();
    }

    private bool Tap(ushort scanCode) {
        if (!Send(new Keystroke(scanCode, KeystrokeFlags.None, 0, -1))) {
            return false;
        }

        var up = new Keystroke(scanCode, KeystrokeFlags.Up, 0, -1);
        // A key left down would keep acting on the session, so the release is tried twice.
        return Send(up) || Send(up);
    }

    private bool Send(Keystroke keystroke) {
        uint flags = keystroke.IsUnicode ? MonitorNativeMethods.KEYEVENTF_UNICODE : MonitorNativeMethods.KEYEVENTF_SCANCODE;
        if (keystroke.IsExtended) {
            flags |= SessionTypingNative.KEYEVENTF_EXTENDEDKEY;
        }
        if (keystroke.IsUp) {
            flags |= MonitorNativeMethods.KEYEVENTF_KEYUP;
        }

        _input[0].Type = MonitorNativeMethods.INPUT_KEYBOARD;
        _input[0].Data.Keyboard = new MonitorNativeMethods.KEYBDINPUT {
            Vk = 0,
            Scan = keystroke.Code,
            Flags = flags,
            Time = 0,
            ExtraInfo = IntPtr.Zero
        };

        return _sink.Send(_input) == 1;
    }

    private void ClearInput() {
        _input[0] = default;
    }

    private static bool IsSameKey(Keystroke pressed, Keystroke released) {
        const KeystrokeFlags identity = KeystrokeFlags.Extended | KeystrokeFlags.Unicode;
        return pressed.Code == released.Code && (pressed.Flags & identity) == (released.Flags & identity);
    }
}
