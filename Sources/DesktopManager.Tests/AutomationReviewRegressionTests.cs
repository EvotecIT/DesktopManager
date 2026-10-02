using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
[DoNotParallelize]
public class AutomationReviewRegressionTests {
    [TestMethod]
    public void FullProviderQueue_RetainsSubscriptionCleanupUntilRecovery() {
        using var dispatcher = new UiAutomationStaDispatcher();
        using var started = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);
        using var cleaned = new ManualResetEventSlim(false);
        Task blocked = Task.Run(() => Assert.ThrowsExactly<UiAutomationOperationInFlightException>(() =>
            dispatcher.Invoke(_ => { started.Set(); release.Wait(); return 1; }, 100)));
        try {
            Assert.IsTrue(started.Wait(2000));
            Assert.IsTrue(blocked.Wait(2000));
            for (int index = 0; index < UiAutomationStaDispatcher.SubscriptionCapacity; index++) {
                Assert.IsTrue(dispatcher.TryRetainSubscription());
            }
            Assert.IsFalse(dispatcher.TryRetainSubscription());
            for (int index = 0; index < UiAutomationStaDispatcher.QueueCapacity; index++) { dispatcher.Post(_ => { }); }
            Assert.ThrowsExactly<TimeoutException>(() => dispatcher.Post(_ => { }));
            for (int index = 0; index < UiAutomationStaDispatcher.SubscriptionCapacity; index++) {
                dispatcher.PostSubscriptionCleanup(_ => {
                    dispatcher.ReleaseSubscription();
                    if (dispatcher.SubscriptionCount == 0) { cleaned.Set(); }
                });
            }
            release.Set();
            Assert.IsTrue(cleaned.Wait(2000), "Saturation must not lose native subscription cleanup.");
            Assert.AreEqual(0, dispatcher.SubscriptionCount);
        } finally { release.Set(); Assert.IsTrue(blocked.Wait(2000)); }
    }

    [TestMethod]
    [TestCategory("UITest")]
    public void NativeTextInput_PreservesFocusedEditorAndClipboardWhenRejected() {
        TestHelper.RequireOwnedWindowMutationTests();
        Exception? failure = null;
        var thread = new Thread(() => {
            try {
                using var original = ClipboardHelper.CaptureSnapshot();
                using var form = new FocusErasingForm { Text = "Owned input fixture", ShowInTaskbar = false };
                using var first = new TextBox { Text = "first", Top = 10 };
                using var second = new TextBox { Text = "second", Top = 40 };
                using var button = new Button { Text = "Focus fixture", Top = 75 };
                form.Controls.Add(first);
                form.Controls.Add(second);
                form.Controls.Add(button);
                _ = first.Handle;
                _ = second.Handle;
                try {
                    var window = new WindowInfo { Handle = form.Handle };
                    var settings = new WindowInputOptions { ActivateWindow = false, PreserveClipboard = true };
                    form.Show();
                    Assert.AreEqual(0, new WindowManager().GetWindows(new WindowQueryOptions {
                        Handle = second.Handle, IncludeHidden = true
                    }).Count, "An explicit window query must still exclude child controls.");
                    button.Focus();
                    Application.DoEvents();
                    var clipboard = new DataObject();
                    clipboard.SetData(DataFormats.UnicodeText, "preserved");
                    clipboard.SetData(DataFormats.Html, "<b>preserved</b>");
                    Clipboard.SetDataObject(clipboard, true);
                    Assert.ThrowsExactly<InvalidOperationException>(() => WindowInputService.PasteText(window, "replacement", settings));
                    Assert.IsTrue(Clipboard.ContainsData(DataFormats.Html), "Rejected paste must not rewrite the original clipboard formats.");
                    Assert.AreEqual("preserved", Clipboard.GetText(TextDataFormat.UnicodeText));
                    settings.PreserveClipboard = false;
                    second.Focus();
                    second.Select(0, second.TextLength);
                    Application.DoEvents();
                    Assert.AreEqual(second.Handle, WindowActivationService.GetFocusedControlHandle(form.Handle));
                    WindowInputService.PasteText(window, "replacement", settings);
                    Assert.AreEqual("first", first.Text);
                    Assert.AreEqual("replacement", second.Text);
                    foreach (bool asScript in new[] { false, true }) {
                        MonitorNativeMethods.SetFocus(second.Handle);
                        second.Select(0, second.TextLength);
                        Assert.AreEqual(second.Handle, WindowActivationService.GetFocusedControlHandle(form.Handle));
                        WindowInputService.TypeText(window, "typed", new WindowInputOptions {
                            ActivateWindow = true, UseSendInput = false, TypeTextAsScript = asScript
                        });
                        Assert.AreEqual("first", first.Text);
                        Assert.AreEqual("typed", second.Text);
                    }
                } finally { original.Restore(); }
            } catch (Exception ex) { failure = ex; }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.IsTrue(thread.Join(10000));
        if (failure != null) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw(); }
    }

    [TestMethod]
    [TestCategory("UITest")]
    public void ApplyLayout_RestoresExactNegativeOneCoordinates() {
        TestHelper.RequireOwnedWindowMutationTests();
        using var harness = WinFormsWindowHarness.Create("Negative layout " + Guid.NewGuid().ToString("N"));
        var manager = new WindowManager();
        WindowPosition saved = manager.GetWindowPosition(harness.Window);
        saved.Right = -1 + saved.Width;
        saved.Bottom = -1 + saved.Height;
        saved.Left = -1;
        saved.Top = -1;
        WindowLayoutApplyResult result = manager.ApplyLayout(new WindowLayout { Windows = new() { saved } }).Single();
        Assert.IsTrue(result.Applied, result.Error);
        WindowPosition actual = manager.GetWindowPosition(harness.Window);
        Assert.AreEqual(-1, actual.Left);
        Assert.AreEqual(-1, actual.Top);
    }

    [TestMethod]
    [TestCategory("UITest")]
    public void WindowWaitAsync_ReturnsTaskBeforeSlowTitleReadAndHonorsDeadline() {
        TestHelper.RequireOwnedWindowUiTests();
        using var ready = new ManualResetEventSlim(false);
        using var nativeReadStarted = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);
        using var returned = new ManualResetEventSlim(false);
        IntPtr handle = IntPtr.Zero;
        var owner = new Thread(() => {
            using var form = new SlowTitleForm(nativeReadStarted, release);
            handle = form.Handle;
            form.Armed = true;
            ready.Set();
            Application.Run(form);
        }) { IsBackground = true };
        owner.SetApartmentState(ApartmentState.STA);
        owner.Start();
        Task? caller = null;
        try {
            Assert.IsTrue(ready.Wait(2000));
            caller = Task.Run(async () => {
                Task<IReadOnlyList<WindowInfo>> wait = new WindowManager().WaitWindowsAsync(
                    new WindowQueryOptions { Handle = handle, IncludeHidden = true, IncludeEmptyTitles = true }, 250);
                returned.Set();
                await Assert.ThrowsAsync<TimeoutException>(() => wait);
            });
            Assert.IsTrue(nativeReadStarted.Wait(2000));
            Assert.IsTrue(returned.Wait(200), "The initial scan blocked the caller before returning its task.");
            Assert.IsTrue(caller.Wait(2000), "The title read must respect the observation deadline.");
        } finally {
            release.Set();
            MonitorNativeMethods.PostMessage(handle, 0x0010, IntPtr.Zero, IntPtr.Zero);
            Assert.IsTrue(owner.Join(2000));
            if (caller != null) { Assert.IsTrue(caller.Wait(2000)); }
        }
    }

    [TestMethod]
    [TestCategory("UITest")]
    public async Task WindowWaitAsync_RejectsMatchCompletedAfterMetadataDeadline() {
        TestHelper.RequireOwnedWindowUiTests();
        using var ready = new ManualResetEventSlim(false);
        using var metadataStarted = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);
        IntPtr handle = IntPtr.Zero;
        var owner = new Thread(() => {
            using var form = new Form { Text = "Owned metadata deadline fixture", ShowInTaskbar = false };
            handle = form.Handle;
            form.Shown += (_, _) => ready.Set();
            Application.Run(form);
        }) { IsBackground = true };
        owner.SetApartmentState(ApartmentState.STA);
        owner.Start();
        Task<IReadOnlyList<WindowInfo>>? wait = null;
        try {
            Assert.IsTrue(ready.Wait(2000));
            var desktop = new FakeDesktopManager {
                DevicePathCount = 0,
                BeforeMonitorEnumeration = () => { metadataStarted.Set(); release.Wait(); }
            };
            var manager = new WindowManager(new Monitors(() => desktop));
            wait = manager.WaitWindowsAsync(new WindowQueryOptions { Handle = handle, IncludeHidden = true }, 250);
            Assert.IsTrue(metadataStarted.Wait(2000));
            await Task.Delay(400);
            release.Set();
            await Assert.ThrowsAsync<TimeoutException>(() => wait);
        } finally {
            release.Set();
            MonitorNativeMethods.PostMessage(handle, 0x0010, IntPtr.Zero, IntPtr.Zero);
            Assert.IsTrue(owner.Join(2000));
            if (wait != null) { try { await wait; } catch (TimeoutException) { } }
        }
    }

    private sealed class FocusErasingForm : Form {
        protected override void WndProc(ref Message message) {
            // Model a native host that keeps focus on the parent after activation.
            if (message.Msg == 0x0007) { message.Result = IntPtr.Zero; return; }
            base.WndProc(ref message);
        }
    }

    private sealed class SlowTitleForm : Form {
        private readonly ManualResetEventSlim _started;
        private readonly ManualResetEventSlim _release;
        internal volatile bool Armed;
        internal SlowTitleForm(ManualResetEventSlim started, ManualResetEventSlim release) {
            _started = started;
            _release = release;
            ShowInTaskbar = false;
        }
        protected override void WndProc(ref Message message) {
            if (Armed && message.Msg == 0x000E) {
                _started.Set();
                _release.Wait();
                message.Result = IntPtr.Zero;
                return;
            }
            base.WndProc(ref message);
        }
    }
}
