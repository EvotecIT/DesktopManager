using System.Management.Automation;

namespace DesktopManager.PowerShell {
    /// <summary>Gets information about desktop windows.</summary>
    /// <para type="synopsis">Gets information about desktop windows.</para>
    /// <para type="description">Retrieves information about visible windows on the desktop. You can filter windows by name using wildcards.</para>
    /// <example>
    ///   <para>Get all visible windows</para>
    ///   <code>Get-DesktopWindow</code>
    /// </example>
    /// <example>
    ///   <para>Get windows with "Notepad" in the title</para>
    ///   <code>Get-DesktopWindow -Name "*Notepad*"</code>
    /// </example>
    [Cmdlet(VerbsCommon.Get, "DesktopWindow")]
    public class CmdletGetDesktopWindow : PSCmdlet {
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
            WriteObject(windows, true);
        }
    }
}