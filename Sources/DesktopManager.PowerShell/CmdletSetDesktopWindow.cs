using System;
using System.Management.Automation;

namespace DesktopManager.PowerShell {
    /// <summary>Sets the position, size and state of a desktop window.</summary>
    /// <para type="synopsis">Sets the position, size and state of a desktop window.</para>
    /// <para type="description">Sets the position, size and state of a window on the desktop. You can identify the window by its title (supports wildcards).</para>
    /// <example>
    ///   <para>Move a specific window to coordinates (100,100)</para>
    ///   <code>Set-DesktopWindowPosition -Name "Calculator" -Left 100 -Top 100</code>
    /// </example>
    /// <example>
    ///   <para>Set window position and size</para>
    ///   <code>Set-DesktopWindowPosition -Name "Notepad" -Left 100 -Top 100 -Width 800 -Height 600</code>
    /// </example>
    /// <example>
    ///   <para>Minimize a window</para>
    ///   <code>Set-DesktopWindowPosition -Name "Calculator" -State Minimize</code>
    /// </example>
    [Cmdlet(VerbsCommon.Set, "DesktopWindow", SupportsShouldProcess = true)]
    public class CmdletSetDesktopWindow : PSCmdlet {
        /// <summary>
        /// <para type="description">The title of the window to move. Supports wildcards.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string Name { get; set; }

        /// <summary>
        /// <para type="description">The left position of the window.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public int Left { get; set; } = -1;

        /// <summary>
        /// <para type="description">The top position of the window.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public int Top { get; set; } = -1;

        /// <summary>
        /// <para type="description">The width of the window. If not specified, current width is maintained.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public int Width { get; set; } = -1;

        /// <summary>
        /// <para type="description">The height of the window. If not specified, current height is maintained.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public int Height { get; set; } = -1;

        /// <summary>
        /// <para type="description">The desired window state (Normal, Minimize, Maximize, or Close).</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public WindowState? State { get; set; }

        /// <summary>
        /// Begin processing
        /// </summary>
        protected override void BeginProcessing() {
            var manager = new WindowManager();
            var windows = manager.GetWindows(Name);

            foreach (var window in windows) {
                var action = GetActionDescription();
                if (ShouldProcess($"Window '{window.Title}'", action)) {
                    try {
                        if (State.HasValue) {
                            switch (State.Value) {
                                case WindowState.Close:
                                    manager.CloseWindow(window);
                                    continue; // Skip any position/size changes
                                case WindowState.Minimize:
                                    manager.MinimizeWindow(window);
                                    break;
                                case WindowState.Maximize:
                                    manager.MaximizeWindow(window);
                                    break;
                                case WindowState.Normal:
                                    manager.RestoreWindow(window);
                                    break;
                            }
                        }

                        if (Left >= 0 || Top >= 0 || Width >= 0 || Height >= 0) {
                            manager.SetWindowPosition(window, Left, Top, Width, Height);
                        }
                    } catch (Exception ex) {
                        WriteWarning($"Failed to modify window '{window.Title}': {ex.Message}");
                    }
                }
            }
        }

        private string GetActionDescription() {
            var parts = new System.Collections.Generic.List<string>();

            if (State.HasValue) {
                parts.Add(State.Value.ToString());
            }
            if (Left >= 0 || Top >= 0) {
                parts.Add($"Move to ({Left}, {Top})");
            }
            if (Width >= 0 || Height >= 0) {
                parts.Add($"Resize to {Width}x{Height}");
            }

            return string.Join(" and ", parts);
        }
    }
}