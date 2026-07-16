using System;
using System.Collections.Generic;
using System.Text.Json;

namespace DesktopManager.Cli;

internal static partial class McpCatalog {
    public static object[] GetResources() {
        return new object[] {
            new {
                name = "desktop_monitors",
                title = "Desktop Monitors",
                uri = "desktop://monitors",
                description = "Current connected monitor list as JSON.",
                mimeType = "application/json"
            },
            new {
                name = "desktop_windows_visible",
                title = "Visible Windows",
                uri = "desktop://windows/visible",
                description = "Current visible windows as JSON.",
                mimeType = "application/json"
            },
            new {
                name = "desktop_active_window",
                title = "Active Window",
                uri = "desktop://windows/active",
                description = "Current active window as JSON.",
                mimeType = "application/json"
            },
            new {
                name = "desktop_layouts",
                title = "Named Layouts",
                uri = "desktop://layouts",
                description = "Saved named layouts as JSON.",
                mimeType = "application/json"
            },
            new {
                name = "desktop_targets",
                title = "Named Targets",
                uri = "desktop://targets",
                description = "Saved reusable window-relative targets as JSON.",
                mimeType = "application/json"
            },
            new {
                name = "desktop_visual_baselines",
                title = "Named Visual Baselines",
                uri = "desktop://visual-baselines",
                description = "Saved reusable visual baselines as JSON.",
                mimeType = "application/json"
            },
            new {
                name = "desktop_control_targets",
                title = "Named Control Targets",
                uri = "desktop://control-targets",
                description = "Saved reusable control selector targets as JSON.",
                mimeType = "application/json"
            },
            new {
                name = "desktop_snapshot_current",
                title = "Current Desktop Snapshot",
                uri = "desktop://snapshot/current",
                description = "Current windows and monitors summary as JSON.",
                mimeType = "application/json"
            }
        };
    }

    public static object[] GetPrompts() {
        return new object[] {
            new {
                name = "prepare_for_coding",
                title = "Prepare For Coding",
                description = "Arrange the desktop for focused coding work.",
                arguments = new object[] {
                    new {
                        name = "layoutName",
                        description = "Preferred named layout to apply before focusing the editor.",
                        required = false
                    }
                }
            },
            new {
                name = "prepare_for_screen_sharing",
                title = "Prepare For Screen Sharing",
                description = "Arrange the desktop for a clean screen sharing session.",
                arguments = new object[] {
                    new {
                        name = "layoutName",
                        description = "Preferred named layout to apply before sharing.",
                        required = false
                    }
                }
            },
            new {
                name = "clean_up_distractions",
                title = "Clean Up Distractions",
                description = "Hide or minimize noisy windows before focused work.",
                arguments = Array.Empty<object>()
            }
        };
    }

    public static object ReadResource(string uri) {
        return uri switch {
            "desktop://monitors" => DesktopOperations.ListMonitors(connectedOnly: true),
            "desktop://windows/visible" => DesktopOperations.ListWindows(new WindowSelectionCriteria()),
            "desktop://windows/active" => DesktopOperations.GetActiveWindow(),
            "desktop://layouts" => DesktopOperations.ListLayouts(),
            "desktop://targets" => DesktopOperations.ListWindowTargets(),
            "desktop://visual-baselines" => DesktopOperations.ListVisualBaselines(),
            "desktop://control-targets" => DesktopOperations.ListControlTargets(),
            "desktop://snapshot/current" => DesktopOperations.GetCurrentSnapshotSummary(),
            _ => throw new CommandLineException($"Unknown resource '{uri}'.")
        };
    }

    public static object GetPrompt(string name, JsonElement arguments) {
        string? layoutName = ReadOptionalString(arguments, "layoutName");
        return name switch {
            "prepare_for_coding" => BuildPrompt("Prepare the desktop for focused coding work.", layoutName, "Start by listing named layouts. If the requested layout exists, apply it. Then inspect visible windows and focus the main editor or terminal window. If the layout is missing, explain the gap and suggest the nearest saved layout."),
            "prepare_for_screen_sharing" => BuildPrompt("Prepare the desktop for a clean screen sharing session.", layoutName, "Start by listing named layouts. If the requested layout exists, apply it. Then inspect visible windows, minimize obviously distracting windows, and focus the application that should be shared."),
            "clean_up_distractions" => BuildPrompt("Clean up distracting windows before focused work.", null, "Inspect visible windows first. Minimize obvious distractions such as chat, mail, or utility windows when appropriate, but avoid closing anything. Explain what changed."),
            _ => throw new CommandLineException($"Unknown prompt '{name}'.")
        };
    }

    private static object BuildPrompt(string summary, string? layoutName, string instructions) {
        string layoutText = string.IsNullOrWhiteSpace(layoutName) ? "No preferred layout was provided." : $"Preferred layout: {layoutName}.";
        return new {
            description = summary,
            messages = new[] {
                new {
                    role = "user",
                    content = new {
                        type = "text",
                        text = $"{summary} {layoutText} {instructions}"
                    }
                }
            }
        };
    }
}
