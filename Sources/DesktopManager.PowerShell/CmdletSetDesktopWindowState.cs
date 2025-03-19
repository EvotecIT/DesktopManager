using System;
using System.Management.Automation;

namespace DesktopManager.PowerShell {
    /// <summary>Sets the state of a desktop window (close, minimize, maximize, restore).</summary>
    /// <para type="synopsis">Sets the state of a desktop window (close, minimize, maximize, restore).</para>
    /// <para type="description">Changes the state of windows on the desktop. You can identify windows by title (supports wildcards).</para>
    /// <example>
    ///   <para>Minimize a window</para>
    ///   <code>Set-DesktopWindowState -Name "Calculator" -Action Minimize</code>
    /// </example>
    [Cmdlet(VerbsCommon.Set, "DesktopWindowState", SupportsShouldProcess = true)]
    public class CmdletSetDesktopWindowState : PSCmdlet {
        /// <summary>
        /// <para type="description">The title of the window to operate on. Supports wildcards.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string Name { get; set; }

        /// <summary>
        /// <para type="description">The action to perform on the window.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 1)]
        [ValidateSet("Close", "Minimize", "Maximize", "Restore")]
        public string Action { get; set; }

        /// <summary>
        /// Begin processing
        /// </summary>
        protected override void BeginProcessing() {
            var manager = new WindowManager();
            var windows = manager.GetWindows(Name);

            foreach (var window in windows) {
                if (ShouldProcess($"Window '{window.Title}'", Action)) {
                    try {
                        switch (Action.ToLowerInvariant()) {
                            case "close":
                                manager.CloseWindow(window);
                                break;
                            case "minimize":
                                manager.MinimizeWindow(window);
                                break;
                            case "maximize":
                                manager.MaximizeWindow(window);
                                break;
                            case "restore":
                                manager.RestoreWindow(window);
                                break;
                        }
                    }
                    catch (Exception ex) {
                        WriteWarning($"Failed to {Action.ToLowerInvariant()} window '{window.Title}': {ex.Message}");
                    }
                }
            }
        }
    }
}