using System;
using System.Linq;
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
        /// <para type="description">Target monitor index to move the window to.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public int? MonitorIndex { get; set; }

        /// <summary>
        /// <para type="description">The desired window state (Normal, Minimize, Maximize, or Close).</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public WindowState? State { get; set; }

        /// <summary>
        /// <para type="description">Set the window as top-most.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter TopMost { get; set; }

        /// <summary>
        /// <para type="description">Activate the window.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter Activate { get; set; }

        /// <summary>
        /// <para type="description">Re-query the mutated window and report the observed postcondition instead of relying only on mutation success.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter Verify { get; set; }

        /// <summary>
        /// <para type="description">Geometry verification tolerance in pixels.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public int VerificationTolerancePixels { get; set; } = 10;

        /// <summary>
        /// <para type="description">Return a structured mutation result object for each matching window.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter PassThru { get; set; }

        /// <summary>
        /// Applies the requested window modifications.
        /// </summary>
        protected override void BeginProcessing() {
            var automation = new DesktopAutomationService();
            IReadOnlyList<WindowInfo> windows = automation.GetWindows(new WindowQueryOptions {
                TitlePattern = Name,
                IncludeHidden = true,
                IncludeCloaked = true,
                IncludeOwned = true,
                IncludeEmptyTitles = true
            });

            Monitor targetMonitor = null;
            if (MonitorIndex.HasValue) {
                targetMonitor = automation.GetMonitor(index: MonitorIndex.Value);
                if (targetMonitor == null) {
                    WriteWarning($"Monitor with index {MonitorIndex.Value} not found");
                }
            }

            foreach (var window in windows) {
                var action = GetActionDescription();
                if (MonitorIndex.HasValue) {
                    action = $"Move to monitor {MonitorIndex.Value}" + (string.IsNullOrEmpty(action) ? string.Empty : $" and {action}");
                }
                if (ShouldProcess($"Window '{window.Title}'", action)) {
                    DesktopWindowMutationRecord result = null;
                    bool closedWindow = false;
                    try {
                        if (targetMonitor != null) {
                            automation.MoveWindowToMonitor(window.Handle, targetMonitor.Index);
                        }
                        if (Left >= 0 || Top >= 0 || Width >= 0 || Height >= 0) {
                            automation.MoveWindows(
                                new WindowQueryOptions {
                                    Handle = window.Handle,
                                    IncludeHidden = true,
                                    IncludeCloaked = true,
                                    IncludeOwned = true,
                                    IncludeEmptyTitles = true
                                },
                                monitorIndex: null,
                                x: Left >= 0 ? Left : null,
                                y: Top >= 0 ? Top : null,
                                width: Width >= 0 ? Width : null,
                                height: Height >= 0 ? Height : null,
                                activate: false,
                                all: false);
                        }
                        if (State.HasValue) {
                            switch (State.Value) {
                                case WindowState.Close:
                                    automation.CloseWindow(window.Handle);
                                    closedWindow = true;
                                    break;
                                case WindowState.Minimize:
                                    automation.MinimizeWindow(window.Handle);
                                    break;
                                case WindowState.Maximize:
                                    automation.MaximizeWindow(window.Handle);
                                    break;
                                case WindowState.Normal:
                                    automation.RestoreWindow(window.Handle);
                                    break;
                            }
                        }
                        if (!closedWindow && TopMost.IsPresent) {
                            automation.SetWindowTopMost(window.Handle, true);
                        }
                        if (!closedWindow && Activate.IsPresent) {
                            automation.ActivateWindow(window.Handle);
                        }

                        if (Verify.IsPresent || PassThru.IsPresent) {
                            result = DesktopWindowMutationVerifier.Verify(
                                automation,
                                ResolveActionName(),
                                window,
                                VerificationTolerancePixels,
                                expectedMonitorIndex: targetMonitor?.Index ?? MonitorIndex,
                                expectedLeft: Left >= 0 ? Left : null,
                                expectedTop: Top >= 0 ? Top : null,
                                expectedWidth: Width >= 0 ? Width : null,
                                expectedHeight: Height >= 0 ? Height : null,
                                expectedState: closedWindow ? null : State,
                                expectedTopMost: !closedWindow && TopMost.IsPresent ? true : null,
                                requireForeground: !closedWindow && Activate.IsPresent,
                                expectClosed: closedWindow);
                        }
                    } catch (Exception ex) {
                        WriteWarning($"Failed to modify window '{window.Title}': {ex.Message}");
                        if (Verify.IsPresent || PassThru.IsPresent) {
                            result = DesktopWindowMutationVerifier.CreateFailureRecord(ResolveActionName(), window, ex.Message, Verify.IsPresent, VerificationTolerancePixels);
                        }
                    }

                    if (result != null) {
                        WriteObject(result);
                    }
                }
            }
        }

        private string ResolveActionName() {
            if (State.HasValue) {
                return State.Value switch {
                    WindowState.Close => "close",
                    WindowState.Minimize => "minimize",
                    WindowState.Maximize => "maximize",
                    WindowState.Normal => "restore",
                    _ => "window-mutation"
                };
            }

            if (MonitorIndex.HasValue || Left >= 0 || Top >= 0 || Width >= 0 || Height >= 0) {
                return "move";
            }

            if (TopMost.IsPresent) {
                return "topmost";
            }

            if (Activate.IsPresent) {
                return "focus";
            }

            return "window-mutation";
        }

        private string GetActionDescription() {
            var parts = new System.Collections.Generic.List<string>();

            if (State.HasValue) {
                parts.Add(State.Value.ToString());
            }
            if (MonitorIndex.HasValue) {
                parts.Add($"Move to monitor {MonitorIndex.Value}");
            }
            if (Left >= 0 || Top >= 0) {
                parts.Add($"Move to ({Left}, {Top})");
            }
            if (Width >= 0 || Height >= 0) {
                parts.Add($"Resize to {Width}x{Height}");
            }
            if (TopMost.IsPresent) {
                parts.Add("TopMost");
            }
            if (Activate.IsPresent) {
                parts.Add("Activate");
            }

            return string.Join(" and ", parts);
        }
    }
}
