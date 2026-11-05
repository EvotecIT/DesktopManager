using System;
using System.Linq;
using System.Management.Automation;

namespace DesktopManager.PowerShell {
    /// <summary>Sets the position, size and state of a desktop window.</summary>
    /// <para type="synopsis">Sets the position, size and state of a desktop window.</para>
    /// <para type="description">Sets the position, size and state of a window on the desktop. You can identify the window by its title (supports wildcards).</para>
    /// <para>Positions and sizes use physical desktop pixels, including from DPI-unaware PowerShell hosts. On a 150% display, a 900-pixel width remains 900 physical pixels rather than 1350. Hidden and DWM-cloaked windows are skipped unless explicitly included.</para>
    /// <example>
    ///   <para>Move a specific window to coordinates (100,100)</para>
    ///   <code>Set-DesktopWindow -Name "Calculator" -Left 100 -Top 100</code>
    /// </example>
    /// <example>
    ///   <para>Set window position and size</para>
    ///   <code>Set-DesktopWindow -Name "Notepad" -Left 100 -Top 100 -Width 800 -Height 600</code>
    /// </example>
    /// <example>
    ///   <para>Minimize a window</para>
    ///   <code>Set-DesktopWindow -Name "Calculator" -State Minimize</code>
    /// </example>
    /// <example>
    ///   <para>Move a window to a monitor left of the primary monitor</para>
    ///   <code>Set-DesktopWindow -Name "Notepad" -Left -1800 -Top 100</code>
    /// </example>
    /// <example>
    ///   <para>Clear a window's topmost state</para>
    ///   <code>Set-DesktopWindow -Name "Notepad" -TopMost:$false</code>
    /// </example>
    [Cmdlet(VerbsCommon.Set, "DesktopWindow", SupportsShouldProcess = true)]
    public class CmdletSetDesktopWindow : PSCmdlet {
        /// <summary>
        /// <para type="description">The title of the window to move. Supports wildcards.</para>
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string Name { get; set; }

        /// <summary>
        /// <para type="description">The left position of the window. Negative values reach monitors left of the primary monitor.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public int Left { get; set; } = -1;

        /// <summary>
        /// <para type="description">The top position of the window. Negative values reach monitors above the primary monitor.</para>
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
        /// <para type="description">Set the window as top-most. Use -TopMost:$false to clear it.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter TopMost { get; set; }

        /// <summary>
        /// <para type="description">Activate the window.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter Activate { get; set; }

        /// <summary>
        /// <para type="description">Also change hidden windows that match the name. Hidden windows are skipped by default.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter IncludeHidden { get; set; }

        /// <summary>
        /// <para type="description">Also change DWM-cloaked windows that match the name, including windows on other virtual desktops. Cloaked windows, such as suspended app frames, are skipped by default because changing them can make them appear.</para>
        /// </summary>
        [Parameter(Mandatory = false)]
        public SwitchParameter IncludeCloaked { get; set; }

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
                IncludeHidden = IncludeHidden.IsPresent,
                IncludeCloaked = IncludeCloaked.IsPresent,
                IncludeOwned = true,
                IncludeEmptyTitles = true
            });
            int? left = Bound(nameof(Left)) ? Left : null;
            int? top = Bound(nameof(Top)) ? Top : null;
            int? width = Bound(nameof(Width)) && Width >= 0 ? Width : null;
            int? height = Bound(nameof(Height)) && Height >= 0 ? Height : null;
            bool? topMost = Bound(nameof(TopMost)) ? TopMost.ToBool() : null;

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
                        if (left.HasValue || top.HasValue || width.HasValue || height.HasValue) {
                            automation.MoveWindows(
                                new WindowQueryOptions {
                                    Handle = window.Handle,
                                    IncludeHidden = true,
                                    IncludeCloaked = true,
                                    IncludeOwned = true,
                                    IncludeEmptyTitles = true
                                },
                                monitorIndex: null,
                                x: left,
                                y: top,
                                width: width,
                                height: height,
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
                        if (!closedWindow && topMost.HasValue) {
                            automation.SetWindowTopMost(window.Handle, topMost.Value);
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
                                expectedLeft: left,
                                expectedTop: top,
                                expectedWidth: width,
                                expectedHeight: height,
                                expectedState: closedWindow ? null : State,
                                expectedTopMost: closedWindow ? null : topMost,
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

            if (MonitorIndex.HasValue || Bound(nameof(Left)) || Bound(nameof(Top)) || Width >= 0 || Height >= 0) {
                return "move";
            }

            if (Bound(nameof(TopMost))) {
                return TopMost.IsPresent ? "topmost" : "clear-topmost";
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
            if (Bound(nameof(Left)) || Bound(nameof(Top))) {
                parts.Add($"Move to ({(Bound(nameof(Left)) ? Left.ToString() : "current")}, {(Bound(nameof(Top)) ? Top.ToString() : "current")})");
            }
            if (Width >= 0 || Height >= 0) {
                parts.Add($"Resize to {Width}x{Height}");
            }
            if (Bound(nameof(TopMost))) {
                parts.Add(TopMost.IsPresent ? "TopMost" : "Clear TopMost");
            }
            if (Activate.IsPresent) {
                parts.Add("Activate");
            }

            return string.Join(" and ", parts);
        }

        // Bound rather than compared with a sentinel, so negative coordinates and -TopMost:$false are honoured.
        private bool Bound(string parameterName) {
            return MyInvocation.BoundParameters.ContainsKey(parameterName);
        }
    }
}
