using System;
using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Stops keep-alive messages for a window.</summary>
/// <para type="synopsis">Stops keep-alive messages for a window.</para>
/// <para type="description">Stops sending periodic input messages previously started with Start-DesktopWindowKeepAlive.</para>
/// <example>
///   <code>Stop-DesktopWindowKeepAlive -Name "*Notepad*"</code>
/// </example>
[Cmdlet(VerbsLifecycle.Stop, "DesktopWindowKeepAlive", SupportsShouldProcess = true)]
public sealed class CmdletStopDesktopWindowKeepAlive : PSCmdlet {
    /// <summary>
    /// <para type="description">Window title to match. Supports wildcards.</para>
    /// </summary>
    [Parameter(Position = 0)]
    public string Name { get; set; } = "*";

    /// <summary>
    /// <para type="description">Stop all keep-alive sessions.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public SwitchParameter All { get; set; }

    /// <inheritdoc/>
    protected override void BeginProcessing() {
        if (All.IsPresent) {
            if (ShouldProcess("All windows", "Stop keep-alive")) {
                WindowKeepAlive.Instance.StopAll();
            }
            return;
        }

        var manager = new WindowManager();
        var windows = manager.GetWindows(Name);
        foreach (var window in windows) {
            if (ShouldProcess(window.Title, "Stop keep-alive")) {
                WindowKeepAlive.Instance.Stop(window.Handle);
            }
        }
    }
}
