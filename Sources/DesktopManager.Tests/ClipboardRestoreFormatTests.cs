namespace DesktopManager.Tests;

/// <summary>Protects privacy metadata and captured payload order without opening or writing the clipboard.</summary>
[TestClass]
public class ClipboardRestoreFormatTests {
    [TestMethod]
    [DataRow("ExcludeClipboardContentFromMonitorProcessing", true)]
    [DataRow("Clipboard Viewer Ignore", true)]
    [DataRow("CanIncludeInClipboardHistory", true)]
    [DataRow("CanUploadToCloudClipboard", true)]
    [DataRow("DesktopManager.Tests.OrdinaryFormat", false)]
    public void OrderRestoreFormats_TransfersExistingPrivacyMarkersBeforePayloads(string name, bool privacyMarker) {
        uint format = NativeClipboardMethods.RegisterClipboardFormat(name);
        Assert.AreNotEqual(0u, format);
        var original = new[] {
            new KeyValuePair<uint, IntPtr>(MonitorNativeMethods.CF_UNICODETEXT, new IntPtr(1)),
            new KeyValuePair<uint, IntPtr>(format, new IntPtr(2)),
            new KeyValuePair<uint, IntPtr>(NativeClipboardMethods.CF_DIB, new IntPtr(3))
        };

        var ordered = ClipboardHelper.OrderRestoreFormats(original).ToArray();

        uint[] expectedFormats = privacyMarker
            ? new[] { format, MonitorNativeMethods.CF_UNICODETEXT, NativeClipboardMethods.CF_DIB }
            : new[] { MonitorNativeMethods.CF_UNICODETEXT, format, NativeClipboardMethods.CF_DIB };
        IntPtr[] expectedHandles = privacyMarker
            ? new[] { new IntPtr(2), new IntPtr(1), new IntPtr(3) }
            : new[] { new IntPtr(1), new IntPtr(2), new IntPtr(3) };
        CollectionAssert.AreEqual(expectedFormats, ordered.Select(entry => entry.Key).ToArray());
        CollectionAssert.AreEqual(expectedHandles, ordered.Select(entry => entry.Value).ToArray());
    }
}
