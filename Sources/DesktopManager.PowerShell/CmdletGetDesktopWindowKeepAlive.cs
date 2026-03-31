using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Lists windows currently kept alive.</summary>
/// <para type="synopsis">Lists windows currently kept alive.</para>
/// <para type="description">Returns information about windows that have active keep-alive timers.</para>
[Cmdlet(VerbsCommon.Get, "DesktopWindowKeepAlive")]
public sealed class CmdletGetDesktopWindowKeepAlive : PSCmdlet {
    /// <inheritdoc/>
    protected override void BeginProcessing() {
        IReadOnlyList<WindowInfo> windows = new DesktopAutomationService()
            .GetWindowKeepAliveWindows()
            .ToList();
        WriteObject(windows, true);
    }
}
