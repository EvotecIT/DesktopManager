using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Threading.Timer;

namespace DesktopManager.Tests;

[TestClass]
[DoNotParallelize]
public class AutomationReliabilityTests {
    [TestMethod]
    [TestCategory("UITest")]
    public void BackgroundTyping_PreservesPrefixAndSuffixAroundSelection() {
        TestHelper.RequireOwnedWindowUiTests();
        using Form form = new() { ShowInTaskbar = false };
        using TextBox edit = new() { Text = "prefix old suffix" };
        form.Controls.Add(edit);
        _ = form.Handle;
        _ = edit.Handle;
        edit.Select(7, 3);
        WindowInputService.TypeText(new WindowInfo { Handle = form.Handle }, "new",
            new WindowInputOptions { ActivateWindow = false, UseSendInput = false });
        Assert.AreEqual("prefix new suffix", edit.Text);
        edit.Select(edit.TextLength, 0);
        WindowInputService.TypeText(new WindowInfo { Handle = form.Handle }, " appended",
            new WindowInputOptions { ActivateWindow = false, UseSendInput = false });
        Assert.AreEqual("prefix new suffix appended", edit.Text);
    }

    [TestMethod]
    [TestCategory("UITest")]
    public void ControlClick_ExecutesLeftOnceAndDoesNotConvertRightToLeft() {
        TestHelper.RequireOwnedWindowUiTests();
        using var parent = new PasteCounter();
        IntPtr button = MonitorNativeMethods.CreateWindowExW(0, "BUTTON", "button", 0x50000000,
            0, 0, 100, 30, parent.Handle, new IntPtr(1), IntPtr.Zero, IntPtr.Zero);
        IntPtr check = MonitorNativeMethods.CreateWindowExW(0, "BUTTON", "check", 0x50000003,
            0, 40, 100, 30, parent.Handle, new IntPtr(2), IntPtr.Zero, IntPtr.Zero);
        Assert.AreNotEqual(IntPtr.Zero, button);
        Assert.AreNotEqual(IntPtr.Zero, check);
        WindowControlService.ControlClick(new WindowControlInfo { Handle = button }, MouseButton.Left);
        Assert.AreEqual(1, parent.Clicks);
        WindowControlService.ControlClick(new WindowControlInfo { Handle = button }, MouseButton.Right);
        Assert.AreEqual(1, parent.Clicks);
        WindowControlService.ControlClick(new WindowControlInfo { Handle = check }, MouseButton.Left);
        Assert.IsTrue(WindowControlService.GetCheckState(new WindowControlInfo { Handle = check }));
    }

    [TestMethod]
    [TestCategory("UITest")]
    public void PasteDelivery_SendsOnlyOneMessageDespiteRetryCount() {
        TestHelper.RequireOwnedWindowUiTests();
        using var window = new PasteCounter();
        MethodInfo sendPaste = typeof(WindowInputService).GetMethod("SendPaste", BindingFlags.Static | BindingFlags.NonPublic)!;
        sendPaste.Invoke(null, new object[] { window.Handle, 5, 0 });
        Assert.AreEqual(1, window.Count);
    }

    [TestMethod]
    [TestCategory("UITest")]
    public void EmptyTitle_WithProcessFilter_StillObeysExplicitTitleFilters() {
        TestHelper.RequireOwnedWindowUiTests();
        using Form form = new() { Text = string.Empty, ShowInTaskbar = false };
        var query = new WindowQueryOptions {
            Handle = form.Handle, ProcessId = Process.GetCurrentProcess().Id,
            IncludeHidden = true, IncludeEmptyTitles = true, TitlePattern = "required-title"
        };
        var manager = new WindowManager();
        Assert.AreEqual(0, manager.GetWindows(query).Count);
        query.TitleRegex = new Regex("^required-title$");
        Assert.AreEqual(0, manager.GetWindows(query).Count);
        query.TitleRegex = new Regex("^$");
        Assert.AreEqual(1, manager.GetWindows(query).Count);
    }

    [TestMethod]
    public void MonitorWatcher_DisposalFromNativePowerCallback_Returns() {
        using var watcher = new MonitorWatcher();
        object worker = typeof(MonitorWatcher).GetField("_powerWindow", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(watcher)!;
        IntPtr handle = (IntPtr)worker.GetType().GetField("_hwnd", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(worker)!;
        using var returned = new ManualResetEventSlim(false);
        watcher.MonitorPoweredOn += (_, _) => { watcher.Dispose(); returned.Set(); };
        IntPtr payload = Marshal.AllocHGlobal(24);
        try {
            Marshal.Copy(new Guid("02731015-4510-4526-99E6-E5A17EBD1AEA").ToByteArray(), 0, payload, 16);
            Marshal.WriteInt32(payload, 16, 4);
            Marshal.WriteInt32(payload, 20, 1);
            MonitorNativeMethods.SendMessageTimeout(handle, 0x0218, new IntPtr(0x8013), payload,
                MonitorNativeMethods.SMTO_ABORTIFHUNG, 2000, out _);
            Assert.IsTrue(returned.Wait(2000), "Callback disposal must return without joining itself.");
        } finally {
            Marshal.FreeHGlobal(payload);
        }
    }

    [TestMethod]
    [TestCategory("UITest")]
    public void ChildWindowEnumeration_UsesOneMonitorSnapshotForAllChildren() {
        TestHelper.RequireOwnedWindowUiTests();
        using Form form = new() { ShowInTaskbar = false };
        _ = form.Handle;
        for (int index = 0; index < 12; index++) {
            var child = new Label { Text = "child " + index };
            form.Controls.Add(child);
            _ = child.Handle;
        }
        var desktop = new FakeDesktopManager { DevicePathCount = 0 };
        var manager = new WindowManager(new Monitors(() => desktop));
        List<WindowInfo> children = manager.GetChildWindows(new WindowInfo { Handle = form.Handle }, includeHidden: true);
        Assert.AreEqual(12, children.Count);
        Assert.AreEqual(1, desktop.MonitorEnumerationCalls);
    }

    [TestMethod]
    [TestCategory("UITest")]
    public void ChildWindowEnumeration_EmptyOwnedWindow_ReturnsAnEmptyCollection() {
        TestHelper.RequireOwnedWindowUiTests();
        using Form form = new() { ShowInTaskbar = false };
        Assert.AreEqual(0, new WindowManager().GetChildWindows(new WindowInfo { Handle = form.Handle }, includeHidden: true).Count);
    }

    [TestMethod]
    [TestCategory("UITest")]
    public void PasteFailure_RestoresClipboardWithoutHidingTheDeliveryError() {
        TestHelper.RequireOwnedWindowUiTests();
        Exception? failure = null;
        var thread = new Thread(() => {
            try {
                using ClipboardHelper.ClipboardSnapshot original = ClipboardHelper.CaptureSnapshot();
                try {
                    ClipboardHelper.SetText("clipboard backup");
                    Assert.ThrowsExactly<NativeOperationOutcomeUnknownException>(() => WindowInputService.PasteText(
                        new WindowInfo { Handle = new IntPtr(987654) }, "replacement",
                        new WindowInputOptions { PreserveClipboard = true, ActivateWindow = false }));
                    Assert.IsTrue(ClipboardHelper.TryGetText(out string text));
                    Assert.AreEqual("clipboard backup", text);
                } finally { original.Restore(); }
            } catch (Exception ex) { failure = ex; }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.IsTrue(thread.Join(10000), "Clipboard validation did not finish.");
        if (failure != null) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw(); }
    }

    [TestMethod]
    public void HotkeyService_DisposalFromMessageCallback_ReturnsAndRejectsLaterWork() {
        using var service = new HotkeyService();
        using var returned = new ManualResetEventSlim(false);
        var callbacks = (Dictionary<int, Action>)typeof(HotkeyService).GetField("_callbacks", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(service)!;
        callbacks[123] = () => { service.Dispose(); returned.Set(); };
        MonitorNativeMethods.PostMessage(service.WindowHandle, MonitorNativeMethods.WM_HOTKEY, new IntPtr(123), IntPtr.Zero);
        Assert.IsTrue(returned.Wait(2000), "Callback disposal must return without joining itself.");
        Assert.ThrowsExactly<ObjectDisposedException>(() => service.UnregisterHotkey(123));
    }

    [TestMethod]
    public void AudioWatcher_CallbackSubscriberRunsOffNativeThreadAndCanDispose() {
        using var watcher = new AudioEndpointWatcher();
        using var delivered = new ManualResetEventSlim(false);
        int callerThread = Environment.CurrentManagedThreadId;
        int subscriberThread = callerThread;
        watcher.Changed += (_, _) => {
            subscriberThread = Environment.CurrentManagedThreadId;
            watcher.Dispose();
            delivered.Set();
        };
        Assert.AreEqual(0, ((IMMNotificationClient)watcher).OnDeviceAdded("owned-test-endpoint"));
        Assert.IsTrue(delivered.Wait(2000), "Subscriber disposal did not finish.");
        Assert.AreNotEqual(callerThread, subscriberThread);
    }

    [TestMethod]
    public void UiAutomation_BlockedWorker_DisposalRejectsNewWorkAndCompletesAfterRelease() {
        using var dispatcher = new UiAutomationStaDispatcher();
        using var started = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);
        Task first = Task.Run(() => Assert.ThrowsExactly<UiAutomationOperationInFlightException>(() =>
            dispatcher.Invoke(_ => { started.Set(); release.Wait(); return 1; }, 100)));
        try {
            Assert.IsTrue(started.Wait(2000));
            Assert.IsTrue(first.Wait(2000));
            dispatcher.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(() => dispatcher.Invoke(_ => 2, 100));
        } finally {
            release.Set();
            Assert.IsTrue(first.Wait(2000));
        }
    }

    [TestMethod]
    public void UiAutomation_BlockedProcess_DoesNotBlockAnotherProcess() {
        using var pool = new UiAutomationDispatcherPool();
        using var started = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);
        Task first = Task.Run(() => Assert.ThrowsExactly<UiAutomationOperationInFlightException>(() =>
            pool.Invoke(100, _ => { started.Set(); release.Wait(); return 1; }, 100)));
        try {
            Assert.IsTrue(started.Wait(2000));
            Assert.IsTrue(first.Wait(2000));
            Assert.AreEqual(2, pool.Invoke(200, _ => 2, 1000));
            Assert.IsTrue(pool.GetHealth().Single(item => item.ProcessId == 100).IsBlocked);
        } finally {
            release.Set();
            Assert.IsTrue(first.Wait(2000));
        }
        Assert.AreEqual(3, pool.Invoke(100, _ => 3, 1000));
    }

#if NET8_0_OR_GREATER
    [TestMethod]
    public void ConcurrentKeepAlive_StartsOwnExactlyOneTimerAndStopReleasesIt() {
        var service = WindowKeepAlive.Instance;
        IntPtr handle = new(987654);
        long before = Timer.ActiveCount;
        try {
            Parallel.For(0, 32, _ => service.Start(handle, TimeSpan.FromHours(1)));
            Assert.AreEqual(before + 1, Timer.ActiveCount);
        } finally {
            service.Stop(handle);
        }
        Assert.AreEqual(before, Timer.ActiveCount);
        Assert.IsFalse(service.IsActive(handle));
    }
#endif

    private sealed class PasteCounter : NativeWindow, IDisposable {
        internal PasteCounter() { CreateHandle(new CreateParams { Parent = new IntPtr(-3) }); }
        internal int Count;
        internal int Clicks;
        protected override void WndProc(ref Message message) {
            if (message.Msg == 0x0302) { Count++; }
            else if (message.Msg == 0x0111 && (message.WParam.ToInt64() >> 16) == 0) { Clicks++; }
            else { base.WndProc(ref message); }
        }
        public void Dispose() { DestroyHandle(); }
    }
}
