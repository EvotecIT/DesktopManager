#if NET8_0_OR_GREATER
namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests for PowerShell control mutation verification records and cmdlet defaults.
/// </summary>
public class PowerShellControlMutationTests {
    [TestMethod]
    /// <summary>
    /// Ensures PowerShell control failure records distinguish verification failures from plain mutation failures.
    /// </summary>
    public void CreateFailureRecord_MapsVerificationState() {
        var requestedControl = new WindowControlInfo {
            Handle = new IntPtr(0x1234),
            ParentWindowHandle = new IntPtr(0x4321),
            ClassName = "Edit",
            Text = "Editor"
        };

        global::DesktopManager.PowerShell.DesktopControlMutationRecord verifiedFailure =
            global::DesktopManager.PowerShell.DesktopControlMutationVerifier.CreateFailureRecord(
                "set-text",
                requestedControl,
                "Verification mismatch.",
                verificationPerformed: true);
        global::DesktopManager.PowerShell.DesktopControlMutationRecord plainFailure =
            global::DesktopManager.PowerShell.DesktopControlMutationVerifier.CreateFailureRecord(
                "set-text",
                requestedControl,
                "Mutation call failed.",
                verificationPerformed: false);

        Assert.IsFalse(verifiedFailure.Success);
        Assert.IsTrue(verifiedFailure.VerificationPerformed);
        Assert.AreEqual(false, verifiedFailure.Verified);
        Assert.AreEqual("error", verifiedFailure.VerificationMode);
        Assert.AreEqual("Edit", verifiedFailure.RequestedControl.ClassName);

        Assert.IsFalse(plainFailure.Success);
        Assert.IsFalse(plainFailure.VerificationPerformed);
        Assert.IsNull(plainFailure.Verified);
        Assert.AreEqual(string.Empty, plainFailure.VerificationMode);
    }

    [TestMethod]
    /// <summary>
    /// Ensures PowerShell control mutation records preserve observed evidence for operator feedback.
    /// </summary>
    public void DesktopControlMutationRecord_StoresObservedEvidence() {
        var requestedControl = new WindowControlInfo {
            Handle = new IntPtr(0x1234),
            ParentWindowHandle = new IntPtr(0x4321),
            ClassName = "Edit",
            Text = "Editor"
        };
        var observedControl = new WindowControlInfo {
            Handle = new IntPtr(0x1234),
            ParentWindowHandle = new IntPtr(0x4321),
            ClassName = "Edit",
            Text = "Editor",
            Value = "Hello world"
        };
        var parentWindow = new WindowInfo {
            Handle = new IntPtr(0x4321),
            Title = "DesktopManager Command Surface",
            ProcessId = 42
        };

        var record = new global::DesktopManager.PowerShell.DesktopControlMutationRecord {
            Action = "set-text",
            Success = true,
            VerificationPerformed = true,
            Verified = true,
            VerificationMode = "text",
            VerificationSummary = "Observed the requested postcondition after 'set-text'.",
            RequestedControl = requestedControl,
            ObservedControl = observedControl,
            ParentWindow = parentWindow,
            VerificationNotes = new[] { "Control text matched." }
        };

        Assert.AreEqual("set-text", record.Action);
        Assert.AreEqual(true, record.Verified);
        Assert.AreEqual("text", record.VerificationMode);
        Assert.AreEqual("Edit", record.RequestedControl.ClassName);
        Assert.AreEqual("Hello world", record.ObservedControl.Value);
        Assert.AreEqual("DesktopManager Command Surface", record.ParentWindow.Title);
        Assert.AreEqual(1, record.VerificationNotes.Count);
    }

    [DataTestMethod]
    [DataRow("CmdletInvokeDesktopControlClick")]
    [DataRow("CmdletSendDesktopControlKey")]
    [DataRow("CmdletSetDesktopControlCheck")]
    [DataRow("CmdletSetDesktopControlText")]
    /// <summary>
    /// Ensures control mutation cmdlets expose the shared verification and pass-through options.
    /// </summary>
    public void ControlMutationCmdlets_ExposeSharedVerificationParameters(string typeName) {
        Type? cmdletType = Type.GetType($"DesktopManager.PowerShell.{typeName}, DesktopManager.PowerShell", throwOnError: true);
        Assert.IsNotNull(cmdletType);

        object? instance = Activator.CreateInstance(cmdletType);
        System.Reflection.PropertyInfo? verifyProperty = cmdletType.GetProperty("Verify");
        System.Reflection.PropertyInfo? passThruProperty = cmdletType.GetProperty("PassThru");

        Assert.IsNotNull(instance);
        Assert.IsNotNull(verifyProperty);
        Assert.IsNotNull(passThruProperty);
    }

    [DataTestMethod]
    [DataRow("CmdletGetDesktopControlState", "Control")]
    [DataRow("CmdletSetDesktopControlFocus", "Control", "EnsureForeground", "PassThru")]
    [DataRow("CmdletSetDesktopControlEnabled", "Control", "Enabled", "PassThru")]
    [DataRow("CmdletSetDesktopControlVisibility", "Control", "Visible", "PassThru")]
    /// <summary>
    /// Ensures the newer handle-first control cmdlets expose the expected PowerShell parameters.
    /// </summary>
    public void ControlStateCmdlets_ExposeExpectedParameters(string typeName, params string[] parameterNames) {
        Type? cmdletType = Type.GetType($"DesktopManager.PowerShell.{typeName}, DesktopManager.PowerShell", throwOnError: true);
        Assert.IsNotNull(cmdletType);

        object? instance = Activator.CreateInstance(cmdletType);
        Assert.IsNotNull(instance);

        foreach (string parameterName in parameterNames) {
            System.Reflection.PropertyInfo? property = cmdletType.GetProperty(parameterName);
            Assert.IsNotNull(property, $"Expected parameter '{parameterName}' on '{typeName}'.");
        }
    }
}
#endif
