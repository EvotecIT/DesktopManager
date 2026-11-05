using System.Threading;

namespace DesktopManager.Tests;

/// <summary>
/// Protects what session typing injects and when it stops, using a recording input sink and a scripted desktop
/// instead of the real one. Events are written as in <see cref="KeystrokePlannerTests"/>.
/// </summary>
[TestClass]
public class SessionTypingRunnerTests {
    private static readonly IntPtr Target = new(0x1000);
    private static readonly IntPtr Other = new(0x2000);

    [TestMethod]
    public void Run_InjectsScanCodeEventsWithExtendedAndReleaseFlags() {
        var desktop = new FakeDesktop();
        var sink = new RecordingSink();

        SessionTypingResult result = Run(sink, desktop, "@", Options(layout: "DE"));

        Assert.IsTrue(result.Completed);
        Assert.AreEqual("+1D +E38 +10 -10 -E38 -1D", sink.Describe());
        foreach (MonitorNativeMethods.INPUT input in sink.Inputs) {
            Assert.AreEqual(MonitorNativeMethods.INPUT_KEYBOARD, input.Type);
            Assert.AreEqual((ushort)0, input.Data.Keyboard.Vk, "Scan-code input must not carry a virtual key the local layout could reinterpret.");
            Assert.AreNotEqual(0u, input.Data.Keyboard.Flags & MonitorNativeMethods.KEYEVENTF_SCANCODE);
        }
    }

    [TestMethod]
    public void Run_LocalProfileSendsUnicodePackets() {
        var sink = new RecordingSink();

        SessionTypingResult result = Run(sink, new FakeDesktop(), "é", new SessionTypingOptions { Profile = SessionTypingProfile.Local });

        Assert.AreEqual("Unicode", result.KeyboardLayout);
        Assert.AreEqual("+U00E9 -U00E9", sink.Describe());
        Assert.AreNotEqual(0u, sink.Inputs[0].Data.Keyboard.Flags & MonitorNativeMethods.KEYEVENTF_UNICODE);
    }

    [TestMethod]
    public void Run_StopsAtTheNextCharacterWhenTheForegroundChangesAndReleasesNothingItDidNotPress() {
        var desktop = new FakeDesktop();
        var sink = new RecordingSink { OnSend = (count, _) => { if (count == 4) { desktop.Foreground = Other; } } };

        SessionTypingResult result = Run(sink, desktop, "abc", Options());

        Assert.AreEqual(SessionTypingAbortReason.ForegroundChanged, result.AbortReason);
        Assert.AreEqual(2, result.CharactersSent);
        Assert.AreEqual("+1E -1E +30 -30", sink.Describe());
    }

    [TestMethod]
    [DataRow(SessionTypingAbortReason.UserInput)]
    [DataRow(SessionTypingAbortReason.FocusChanged)]
    [DataRow(SessionTypingAbortReason.Cancelled)]
    public void Run_StopsBetweenCharactersOnUserInputFocusChangeOrCancellation(SessionTypingAbortReason reason) {
        var desktop = new FakeDesktop();
        using var cancellation = new CancellationTokenSource();
        var sink = new RecordingSink {
            OnSend = (count, _) => {
                if (count != 2) {
                    return;
                }
                if (reason == SessionTypingAbortReason.UserInput) {
                    desktop.RaiseUserInput();
                } else if (reason == SessionTypingAbortReason.FocusChanged) {
                    desktop.Focus = new IntPtr(0x3001);
                } else {
                    cancellation.Cancel();
                }
            }
        };

        SessionTypingResult result = Run(sink, desktop, "ab", Options(), cancellation.Token);

        Assert.AreEqual(reason, result.AbortReason);
        Assert.AreEqual(1, result.CharactersSent);
        Assert.AreEqual("+1E -1E", sink.Describe());
    }

    [TestMethod]
    public void Run_RejectedInputStillReleasesTheModifiersItPressed() {
        var desktop = new FakeDesktop();
        // Shift and AltGr are down when Windows rejects the key itself.
        var sink = new RecordingSink { Reject = count => count == 4 };

        SessionTypingResult result = Run(sink, desktop, "Ż", Options(layout: "PL"));

        Assert.AreEqual(SessionTypingAbortReason.InputRejected, result.AbortReason);
        Assert.AreEqual(0, result.CharactersSent);
        Assert.AreEqual("+1D +E38 +2A -2A -E38 -1D", sink.DescribeAccepted());
    }

    [TestMethod]
    public void Run_PacesKeysLinesAndChunksThroughTheEnvironment() {
        var desktop = new FakeDesktop();
        var options = Options();
        options.KeyDelayMilliseconds = 3;
        options.ModifierHoldMilliseconds = 0;
        options.LineDelayMilliseconds = 40;
        options.ChunkSize = 2;
        options.ChunkPauseMilliseconds = 500;

        Run(new RecordingSink(), desktop, "ab\nc", options);

        // a | b ends the first chunk | Enter waits for the line | c is last, so no chunk pause follows it.
        CollectionAssert.AreEqual(new[] { 3, 500, 40, 3 }, desktop.Delays);
    }

    [TestMethod]
    public void Run_WaitsForTheTargetToBecomeForegroundInsteadOfActivatingIt() {
        var desktop = new FakeDesktop { Foreground = Other };
        int polls = 0;
        desktop.OnDelay = () => { if (++polls == 3) { desktop.Foreground = Target; } };
        var sink = new RecordingSink();

        SessionTypingResult result = Run(sink, desktop, "a", Options());

        Assert.IsTrue(result.Completed);
        Assert.AreEqual(0, desktop.Activations);
        Assert.AreEqual("+1E -1E", sink.Describe());
    }

    [TestMethod]
    public void Run_ForegroundTimeoutThrowsBeforeAnyInput() {
        var desktop = new FakeDesktop { Foreground = Other };
        var sink = new RecordingSink();
        var options = Options();
        options.ForegroundTimeout = TimeSpan.FromMilliseconds(200);

        Assert.ThrowsExactly<TimeoutException>(() => Run(sink, desktop, "a", options));
        Assert.AreEqual(0, sink.Inputs.Count);
    }

    [TestMethod]
    public void Run_RefusesAnElevatedTargetBeforeAnyInput() {
        var desktop = new FakeDesktop { ElevatedBeyondCaller = true };
        var sink = new RecordingSink();

        InvalidOperationException error = Assert.ThrowsExactly<InvalidOperationException>(() => Run(sink, desktop, "a", Options()));

        StringAssert.Contains(error.Message, "elevated");
        Assert.AreEqual(0, sink.Inputs.Count);
    }

    [TestMethod]
    public void Run_WaitsForHeldModifiersToBeReleasedAndNeverReleasesThemItself() {
        var desktop = new FakeDesktop();
        desktop.HeldModifiers.Add("Left Ctrl");
        int polls = 0;
        desktop.OnDelay = () => { if (++polls == 2) { desktop.HeldModifiers.Clear(); } };
        var sink = new RecordingSink();

        Run(sink, desktop, "a", Options());

        Assert.AreEqual("+1E -1E", sink.Describe());
    }

    [TestMethod]
    public void Run_ModifiersHeldPastTheTimeoutThrowNamingThem() {
        var desktop = new FakeDesktop();
        desktop.HeldModifiers.Add("Right Alt");
        var options = Options();
        options.ModifierReleaseTimeout = TimeSpan.FromMilliseconds(100);

        TimeoutException error = Assert.ThrowsExactly<TimeoutException>(() => Run(new RecordingSink(), desktop, "a", options));

        StringAssert.Contains(error.Message, "Right Alt");
    }

    [TestMethod]
    public void Run_CapsLockFailsByDefaultTurnsOffAndBackOnOrIsCompensated() {
        var desktop = new FakeDesktop { CapsLockOn = true };

        Assert.ThrowsExactly<InvalidOperationException>(() => Run(new RecordingSink(), desktop, "a", Options()));

        var turnOff = new RecordingSink();
        var turnOffOptions = Options();
        turnOffOptions.CapsLock = CapsLockPolicy.TurnOff;
        Run(turnOff, desktop, "a", turnOffOptions);
        Assert.AreEqual("+3A -3A +1E -1E +3A -3A", turnOff.Describe());

        var compensate = new RecordingSink();
        var compensateOptions = Options();
        compensateOptions.CapsLock = CapsLockPolicy.Compensate;
        Run(compensate, desktop, "a", compensateOptions);
        Assert.AreEqual("+2A +1E -1E -2A", compensate.Describe());
    }

    [TestMethod]
    public void Run_CapsLockIsIgnoredForUnicodePackets() {
        var desktop = new FakeDesktop { CapsLockOn = true };
        var sink = new RecordingSink();

        SessionTypingResult result = Run(sink, desktop, "a", new SessionTypingOptions { Profile = SessionTypingProfile.Local });

        Assert.IsTrue(result.Completed);
    }

    [TestMethod]
    public void Run_UnmappedCharacterThrowsBeforeAnyInputOrInputMonitoring() {
        var desktop = new FakeDesktop();
        var sink = new RecordingSink();

        Assert.ThrowsExactly<InvalidOperationException>(() => Run(sink, desktop, "a€", Options()));
        Assert.AreEqual(0, sink.Inputs.Count);
        Assert.AreEqual(0, desktop.MonitorsStarted);
    }

    [TestMethod]
    public void Run_StartsAndStopsTheUserInputMonitorAroundTyping() {
        var desktop = new FakeDesktop();

        Run(new RecordingSink(), desktop, "a", Options());

        Assert.AreEqual(1, desktop.MonitorsStarted);
        Assert.AreEqual(0, desktop.MonitorsRunning);
    }

    [TestMethod]
    public void Resolve_FillsPacingFromTheProfileWithoutChangingTheOptions() {
        var options = new SessionTypingOptions { Profile = SessionTypingProfile.Vnc, KeyDelayMilliseconds = 1 };

        SessionTypingSettings vnc = SessionTypingSettings.Resolve(options);
        SessionTypingSettings rdp = SessionTypingSettings.Resolve(new SessionTypingOptions { Profile = SessionTypingProfile.Rdp });

        Assert.AreEqual(1, vnc.KeyDelayMilliseconds);
        Assert.IsTrue(vnc.ModifierHoldMilliseconds > rdp.ModifierHoldMilliseconds, "VNC holds modifiers longer than RDP.");
        Assert.IsNull(options.ModifierHoldMilliseconds);
        Assert.AreEqual(KeystrokeMode.ScanCode, vnc.Mode);
        Assert.AreEqual(KeystrokeMode.Unicode, SessionTypingSettings.Resolve(new SessionTypingOptions { Profile = SessionTypingProfile.Local }).Mode);
    }

    [TestMethod]
    public void Resolve_ActivationIsOnlyAllowedForLocalWindows() {
        Assert.ThrowsExactly<ArgumentException>(() => SessionTypingSettings.Resolve(new SessionTypingOptions { Profile = SessionTypingProfile.Rdp, ActivateWindow = true }));
        Assert.IsTrue(SessionTypingSettings.Resolve(new SessionTypingOptions { Profile = SessionTypingProfile.Local, ActivateWindow = true }).ActivateWindow);
    }

    private static SessionTypingOptions Options(string layout = "US") {
        return new SessionTypingOptions {
            Profile = SessionTypingProfile.Rdp,
            KeyboardLayout = layout,
            KeyDelayMilliseconds = 0,
            ModifierHoldMilliseconds = 0,
            LineDelayMilliseconds = 0,
            ChunkSize = 0,
            ChunkPauseMilliseconds = 0
        };
    }

    private static SessionTypingResult Run(RecordingSink sink, FakeDesktop desktop, string text, SessionTypingOptions options, CancellationToken cancellationToken = default) {
        SessionTypingSettings settings = SessionTypingSettings.Resolve(options);
        var runner = new SessionTypingRunner(sink, desktop);
        return runner.Run(Target, text.ToCharArray(), settings, () => KeystrokePlannerTests.Layout(settings.KeyboardLayout ?? "US"), cancellationToken);
    }

    private sealed class RecordingSink : IInputSink {
        private readonly List<bool> _accepted = new();

        internal List<MonitorNativeMethods.INPUT> Inputs { get; } = new();

        internal Action<int, MonitorNativeMethods.INPUT>? OnSend { get; set; }

        internal Func<int, bool>? Reject { get; set; }

        public uint Send(MonitorNativeMethods.INPUT[] inputs) {
            uint accepted = 0;
            foreach (MonitorNativeMethods.INPUT input in inputs) {
                Inputs.Add(input);
                bool ok = Reject == null || !Reject(Inputs.Count);
                _accepted.Add(ok);
                if (ok) {
                    accepted++;
                }

                OnSend?.Invoke(Inputs.Count, input);
            }

            return accepted;
        }

        internal string Describe() {
            return string.Join(" ", Inputs.Select(Format));
        }

        internal string DescribeAccepted() {
            return string.Join(" ", Inputs.Where((_, index) => _accepted[index]).Select(Format));
        }

        private static string Format(MonitorNativeMethods.INPUT input) {
            MonitorNativeMethods.KEYBDINPUT keyboard = input.Data.Keyboard;
            bool unicode = (keyboard.Flags & MonitorNativeMethods.KEYEVENTF_UNICODE) != 0;
            bool extended = (keyboard.Flags & 0x0001) != 0;
            bool up = (keyboard.Flags & MonitorNativeMethods.KEYEVENTF_KEYUP) != 0;
            return (up ? "-" : "+") + (unicode ? "U" : string.Empty) + (extended ? "E" : string.Empty) + keyboard.Scan.ToString(unicode ? "X4" : "X2");
        }
    }

    private sealed class FakeDesktop : ISessionTypingEnvironment {
        private Action? _userInput;

        internal IntPtr Foreground { get; set; } = Target;

        internal IntPtr Focus { get; set; } = new(0x3000);

        internal bool ElevatedBeyondCaller { get; set; }

        internal bool CapsLockOn { get; set; }

        internal List<string> HeldModifiers { get; } = new();

        internal List<int> Delays { get; } = new();

        internal Action? OnDelay { get; set; }

        internal int Activations { get; private set; }

        internal int MonitorsStarted { get; private set; }

        internal int MonitorsRunning { get; private set; }

        internal void RaiseUserInput() {
            _userInput?.Invoke();
        }

        public IntPtr GetRootWindow(IntPtr window) => window;

        public IntPtr GetForegroundWindow() => Foreground;

        public IntPtr GetFocusedWindow() => Focus;

        public void TryActivate(IntPtr window) => Activations++;

        public bool IsElevatedBeyondCaller(IntPtr window) => ElevatedBeyondCaller;

        public IReadOnlyList<string> GetHeldModifiers() => HeldModifiers.ToArray();

        public bool IsCapsLockOn() => CapsLockOn;

        public bool Delay(int milliseconds, CancellationToken cancellationToken) {
            Delays.Add(milliseconds);
            OnDelay?.Invoke();
            return !cancellationToken.IsCancellationRequested;
        }

        public IDisposable StartUserInputMonitor(Action onUserInput) {
            _userInput = onUserInput;
            MonitorsStarted++;
            MonitorsRunning++;
            return new Stop(this);
        }

        private sealed class Stop : IDisposable {
            private readonly FakeDesktop _owner;

            internal Stop(FakeDesktop owner) {
                _owner = owner;
            }

            public void Dispose() {
                _owner.MonitorsRunning--;
                _owner._userInput = null;
            }
        }
    }
}
