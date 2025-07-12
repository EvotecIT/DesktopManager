using System.Management.Automation;
using System.Runtime.Versioning;

namespace DesktopManager.PowerShell;

/// <summary>Presses keyboard keys.</summary>
/// <para type="synopsis">Simulates pressing keyboard keys or key combinations.</para>
/// <para type="description">When a window is provided it is brought to the foreground prior to sending the key presses.</para>
/// <example>
///   <code>Invoke-DesktopKeyPress -Keys @([DesktopManager.VirtualKey]::VK_LWIN, [DesktopManager.VirtualKey]::VK_R)</code>
/// </example>
[Cmdlet(VerbsLifecycle.Invoke, "DesktopKeyPress", SupportsShouldProcess = true)]
[SupportedOSPlatform("windows")]
public sealed class CmdletInvokeDesktopKeyPress : PSCmdlet {
    /// <summary>
    /// <para type="description">Keys to press in sequence.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public VirtualKey[] Keys { get; set; } = Array.Empty<VirtualKey>();

    /// <summary>
    /// <para type="description">Optional target window to activate before pressing keys.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public WindowInfo Window { get; set; }

    /// <inheritdoc />
    protected override void BeginProcessing() {
        if (ShouldProcess(Window != null ? Window.Title : "Desktop", "Press keys")) {
            if (Window != null) {
                MonitorNativeMethods.SetForegroundWindow(Window.Handle);
            }
            KeyboardInputService.PressShortcut(Keys);
        }
    }
}
