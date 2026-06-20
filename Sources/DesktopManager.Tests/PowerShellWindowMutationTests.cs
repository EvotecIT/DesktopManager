#if NET8_0_OR_GREATER
namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for PowerShell window mutation verification records and cmdlet defaults.
/// </summary>
public class PowerShellWindowMutationTests {
    [TestMethod]
    /// <summary>
    /// Ensures PowerShell failure records distinguish verification failures from plain mutation failures.
    /// </summary>
    public void CreateFailureRecord_MapsVerificationState() {
        var requestedWindow = new WindowInfo {
            Title = "Editor",
            Handle = new IntPtr(0x1234),
            ProcessId = 42
        };

        global::DesktopManager.PowerShell.DesktopWindowMutationRecord verifiedFailure =
            global::DesktopManager.PowerShell.DesktopWindowMutationVerifier.CreateFailureRecord(
                "move",
                requestedWindow,
                "Verification mismatch.",
                verificationPerformed: true,
                tolerancePixels: 12);
        global::DesktopManager.PowerShell.DesktopWindowMutationRecord plainFailure =
            global::DesktopManager.PowerShell.DesktopWindowMutationVerifier.CreateFailureRecord(
                "move",
                requestedWindow,
                "Mutation call failed.",
                verificationPerformed: false,
                tolerancePixels: 12);

        Assert.IsFalse(verifiedFailure.Success);
        Assert.IsTrue(verifiedFailure.VerificationPerformed);
        Assert.AreEqual(false, verifiedFailure.Verified);
        Assert.AreEqual("error", verifiedFailure.VerificationMode);
        Assert.AreEqual(12, verifiedFailure.VerificationTolerancePixels);
        Assert.AreEqual("Editor", verifiedFailure.RequestedWindow.Title);

        Assert.IsFalse(plainFailure.Success);
        Assert.IsFalse(plainFailure.VerificationPerformed);
        Assert.IsNull(plainFailure.Verified);
        Assert.AreEqual(string.Empty, plainFailure.VerificationMode);
    }

    [TestMethod]
    /// <summary>
    /// Ensures PowerShell mutation records preserve observed and active window evidence for operator feedback.
    /// </summary>
    public void DesktopWindowMutationRecord_StoresObservedEvidence() {
        var requestedWindow = new WindowInfo {
            Title = "Editor",
            Handle = new IntPtr(0x1234),
            ProcessId = 42
        };
        var observedWindow = new WindowInfo {
            Title = "Editor",
            Handle = new IntPtr(0x1234),
            ProcessId = 42
        };
        var activeWindow = new WindowInfo {
            Title = "Editor",
            Handle = new IntPtr(0x1234),
            ProcessId = 42
        };

        var record = new global::DesktopManager.PowerShell.DesktopWindowMutationRecord {
            Action = "move",
            Success = true,
            VerificationPerformed = true,
            Verified = true,
            VerificationMode = "geometry",
            VerificationSummary = "Observed the requested postcondition after 'move'.",
            VerificationTolerancePixels = 10,
            RequestedWindow = requestedWindow,
            ObservedWindow = observedWindow,
            ActiveWindow = activeWindow,
            VerificationNotes = new[] { "Window geometry matched." }
        };

        Assert.AreEqual("move", record.Action);
        Assert.AreEqual(true, record.Verified);
        Assert.AreEqual("geometry", record.VerificationMode);
        Assert.AreEqual("Editor", record.RequestedWindow.Title);
        Assert.AreEqual("Editor", record.ObservedWindow.Title);
        Assert.AreEqual("Editor", record.ActiveWindow.Title);
        Assert.AreEqual(1, record.VerificationNotes.Count);
    }

    [TestMethod]
    [DataRow("CmdletInvokeDesktopWindowClick")]
    [DataRow("CmdletInvokeDesktopWindowDrag")]
    [DataRow("CmdletInvokeDesktopWindowScroll")]
    /// <summary>
    /// Ensures the pointer-style PowerShell cmdlets expose the shared verification and pass-through options.
    /// </summary>
    public void PointerWindowCmdlets_ExposeSharedVerificationParameters(string typeName) {
        Type? cmdletType = Type.GetType($"DesktopManager.PowerShell.{typeName}, DesktopManager.PowerShell", throwOnError: true);
        Assert.IsNotNull(cmdletType);

        object? instance = Activator.CreateInstance(cmdletType);
        System.Reflection.PropertyInfo? verifyProperty = cmdletType.GetProperty("Verify");
        System.Reflection.PropertyInfo? toleranceProperty = cmdletType.GetProperty("VerificationTolerancePixels");
        System.Reflection.PropertyInfo? passThruProperty = cmdletType.GetProperty("PassThru");

        Assert.IsNotNull(instance);
        Assert.IsNotNull(verifyProperty);
        Assert.IsNotNull(toleranceProperty);
        Assert.IsNotNull(passThruProperty);
        Assert.AreEqual(10, toleranceProperty.GetValue(instance));
    }
}
#endif
