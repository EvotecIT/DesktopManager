using System;
using System.Management.Automation;

namespace DesktopManager.PowerShell {
    /// <summary>Sets the position of a desktop window.</summary>
    /// <para type="synopsis">Sets the position of a desktop window.</para>
    /// <para type="description">Sets the position of a window on the desktop. You can identify the window by its title (supports wildcards).</para>
    /// <example>
    ///   <para>Move a specific window to coordinates (100,100)</para>
    ///   <code>Set-DesktopWindowPosition -Name "Calculator" -Left 100 -Top 100</code>
    /// </example>
    [Cmdlet(VerbsCommon.Set, "DesktopWindowPosition", SupportsShouldProcess = true)]
    public class CmdletSetDesktopWindowPosition : PSCmdlet {
        /// <summary>
        /// <para type="description">The title of the window to move. Supports wildcards.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string Name { get; set; }

        /// <summary>
        /// <para type="description">The left position of the window.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 1)]
        public int Left { get; set; }

        /// <summary>
        /// <para type="description">The top position of the window.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 2)]
        public int Top { get; set; }

        /// <summary>
        /// Begin processing
        /// </summary>
        protected override void BeginProcessing() {
            var manager = new WindowManager();
            var windows = manager.GetWindows(Name);

            foreach (var window in windows) {
                if (ShouldProcess($"Window '{window.Title}'", $"Move to position Left: {Left}, Top: {Top}")) {
                    try {
                        manager.SetWindowPosition(window, Left, Top);
                    }
                    catch (Exception ex) {
                        WriteWarning($"Failed to move window '{window.Title}': {ex.Message}");
                    }
                }
            }
        }
    }
}