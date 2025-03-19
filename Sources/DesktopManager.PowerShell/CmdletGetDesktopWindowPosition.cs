using System;
using System.Management.Automation;

namespace DesktopManager.PowerShell {
    /// <summary>Gets the position of desktop windows.</summary>
    /// <para type="synopsis">Gets the position of desktop windows.</para>
    /// <para type="description">Retrieves position information about visible windows on the desktop. You can filter windows by name using wildcards.</para>
    /// <example>
    ///   <para>Get position of all visible windows</para>
    ///   <code>Get-DesktopWindowPosition</code>
    /// </example>
    /// <example>
    ///   <para>Get position of windows with "Notepad" in the title</para>
    ///   <code>Get-DesktopWindowPosition -Name "*Notepad*"</code>
    /// </example>
    [Cmdlet(VerbsCommon.Get, "DesktopWindowPosition")]
    public class CmdletGetDesktopWindowPosition : PSCmdlet {
        /// <summary>
        /// <para type="description">Filter windows by title. Supports wildcards.</para>
        /// </summary>
        [Parameter(Position = 0)]
        public string Name { get; set; } = "*";

        /// <summary>
        /// Begin processing
        /// </summary>
        protected override void BeginProcessing() {
            var manager = new WindowManager();
            var windows = manager.GetWindows(Name);

            foreach (var window in windows) {
                try {
                    var position = manager.GetWindowPosition(window);
                    WriteObject(position);
                }
                catch (Exception ex) {
                    WriteWarning($"Failed to get position for window '{window.Title}': {ex.Message}");
                }
            }
        }
    }
}