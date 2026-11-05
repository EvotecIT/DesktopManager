namespace DesktopManager.Tests;

/// <summary>
/// Protects the formats the library puts on the clipboard to paste text. Building them only registers format names;
/// the clipboard itself is not touched.
/// </summary>
[TestClass]
public class ClipboardPasteFormatTests {
    [TestMethod]
    public void BuildPasteFormats_PrivateMarkersPrecedeTextAndExcludeHistoryCloudAndMonitors() {
        List<KeyValuePair<uint, byte[]>> formats = ClipboardHelper.BuildPasteFormats("pw", privateText: true);

        Assert.AreEqual(4, formats.Count);
        Assert.AreEqual(MonitorNativeMethods.CF_UNICODETEXT, formats[formats.Count - 1].Key, "Private markers must be transferred before secret text.");
        CollectionAssert.AreEqual(new byte[] { (byte)'p', 0, (byte)'w', 0, 0, 0 }, formats[formats.Count - 1].Value);
        foreach (string name in new[] { "ExcludeClipboardContentFromMonitorProcessing", "CanIncludeInClipboardHistory", "CanUploadToCloudClipboard" }) {
            uint format = NativeClipboardMethods.RegisterClipboardFormat(name);
            int markerIndex = formats.FindIndex(entry => entry.Key == format);
            Assert.IsTrue(markerIndex >= 0 && markerIndex < formats.Count - 1, name + " must precede secret text.");
            byte[] value = formats[markerIndex].Value;
            // The history and cloud flags are DWORD 0, which means "do not include".
            CollectionAssert.AreEqual(new byte[4], value, name);
        }
    }

    [TestMethod]
    public void BuildPasteFormats_OrdinaryTextIsPlainUnicodeText() {
        List<KeyValuePair<uint, byte[]>> formats = ClipboardHelper.BuildPasteFormats("x", privateText: false);

        Assert.AreEqual(1, formats.Count);
        Assert.AreEqual(MonitorNativeMethods.CF_UNICODETEXT, formats[0].Key);
    }
}
