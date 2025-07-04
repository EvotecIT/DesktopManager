using System;
using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Starts sending keep-alive input to a window.</summary>
/// <para type="synopsis">Starts sending keep-alive input to a window.</para>
/// <para type="description">Periodically sends a harmless input message to the specified window so that the session stays active.</para>
/// <example>
///   <code>Start-DesktopWindowKeepAlive -Name "*Notepad*"</code>
/// </example>
[Cmdlet(VerbsLifecycle.Start, "DesktopWindowKeepAlive", SupportsShouldProcess = true)]
public sealed class CmdletStartDesktopWindowKeepAlive : PSCmdlet {
    /// <summary>
    /// <para type="description">Window title to match. Supports wildcards.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string Name { get; set; }

    /// <summary>
    /// <para type="description">Interval between keep-alive messages.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(1);

    /// <inheritdoc/>
    protected override void BeginProcessing() {
        var manager = new WindowManager();
        var windows = manager.GetWindows(Name);
        foreach (var window in windows) {
            if (ShouldProcess(window.Title, $"Start keep-alive every {Interval}")) {
                WindowKeepAlive.Instance.Start(window, Interval);
            }
        }
    }
}
