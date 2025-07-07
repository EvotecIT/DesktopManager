using System;
using System.Linq;
using System.Management.Automation;

namespace DesktopManager.PowerShell {
    /// <summary>Snaps a desktop window to a predefined position.</summary>
    /// <para type="synopsis">Snaps a desktop window to a predefined position.</para>
    /// <example>
    ///   <para>Snap Notepad to the left half of the screen</para>
    ///   <code>Set-DesktopWindowSnap -Name "*Notepad*" -Position Left</code>
    /// </example>
    [Cmdlet(VerbsCommon.Set, "DesktopWindowSnap", SupportsShouldProcess = true)]
    public class CmdletSetDesktopWindowSnap : PSCmdlet {
        /// <summary>
        /// <para type="description">The window title to snap. Supports wildcards.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string Name { get; set; }

        /// <summary>
        /// <para type="description">The snap position.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 1)]
        public SnapPosition Position { get; set; }

        /// <summary>
        /// Snaps matching windows to the chosen position.
        /// </summary>
        protected override void BeginProcessing() {
            var manager = new WindowManager();
            var windows = manager.GetWindows(Name);
            foreach (var window in windows) {
                if (ShouldProcess($"Window '{window.Title}'", $"Snap {Position}")) {
                    try {
                        manager.SnapWindow(window, Position);
                    } catch (Exception ex) {
                        WriteWarning($"Failed to snap window '{window.Title}': {ex.Message}");
                    }
                }
            }
        }
    }
}
