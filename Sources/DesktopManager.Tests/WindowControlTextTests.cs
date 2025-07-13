using System;
using System.Runtime.InteropServices;
#if NETFRAMEWORK
using System.Windows.Forms;
#endif

namespace DesktopManager.Tests;

[TestClass]
public class WindowControlTextTests {
    [TestMethod]
    public void GetAndSetText_Works() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }
#if NETFRAMEWORK
        using Form form = new();
        using TextBox box = new() { Text = "Initial" };
        form.Controls.Add(box);
        form.Show();
        box.CreateControl();

        WindowControlInfo info = new() {
            Handle = box.Handle,
            ClassName = "Edit",
            Id = MonitorNativeMethods.GetDlgCtrlID(box.Handle),
            Text = box.Text
        };

        Assert.AreEqual("Initial", WindowControlService.GetControlText(info));
        WindowControlService.SetControlText(info, "Updated");
        Assert.AreEqual("Updated", WindowControlService.GetControlText(info));
        form.Close();
#else
        Assert.Inconclusive("Test only runs on .NET Framework");
#endif
    }
}

