#if NET8_0_OR_GREATER
namespace DesktopManager.Tests;

[TestClass]
/// <summary>
/// Tests the PowerShell workstation-profile failure contract.
/// </summary>
public class PowerShellWorkstationProfileContractTests {
    [TestMethod]
    /// <summary>
    /// Ensures unsuccessful profile applications become actionable PowerShell errors with the available recovery details.
    /// </summary>
    public void CreateApplyFailureMessage_PreservesFailureDetails() {
        var result = new WorkstationProfileApplyResult(
            succeeded: false,
            rolledBack: true,
            error: "A required monitor is missing.",
            warnings: new[] { "Audio state was not changed." });

        Type? cmdletType = Type.GetType(
            "DesktopManager.PowerShell.CmdletRestoreDesktopWorkstationProfile, DesktopManager.PowerShell",
            throwOnError: true);
        System.Reflection.MethodInfo? method = cmdletType?.GetMethod(
            "CreateApplyFailureMessage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.IsNotNull(method);
        string? message = method.Invoke(null, new object[] { "Office", result }) as string;
        Assert.IsNotNull(message);
        StringAssert.Contains(message, "Workstation profile 'Office' could not be restored.");
        StringAssert.Contains(message, "A required monitor is missing.");
        StringAssert.Contains(message, "Previous desktop state was restored.");
        StringAssert.Contains(message, "Audio state was not changed.");
    }
}
#endif
