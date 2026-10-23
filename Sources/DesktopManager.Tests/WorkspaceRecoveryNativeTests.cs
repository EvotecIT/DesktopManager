using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace DesktopManager.Tests;

[TestClass]
#if NET8_0_OR_GREATER
[System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240.0")]
#endif
public sealed class WorkspaceRecoveryNativeTests {
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string id);
    [TestMethod]
    public void NativeRecovery_OwnedDesktopWindowPinsBoundsAndWallpaperRoundTrip() {
        TestHelper.RequireOwnedWindowMutationTests();
        TestHelper.RequireExperimentalDesktopChanges();
        Exception? failure = null;
        var thread = new Thread(() => {
            try { VerifyOwnedDesktop(); }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure != null) { ExceptionDispatchInfo.Capture(failure).Throw(); }
    }

    private static void VerifyOwnedDesktop() {
        string ownedAppId = "Evotec.DesktopManager.WorkspaceTest." + Guid.NewGuid().ToString("N");
        Marshal.ThrowExceptionForHR(SetCurrentProcessExplicitAppUserModelID(ownedAppId));
        var shell = (IWorkspaceShellServices)Activator.CreateInstance(Type.GetTypeFromCLSID(ExperimentalVirtualDesktopNative.ShellClass, true)!)!;
        Guid serviceId = ExperimentalVirtualDesktopNative.ManagerService, iid = typeof(IWorkspaceDesktopManager).GUID;
        var manager = (IWorkspaceDesktopManager)shell.QueryService(ref serviceId, ref iid);
        IWorkspaceDesktop original = manager.GetCurrentDesktop();
        Guid currentId = original.GetId();
        int initialCount = manager.GetCount();
        IWorkspaceDesktop? owned = null;
        string imagePath = Path.Combine(Path.GetTempPath(), "workspace-wallpaper-" + Guid.NewGuid().ToString("N") + ".bmp");
        try {
            owned = manager.CreateDesktop();
            Guid ownedId = owned.GetId();
            using var desktop = new ExperimentalVirtualDesktopService();
            using WinFormsWindowHarness harness = WinFormsWindowHarness.Create("Workspace owned recovery " + Guid.NewGuid().ToString("N"), form => form.ShowInTaskbar = true, 440, 280);
            var windows = new WindowManager();
            WindowPosition position = windows.GetWindowPosition(harness.Window);
            position.VirtualDesktopId = ownedId; position.PinMode = VirtualDesktopPinMode.None;
            desktop.RestoreWindow(position, harness.Window.Handle);

            desktop.CaptureWindow(position);
            Assert.AreEqual(ownedAppId, position.AppUserModelId);
            position.PinMode = VirtualDesktopPinMode.Application;
            Assert.ThrowsExactly<InvalidOperationException>(() => desktop.RestoreWindow(position, harness.Window.Handle));
            desktop.RestoreWindow(position, harness.Window.Handle, restoreApplicationPins: true);
            desktop.CaptureWindow(position);
            Assert.AreEqual(VirtualDesktopPinMode.Application, position.PinMode);
            position.PinMode = VirtualDesktopPinMode.None; position.VirtualDesktopId = ownedId;
            desktop.RestoreWindow(position, harness.Window.Handle, restoreApplicationPins: true);
            using var publicManager = new VirtualDesktopService();
            Assert.AreEqual(ownedId, publicManager.GetWindowDesktopId(harness.Window.Handle));
            desktop.CaptureWindow(position);
            Assert.AreEqual(ownedId, position.VirtualDesktopId);

            position.PinMode = VirtualDesktopPinMode.Window;
            desktop.RestoreWindow(position, harness.Window.Handle);
            desktop.CaptureWindow(position);
            Assert.AreEqual(VirtualDesktopPinMode.Window, position.PinMode);
            position.PinMode = VirtualDesktopPinMode.None; position.VirtualDesktopId = ownedId;
            desktop.RestoreWindow(position, harness.Window.Handle);

            var recovery = new WorkspaceRecoveryService();
            WindowLayout captured = recovery.Capture(new WorkspaceCaptureOptions { IncludeVirtualDesktops = true });
            WindowPosition saved = captured.Windows.Single(window => window.Handle == harness.Window.Handle);
            saved.Left += 35; saved.Right += 35;
            var layout = new WindowLayout { Windows = new List<WindowPosition> { saved } };
            WorkspaceRestoreResult result = recovery.Restore(layout, new WorkspaceRestoreOptions { RestoreVirtualDesktops = true });
            Assert.IsTrue(result.Windows.Single().PositionApplied, result.Windows.Single().Error);
            Assert.IsTrue(result.Windows.Single().VirtualDesktopApplied, result.Windows.Single().Error);
            AssertCurrentDesktop(manager, currentId);

            saved.State = WindowState.Maximize;
            result = recovery.Restore(layout, new WorkspaceRestoreOptions { RestoreVirtualDesktops = true });
            Assert.IsTrue(result.Windows.Single().PositionApplied, result.Windows.Single().Error);
            AssertCurrentDesktop(manager, currentId);
            saved.State = WindowState.Minimize;
            result = recovery.Restore(layout, new WorkspaceRestoreOptions { RestoreVirtualDesktops = true });
            Assert.IsTrue(result.Windows.Single().PositionApplied, result.Windows.Single().Error);
            AssertCurrentDesktop(manager, currentId);

            // Capture an actual maximized-then-minimized window, reset its native history,
            // restore the snapshot, then prove the user's next restore is maximized.
            saved.VirtualDesktopId = currentId;
            saved.State = WindowState.Maximize;
            result = recovery.Restore(layout, new WorkspaceRestoreOptions { RestoreVirtualDesktops = true });
            Assert.IsTrue(result.IsComplete, result.Windows.Single().Error);
            MonitorNativeMethods.ShowWindow(harness.Window.Handle, 7);
            saved = recovery.Capture().Windows.Single(window => window.Handle == harness.Window.Handle);
            Assert.AreEqual(WindowState.Minimize, saved.State);
            Assert.IsTrue(saved.RestoreToMaximized);
            layout.Windows[0] = saved;
            MonitorNativeMethods.ShowWindow(harness.Window.Handle, 4);
            result = recovery.Restore(layout);
            Assert.IsTrue(result.IsComplete, result.Windows.Single().Error);
            MonitorNativeMethods.ShowWindow(harness.Window.Handle, 9);
            Assert.AreEqual(WindowState.Maximize, windows.GetWindowPosition(harness.Window).State);
            AssertCurrentDesktop(manager, currentId);

            using (var bitmap = new Bitmap(16, 16)) {
                using Graphics graphics = Graphics.FromImage(bitmap);
                graphics.Clear(Color.DarkSlateBlue);
                bitmap.Save(imagePath, ImageFormat.Bmp);
            }
            desktop.SetWallpaper(ownedId, imagePath);
            Assert.AreEqual(imagePath, desktop.GetDesktops().Single(item => item.Id == ownedId).WallpaperPath);
            AssertCurrentDesktop(manager, currentId);
        } finally {
            Guid pinService = ExperimentalVirtualDesktopNative.PinsService, pinIid = typeof(IWorkspacePinnedApps).GUID;
            var pins = (IWorkspacePinnedApps)shell.QueryService(ref pinService, ref pinIid);
            try { if (pins.IsAppIdPinned(ownedAppId)) { pins.UnpinAppId(ownedAppId); } }
            finally { ExperimentalVirtualDesktopNative.Release(pins); }
            if (owned != null) {
                manager.RemoveDesktop(owned, original);
                ExperimentalVirtualDesktopNative.Release(owned);
            }
            try { Assert.AreEqual(initialCount, manager.GetCount(), "The temporary desktop must be removed."); }
            finally {
                ExperimentalVirtualDesktopNative.Release(original);
                ExperimentalVirtualDesktopNative.Release(manager);
                ExperimentalVirtualDesktopNative.Release(shell);
                if (File.Exists(imagePath)) { File.Delete(imagePath); }
            }
        }
    }

    private static void AssertCurrentDesktop(IWorkspaceDesktopManager manager, Guid expected) {
        IWorkspaceDesktop actual = manager.GetCurrentDesktop();
        try { Assert.AreEqual(expected, actual.GetId(), "Recovery must not switch the user's active desktop."); }
        finally { ExperimentalVirtualDesktopNative.Release(actual); }
    }
}
