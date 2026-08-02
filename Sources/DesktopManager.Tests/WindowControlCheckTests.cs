using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
public class WindowControlCheckTests {
    private const int WsChild = 0x40000000;
    private const int WsVisible = 0x10000000;
    private const int BsAutoCheckBox = 0x00000003;
    private const int BsAutoThreeState = 0x00000006;

    [TestMethod]
    public void GetAndSetCheckState_Toggles() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("Check State Harness");
        IntPtr checkBoxHandle = MonitorNativeMethods.CreateWindowExW(
            0,
            "Button",
            "Sample",
            WsChild | WsVisible | BsAutoCheckBox,
            10,
            10,
            120,
            24,
            harness.Form.Handle,
            new IntPtr(1001),
            IntPtr.Zero,
            IntPtr.Zero);
        if (checkBoxHandle == IntPtr.Zero) {
            Assert.Inconclusive("Failed to create a native checkbox control for testing.");
        }

        Application.DoEvents();
        Thread.Sleep(100);

        try {
            WindowControlInfo info = new() {
                ParentWindowHandle = harness.Form.Handle,
                Handle = checkBoxHandle,
                ClassName = "Button",
                Id = MonitorNativeMethods.GetDlgCtrlID(checkBoxHandle),
                Text = "Sample"
            };

            Assert.IsFalse(WindowControlService.GetCheckState(info));
            WindowControlService.SetCheckState(info, true);
            Application.DoEvents();
            Thread.Sleep(100);
            Assert.IsTrue(WindowControlService.GetCheckState(info));
            WindowControlService.SetCheckState(info, false);
            Application.DoEvents();
            Thread.Sleep(100);
            Assert.IsFalse(WindowControlService.GetCheckState(info));
        } finally {
            MonitorNativeMethods.DestroyWindow(checkBoxHandle);
        }
    }

    [TestMethod]
    public void TryGetCheckState_Indeterminate_RemainsNullable() {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            Assert.Inconclusive("Test requires Windows");
        }

        TestHelper.RequireOwnedWindowUiTests();
        using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("Three-State Check Harness");
        IntPtr checkBoxHandle = MonitorNativeMethods.CreateWindowExW(
            0,
            "Button",
            "Three state",
            WsChild | WsVisible | BsAutoThreeState,
            10,
            10,
            120,
            24,
            harness.Form.Handle,
            new IntPtr(1002),
            IntPtr.Zero,
            IntPtr.Zero);
        if (checkBoxHandle == IntPtr.Zero) {
            Assert.Inconclusive("Failed to create a native three-state checkbox control for testing.");
        }

        try {
            IntPtr sent = MonitorNativeMethods.SendMessageTimeout(
                checkBoxHandle,
                MonitorNativeMethods.BM_SETCHECK,
                new IntPtr(2),
                IntPtr.Zero,
                MonitorNativeMethods.SMTO_ABORTIFHUNG,
                1000,
                out _);
            Assert.AreNotEqual(IntPtr.Zero, sent);
            var info = new WindowControlInfo {
                ParentWindowHandle = harness.Form.Handle,
                Handle = checkBoxHandle,
                ClassName = "Button"
            };

            Assert.IsTrue(WindowControlService.TryGetCheckState(info, 1000, out bool? state));
            Assert.IsNull(state);
            Assert.IsNull(WindowControlService.GetCheckState(info));
            DesktopControlObservation observation = DesktopAutomationService.CreateNativeControlObservation(
                harness.Window,
                info,
                new DesktopControlObservationOptions { UseUiAutomation = false });
            Assert.IsNull(observation.IsChecked);
        } finally {
            MonitorNativeMethods.DestroyWindow(checkBoxHandle);
        }
    }
}
