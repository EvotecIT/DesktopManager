using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Waits for a desktop window to appear.</summary>
/// <para type="synopsis">Waits for a desktop window to appear.</para>
/// <para type="description">Polls for a window matching the specified title. Supports wildcards. Throws if the timeout is exceeded.</para>
/// <example>
///   <para>Wait up to 10 seconds for Notepad</para>
///   <code>Wait-DesktopWindow -Name "*Notepad*" -TimeoutMs 10000</code>
/// </example>
[Cmdlet(VerbsLifecycle.Wait, "DesktopWindow")]
public sealed class CmdletWaitDesktopWindow : PSCmdlet {
    /// <summary>
    /// <para type="description">Title of the window to wait for. Supports wildcards.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string Name { get; set; }

    /// <summary>
    /// <para type="description">Timeout in milliseconds. Zero waits indefinitely.</para>
    /// </summary>
    [Parameter]
    public int TimeoutMs { get; set; }

    /// <inheritdoc/>
    protected override void BeginProcessing() {
        var manager = new WindowManager();
        var window = manager.WaitWindow(Name, TimeoutMs);
        WriteObject(window);
    }
}

