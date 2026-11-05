using System.Collections.Concurrent;
#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace DesktopManager.Tests;

/// <summary>
/// Protects which copies the clipboard watcher records and its listener lifecycle.
/// </summary>
[TestClass]
[DoNotParallelize]
#if NET5_0_OR_GREATER
[SupportedOSPlatform("windows")]
#endif
public class ClipboardWatcherTests {
    [TestMethod]
    [DataRow(false, false, false, null, false, DisplayName = "No markers")]
    [DataRow(true, false, false, null, true, DisplayName = "Excluded from monitoring")]
    [DataRow(false, true, false, null, true, DisplayName = "Clipboard viewer ignore")]
    [DataRow(false, false, true, 1, false, DisplayName = "History allowed")]
    [DataRow(false, false, true, 0, true, DisplayName = "History refused")]
    [DataRow(false, false, true, null, true, DisplayName = "History flag unreadable")]
    public void IsPrivate_HonoursEachMarkerAppsSetOnPrivateCopies(
        bool excludeFromMonitoring, bool viewerIgnore, bool historyFlagPresent, int? historyFlag, bool expected) {
        Assert.AreEqual(expected, ClipboardWatcher.IsPrivate(excludeFromMonitoring, viewerIgnore, historyFlagPresent, historyFlag));
    }

    [TestMethod]
    public void Lifecycle_StartsListenerAndStopsCleanlyWithoutTouchingTheClipboard() {
        var watcher = new ClipboardWatcher();
        watcher.Dispose();
        watcher.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => watcher.SetText("not written"));
    }

    [TestMethod]
    [TestCategory("UITest")]
    public void ClipboardChanged_ReportsOrdinaryCopiesButNotPrivateCopiesOrItsOwnPuts() {
        TestHelper.RequireOwnedWindowUiTests();
        using ClipboardHelper.ClipboardSnapshot original = ClipboardHelper.CaptureSnapshot();
        try {
            var seen = new BlockingCollection<ClipboardChangedEventArgs>();
            using (var watcher = new ClipboardWatcher()) {
                watcher.ClipboardChanged += (_, copy) => seen.Add(copy);

                SetPrivateText("private copy");
                watcher.SetText("restored entry");
                ClipboardHelper.SetText("ordinary copy");

                Assert.IsTrue(seen.TryTake(out ClipboardChangedEventArgs? first, 5000), "The ordinary copy was not reported.");
                Assert.AreEqual("ordinary copy", first!.Text);
                Assert.IsFalse(seen.TryTake(out _, 500), "Only the ordinary copy may be reported.");
            }
        } finally {
            original.Restore();
        }
    }

    private static void SetPrivateText(string text) {
        uint marker = NativeClipboardMethods.RegisterClipboardFormat("ExcludeClipboardContentFromMonitorProcessing");
        ClipboardHelper.OpenClipboardWithRetry(5, 50);
        try {
            Assert.IsTrue(MonitorNativeMethods.EmptyClipboard());
            SetOpenData(MonitorNativeMethods.CF_UNICODETEXT, ClipboardHelper.ToUnicodeTextBytes(text));
            SetOpenData(marker, new byte[4]);
        } finally {
            MonitorNativeMethods.CloseClipboard();
        }
    }

    private static void SetOpenData(uint format, byte[] data) {
        IntPtr memory = MonitorNativeMethods.GlobalAlloc(MonitorNativeMethods.GMEM_MOVEABLE, (UIntPtr)data.Length);
        IntPtr pointer = MonitorNativeMethods.GlobalLock(memory);
        System.Runtime.InteropServices.Marshal.Copy(data, 0, pointer, data.Length);
        MonitorNativeMethods.GlobalUnlock(memory);
        if (MonitorNativeMethods.SetClipboardData(format, memory) == IntPtr.Zero) {
            MonitorNativeMethods.GlobalFree(memory);
            Assert.Fail("SetClipboardData failed.");
        }
    }
}
