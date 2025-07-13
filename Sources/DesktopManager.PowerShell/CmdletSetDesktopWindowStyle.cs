using System;
using System.Linq;
using System.Management.Automation;

namespace DesktopManager.PowerShell;

/// <summary>Modifies style flags on a desktop window.</summary>
/// <para type="synopsis">Adds or removes style flags on a desktop window.</para>
/// <example>
///   <para>Enable the maximize box on Notepad</para>
///   <code>Set-DesktopWindowStyle -Name "*Notepad*" -Style MaximizeBox</code>
/// </example>
[Cmdlet(VerbsCommon.Set, "DesktopWindowStyle", SupportsShouldProcess = true)]
public sealed class CmdletSetDesktopWindowStyle : PSCmdlet {
    /// <summary>
    /// <para type="description">Window title to modify. Supports wildcards.</para>
    /// </summary>
    [Parameter(Mandatory = true, Position = 0)]
    public string Name { get; set; }

    /// <summary>
    /// <para type="description">Standard style flags to change.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public WindowStyleFlags? Style { get; set; }

    /// <summary>
    /// <para type="description">Extended style flags to change.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public WindowExStyleFlags? ExStyle { get; set; }

    /// <summary>
    /// <para type="description">Remove the specified flags instead of adding them.</para>
    /// </summary>
    [Parameter(Mandatory = false)]
    public SwitchParameter Disable { get; set; }

    /// <inheritdoc />
    protected override void BeginProcessing() {
        var windows = _manager.GetWindows(Name);
        foreach (var window in windows) {
            if (Style.HasValue) {
                ProcessWindow(window, (long)Style.Value, false);
            }
            if (ExStyle.HasValue) {
                ProcessWindow(window, (long)ExStyle.Value, true);
            }
        }
    }

    private void ProcessWindow(WindowInfo window, long flags, bool extended) {
        string action = Disable.IsPresent ? "Clear style" : "Set style";
        if (ShouldProcess($"Window '{window.Title}'", action)) {
            try {
                _manager.SetWindowStyle(window, flags, !Disable.IsPresent, extended);
            } catch (Exception ex) {
                WriteWarning($"Failed to modify style for '{window.Title}': {ex.Message}");
            }
        }
    }

    private readonly WindowManager _manager = new WindowManager();
}

