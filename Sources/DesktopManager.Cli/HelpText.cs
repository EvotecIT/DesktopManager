namespace DesktopManager.Cli;

internal static class HelpText {
    public static string GetGeneralHelp() {
        return """
desktopmanager - Windows desktop automation CLI

Usage:
  desktopmanager <group> <command> [options]

Groups:
  desktop    Manage desktop-wide personalization
  window     List and control windows
  control    Inspect and interact with child controls
  monitor    Inspect and configure monitors
  process    Start desktop applications
  screenshot Capture the desktop, monitors, or windows
  target     Save and resolve reusable window-relative targets
  control-target Save and resolve reusable control selector targets
  layout     Save, apply, and list named layouts
  snapshot   Save, restore, and list named snapshots
  diagnostic Read DesktopManager diagnostic artifacts
  workflow   Run higher-level desktop workflows
  mcp        Host an MCP server over stdio
  help       Show help for a command group

Examples:
  desktopmanager desktop background-color
  desktopmanager window list
  desktopmanager window wait --process notepad --timeout-ms 5000
  desktopmanager control list --window-process notepad
  desktopmanager process start notepad.exe --wait-for-input-idle-ms 1000
  desktopmanager process start-and-wait notepad.exe --timeout-ms 5000
  desktopmanager screenshot desktop
  desktopmanager target save editor-center --x-ratio 0.5 --y-ratio 0.5 --client-area
  desktopmanager control-target save edge-address --control-type Edit --background-text --uia
  desktopmanager window move --title "Visual Studio Code" --x 0 --y 0 --width 1920 --height 1400
  desktopmanager monitor list --json
  desktopmanager monitor set-resolution --primary --width 2560 --height 1440 --orientation default
  desktopmanager layout save coding
  desktopmanager layout apply coding --validate
  desktopmanager layout assert coding --position-tolerance-px 50 --size-tolerance-px 50
  desktopmanager snapshot save workday
  desktopmanager snapshot restore workday
  desktopmanager diagnostic hosted-session --summary-only

Use:
  desktopmanager help desktop
  desktopmanager help window
  desktopmanager help control
  desktopmanager help monitor
  desktopmanager help process
  desktopmanager help screenshot
  desktopmanager help target
  desktopmanager help control-target
  desktopmanager help layout
  desktopmanager help snapshot
  desktopmanager help diagnostic
  desktopmanager help workflow
  desktopmanager help mcp
""";
    }

    public static string GetWindowHelp() {
        return """
Window commands:
  desktopmanager window list [--title <pattern>] [--process <pattern>] [--class <pattern>] [--pid <id>] [--handle <value>] [--active] [--include-empty] [--include-hidden] [--exclude-cloaked] [--exclude-owned] [--json]
  desktopmanager window geometry [selector] [--all] [--json]
  desktopmanager window process-info [selector] [--all] [--json]
  desktopmanager window owner-process-info [selector] [--all] [--json]
  desktopmanager window exists [selector] [--json]
  desktopmanager window active-matches [selector] [--json]
  desktopmanager window move [selector] [--monitor <index>] [--x <value>] [--y <value>] [--width <value>] [--height <value>] [--activate] [--capture-before] [--capture-after] [--artifact-directory <path>] [--verify] [--verify-tolerance-px <value>] [--all] [--json]
  desktopmanager window click [selector] ((--x <value> --y <value> | --x-ratio <value> --y-ratio <value>) | --target <name>) [--button <left|right>] [--activate] [--client-area] [--capture-before] [--capture-after] [--artifact-directory <path>] [--verify] [--verify-tolerance-px <value>] [--all] [--json]
  desktopmanager window drag [selector] (((--start-x <value> --start-y <value>) | (--start-x-ratio <value> --start-y-ratio <value>)) ((--end-x <value> --end-y <value>) | (--end-x-ratio <value> --end-y-ratio <value>)) | (--start-target <name> --end-target <name>)) [--button <left|right>] [--step-delay-ms <value>] [--activate] [--client-area] [--capture-before] [--capture-after] [--artifact-directory <path>] [--verify] [--verify-tolerance-px <value>] [--all] [--json]
  desktopmanager window scroll [selector] ((--x <value> --y <value> | --x-ratio <value> --y-ratio <value>) | --target <name>) --delta <value> [--activate] [--client-area] [--capture-before] [--capture-after] [--artifact-directory <path>] [--verify] [--verify-tolerance-px <value>] [--all] [--json]
  desktopmanager window focus [selector] [--capture-before] [--capture-after] [--artifact-directory <path>] [--verify] [--verify-tolerance-px <value>] [--all] [--json]
  desktopmanager window keep-alive-list [--json]
  desktopmanager window keep-alive-start [selector] [--interval-ms <value>] [--all] [--json]
  desktopmanager window keep-alive-stop [selector] [--all | --all-sessions] [--json]
  desktopmanager window minimize [selector] [--capture-before] [--capture-after] [--artifact-directory <path>] [--verify] [--verify-tolerance-px <value>] [--all] [--json]
  desktopmanager window maximize [selector] [--capture-before] [--capture-after] [--artifact-directory <path>] [--verify] [--verify-tolerance-px <value>] [--all] [--json]
  desktopmanager window restore [selector] [--capture-before] [--capture-after] [--artifact-directory <path>] [--verify] [--verify-tolerance-px <value>] [--all] [--json]
  desktopmanager window close [selector] [--capture-before] [--capture-after] [--artifact-directory <path>] [--verify] [--verify-tolerance-px <value>] [--all] [--json]
  desktopmanager window topmost [selector] (--on | --off) [--capture-before] [--capture-after] [--artifact-directory <path>] [--verify] [--verify-tolerance-px <value>] [--all] [--json]
  desktopmanager window visibility [selector] (--show | --hide) [--capture-before] [--capture-after] [--artifact-directory <path>] [--verify] [--verify-tolerance-px <value>] [--all] [--json]
  desktopmanager window transparency [selector] --alpha <value> [--capture-before] [--capture-after] [--artifact-directory <path>] [--verify] [--verify-tolerance-px <value>] [--all] [--json]
  desktopmanager window snap [selector] --position <left|right|top-left|top-right|bottom-left|bottom-right> [--capture-before] [--capture-after] [--artifact-directory <path>] [--verify] [--verify-tolerance-px <value>] [--all] [--json]
  desktopmanager window type [selector] --text <value> [--paste] [--foreground-input] [--physical-keys] [--hosted-session] [--script] [--chunk-size <value>] [--line-delay-ms <value>] [--delay-ms <value>] [--capture-before] [--capture-after] [--artifact-directory <path>] [--verify] [--verify-tolerance-px <value>] [--all] [--json]
  desktopmanager window keys [selector] --keys <value>[,<value>...] [--no-activate] [--capture-before] [--capture-after] [--artifact-directory <path>] [--verify] [--verify-tolerance-px <value>] [--all] [--json]
  desktopmanager window wait [selector] [--timeout-ms <value>] [--interval-ms <value>] [--all] [--json]

Selectors:
  --title <pattern>
  --process <pattern>
  --class <pattern>
  --pid <id>
  --handle <value>
  --active
  --include-empty
  --capture-before
  --capture-after
  --artifact-directory <path>
  --verify
  --verify-tolerance-px <value>

Examples:
  desktopmanager window list --title "*Notepad*" --json
  desktopmanager window geometry --handle 0xFF1802 --json
  desktopmanager window process-info --process notepad
  desktopmanager window owner-process-info --handle 0xFF1802 --json
  desktopmanager window exists --process notepad
  desktopmanager window active-matches --title "Codex"
  desktopmanager window click --process notepad --x 200 --y 200 --client-area
  desktopmanager window click --process notepad --x-ratio 0.5 --y-ratio 0.5 --client-area
  desktopmanager window click --process notepad --target editor-center
  desktopmanager window drag --process notepad --start-x 200 --start-y 200 --end-x 500 --end-y 200 --client-area
  desktopmanager window drag --process notepad --start-x-ratio 0.2 --start-y-ratio 0.2 --end-x-ratio 0.6 --end-y-ratio 0.2 --client-area
  desktopmanager window drag --process notepad --start-target editor-center --end-target editor-right
  desktopmanager window scroll --process notepad --x 200 --y 200 --delta -120 --client-area
  desktopmanager window scroll --process notepad --x-ratio 0.5 --y-ratio 0.5 --delta -120 --client-area
  desktopmanager window scroll --process notepad --target editor-center --delta -120
  desktopmanager window type --active --text "Hello world"
  desktopmanager window type --process Devolutions.RemoteDesktopManager --text "safe probe" --foreground-input
  desktopmanager window type --process Devolutions.RemoteDesktopManager --text "safe probe" --physical-keys
  desktopmanager window type --process Devolutions.RemoteDesktopManager --text "safe probe" --hosted-session
  desktopmanager window type --process Devolutions.RemoteDesktopManager --text "Write-Host 'hi'`nGet-Date" --script --foreground-input --line-delay-ms 20
  desktopmanager window move --title "Visual Studio Code" --x 0 --y 0 --width 1920 --height 1400 --activate
  desktopmanager window move --title "Visual Studio Code" --x 0 --y 0 --width 1920 --height 1400 --verify --verify-tolerance-px 12
  desktopmanager window keep-alive-list
  desktopmanager window keep-alive-start --process notepad --interval-ms 30000
  desktopmanager window keep-alive-stop --all-sessions
  desktopmanager window maximize --process notepad --verify
  desktopmanager window restore --process notepad
  desktopmanager window close --process notepad --verify
  desktopmanager window topmost --process notepad --on
  desktopmanager window visibility --process notepad --hide
  desktopmanager window transparency --process notepad --alpha 180
  desktopmanager window snap --process notepad --position left
  desktopmanager window type --process notepad --text "Hello world"
  desktopmanager window keys --process msedge --keys VK_RETURN
  desktopmanager window wait --process notepad --timeout-ms 5000

Notes:
  --hosted-session expects the target editor surface to already own focus.
  Hosted-session typing stops immediately if foreground ownership changes mid-input.
  Hosted-session harness diagnostics are written under Artifacts\HostedSessionTyping with a .json snapshot and a companion .summary.txt file.
  --verify re-queries the mutated window and reports observed postconditions instead of only the request outcome.
""";
    }

    public static string GetDesktopHelp() {
        return """
Desktop commands:
  desktopmanager desktop background-color [--json]
  desktopmanager desktop set-background-color --color <decimal|0xRRGGBB|#RRGGBB> [--json]
  desktopmanager desktop wallpaper-position [--json]
  desktopmanager desktop set-wallpaper-position --position <center|tile|stretch|fit|fill|span> [--json]
  desktopmanager desktop start-slideshow --image <path> [--image <path>...] [--json]
  desktopmanager desktop stop-slideshow [--json]
  desktopmanager desktop advance-slideshow --direction <forward|backward> [--json]

Examples:
  desktopmanager desktop background-color
  desktopmanager desktop set-background-color --color 0x102040
  desktopmanager desktop wallpaper-position --json
  desktopmanager desktop set-wallpaper-position --position fill
  desktopmanager desktop start-slideshow --image C:\Wallpapers\img1.jpg --image C:\Wallpapers\img2.jpg
  desktopmanager desktop stop-slideshow
  desktopmanager desktop advance-slideshow --direction forward
""";
    }

    public static string GetTargetHelp() {
        return """
Target commands:
  desktopmanager target save <name> (--x <value> --y <value> | --x-ratio <value> --y-ratio <value>) [(--width <value> --height <value>) | (--width-ratio <value> --height-ratio <value>)] [--client-area] [--description <text>] [--json]
  desktopmanager target get <name> [--json]
  desktopmanager target list [--json]
  desktopmanager target resolve <name> [selector] [--all] [--json]

Selectors:
  --title <pattern>
  --process <pattern>
  --class <pattern>
  --pid <id>
  --handle <value>
  --active
  --include-empty

Examples:
  desktopmanager target save editor-center --x-ratio 0.5 --y-ratio 0.5 --client-area
  desktopmanager target save edge-editor-pane --x-ratio 0.1 --y-ratio 0.15 --width-ratio 0.8 --height-ratio 0.7 --client-area
  desktopmanager target save browser-top-right --x-ratio 0.9 --y-ratio 0.1 --client-area --description "Toolbar area"
  desktopmanager target get editor-center --json
  desktopmanager target list
  desktopmanager target resolve editor-center --process notepad --json
""";
    }

    public static string GetControlTargetHelp() {
        return """
Control-target commands:
  desktopmanager control-target save <name> [control-selector] [--description <text>] [--json]
  desktopmanager control-target get <name> [--json]
  desktopmanager control-target list [--json]
  desktopmanager control-target resolve <name> [window-selector] [--all] [--all-windows] [--json]

Window selectors:
  --title <pattern>
  --process <pattern>
  --class <pattern>
  --pid <id>
  --handle <value>
  --active
  --include-empty

Control selectors:
  --class <pattern>
  --text-pattern <pattern>
  --value-pattern <pattern>
  --id <value>
  --handle <value>
  --automation-id <pattern>
  --control-type <pattern>
  --framework-id <pattern>
  --enabled
  --disabled
  --focusable
  --not-focusable
  --background-click
  --background-text
  --background-keys
  --foreground-fallback
  --uia
  --include-uia
  --ensure-foreground

Examples:
  desktopmanager control-target save edge-address --control-type Edit --background-text --uia --description "Browser address bar"
  desktopmanager control-target save codex-sidebar-toggle --control-type Button --text-pattern "Hide sidebar" --background-click --uia
  desktopmanager control-target get edge-address --json
  desktopmanager control-target list
  desktopmanager control-target resolve edge-address --process msedge --json
""";
    }

    public static string GetControlHelp() {
        return """
Control commands:
  desktopmanager control list [window-selector] ([control-selector] | --target <name>) [--all] [--all-windows] [--json]
  desktopmanager control diagnose [window-selector] ([control-selector] | --target <name>) [--sample-limit <value>] [--action-probe] [--all-windows] [--json]
  desktopmanager control exists [window-selector] ([control-selector] | --target <name>) [--all] [--all-windows] [--json]
  desktopmanager control assert-value [window-selector] ([control-selector] | --target <name>) --expected-value <value> [--contains] [--all] [--all-windows] [--json]
  desktopmanager control wait [window-selector] ([control-selector] | --target <name>) [--timeout-ms <value>] [--interval-ms <value>] [--all] [--all-windows] [--json]
  desktopmanager control click [window-selector] ([control-selector] | --target <name>) [--button <left|right>] [--capture-before] [--capture-after] [--artifact-directory <path>] [--all] [--all-windows] [--json]
  desktopmanager control set-text [window-selector] ([control-selector] | --target <name>) --text <value> [--allow-foreground-input] [--capture-before] [--capture-after] [--artifact-directory <path>] [--all] [--all-windows] [--json]
  desktopmanager control send-keys [window-selector] ([control-selector] | --target <name>) --keys <VK_A,VK_B> [--keys <VK_C>] [--allow-foreground-input] [--capture-before] [--capture-after] [--artifact-directory <path>] [--all] [--all-windows] [--json]

Window selectors:
  --window-title <pattern>
  --window-process <pattern>
  --window-pid <id>
  --window-class <pattern>
  --window-handle <value>
  --window-active

Control selectors:
  --class <pattern>
  --text-pattern <pattern>
  --value-pattern <pattern>
  --id <value>
  --handle <value>
  --automation-id <pattern>
  --control-type <pattern>
  --framework-id <pattern>
  --enabled
  --disabled
  --focusable
  --not-focusable
  --background-click
  --background-text
  --background-keys
  --foreground-fallback
  --uia
  --include-uia
  --ensure-foreground
  --allow-foreground-input
  --action-probe
  --target <name>
  --expected-value <value>
  --contains
  --capture-before
  --capture-after
  --artifact-directory <path>

Examples:
  desktopmanager control list --window-process notepad --json
  desktopmanager control diagnose --window-title "*Codex*" --uia --ensure-foreground --sample-limit 5 --json
  desktopmanager control diagnose --window-title "*Codex*" --uia --ensure-foreground --sample-limit 5 --action-probe --json
  desktopmanager control diagnose --window-process msedge --uia --ensure-foreground --sample-limit 5 --json
  desktopmanager control diagnose --window-title "Codex" --target codex-sidebar-toggle --sample-limit 5 --json
  desktopmanager control exists --window-active --uia --control-type Button --text-pattern "Hide sidebar" --enabled --focusable --ensure-foreground
  desktopmanager control assert-value --window-process msedge --target edge-address --expected-value "https://evotec.xyz" --contains
  desktopmanager control list --window-process msedge --uia --background-click --json
  desktopmanager control list --window-process msedge --uia --foreground-fallback --json
  desktopmanager control list --window-title "Codex" --target codex-sidebar-toggle --json
  desktopmanager control exists --window-title "Codex" --target codex-sidebar-toggle --json
  desktopmanager control wait --window-title "Codex" --target codex-sidebar-toggle --timeout-ms 1000 --interval-ms 100 --json
  desktopmanager control wait --window-active --uia --control-type Button --text-pattern "Show sidebar" --enabled --ensure-foreground --timeout-ms 3000
  desktopmanager control list --window-active --uia --control-type Edit --json
  desktopmanager control click --window-process msedge --target edge-address
  desktopmanager control send-keys --window-title "Codex" --uia --control-type Button --text-pattern "Hide sidebar" --enabled --focusable --ensure-foreground --allow-foreground-input --keys VK_SPACE
  desktopmanager control set-text --window-active --class RichEditD2DPT --text "Hello world"
  desktopmanager control click --window-process notepad --class Edit
  desktopmanager control set-text --window-process notepad --class Edit --text "Hello world"
  desktopmanager control send-keys --window-process notepad --class Edit --keys VK_CONTROL,VK_A
""";
    }

    public static string GetWorkflowHelp() {
        return """
Workflow commands:
  desktopmanager workflow prepare-coding [--layout <name>] [focus-selector] [--capture-before] [--capture-after] [--artifact-directory <path>] [--json]
  desktopmanager workflow prepare-screen-sharing [--layout <name>] [focus-selector] [--capture-before] [--capture-after] [--artifact-directory <path>] [--json]
  desktopmanager workflow clean-up-distractions [--capture-before] [--capture-after] [--artifact-directory <path>] [--json]

Focus selectors:
  --title <pattern>
  --process <pattern>
  --class <pattern>
  --pid <id>
  --handle <value>
  --active
  --include-empty
  --capture-before
  --capture-after
  --artifact-directory <path>

Examples:
  desktopmanager workflow prepare-coding --layout coding
  desktopmanager workflow prepare-coding --process code --capture-after --json
  desktopmanager workflow prepare-screen-sharing --layout meeting --process msedge --capture-before --capture-after --json
  desktopmanager workflow clean-up-distractions --capture-after --json
""";
    }

    public static string GetMonitorHelp() {
        return """
Monitor commands:
  desktopmanager monitor list [--connected] [--primary] [--index <value>] [--device-id <value>] [--device-name <value>] [--json]
  desktopmanager monitor brightness [--connected] [--primary] [--index <value>] [--device-id <value>] [--device-name <value>] [--json]
  desktopmanager monitor wallpaper [--connected] [--primary] [--index <value>] [--device-id <value>] [--device-name <value>] [--json]
  desktopmanager monitor set-wallpaper [--wallpaper-path <path> | --url <value>] [--position <center|tile|stretch|fit|fill|span>] [--connected] [--primary] [--index <value>] [--device-id <value>] [--device-name <value>] [--json]
  desktopmanager monitor set-brightness --brightness <value> [--connected] [--primary] [--index <value>] [--device-id <value>] [--device-name <value>] [--json]
  desktopmanager monitor set-position --left <value> --top <value> --right <value> --bottom <value> [--connected] [--primary] [--index <value>] [--device-id <value>] [--device-name <value>] [--json]
  desktopmanager monitor set-resolution --width <value> --height <value> [--orientation <default|degrees90|degrees180|degrees270>] [--connected] [--primary] [--index <value>] [--device-id <value>] [--device-name <value>] [--json]
  desktopmanager monitor set-dpi-scaling --scaling-percent <value> [--connected] [--primary] [--index <value>] [--device-id <value>] [--device-name <value>] [--json]
  desktopmanager monitor set-taskbar [--position <left|top|right|bottom>] [--show|--hide] [--connected] [--primary] [--index <value>] [--device-id <value>] [--device-name <value>] [--json]

Examples:
  desktopmanager monitor list
  desktopmanager monitor list --json
  desktopmanager monitor list --primary
  desktopmanager monitor brightness --primary
  desktopmanager monitor wallpaper --index 1 --json
  desktopmanager monitor set-wallpaper --primary --wallpaper-path C:\Wallpapers\Aurora.jpg --position fill
  desktopmanager monitor set-brightness --primary --brightness 65
  desktopmanager monitor set-position --device-name \\.\DISPLAY2 --left 1920 --top 0 --right 3840 --bottom 1440
  desktopmanager monitor set-resolution --primary --width 2560 --height 1440 --orientation default
  desktopmanager monitor set-dpi-scaling --primary --scaling-percent 150
  desktopmanager monitor set-taskbar --primary --position bottom --show
""";
    }

    public static string GetProcessHelp() {
        return """
Process commands:
  desktopmanager process start <file> [--arguments <text>] [--working-directory <path>] [--wait-for-input-idle-ms <value>] [--wait-for-window-ms <value>] [--wait-for-window-interval-ms <value>] [--window-title <pattern>] [--window-class <pattern>] [--require-window] [--json]
  desktopmanager process start-and-wait <file> [--arguments <text>] [--working-directory <path>] [--wait-for-input-idle-ms <value>] [--launch-wait-for-window-ms <value>] [--launch-wait-for-window-interval-ms <value>] [--launch-window-title <pattern>] [--launch-window-class <pattern>] [--window-title <pattern>] [--window-class <pattern>] [--include-hidden] [--include-empty] [--follow-process-family] [--timeout-ms <value>] [--interval-ms <value>] [--capture-before] [--capture-after] [--artifact-directory <path>] [--all] [--json]

Examples:
  desktopmanager process start notepad.exe --wait-for-input-idle-ms 1000
  desktopmanager process start notepad.exe --wait-for-window-ms 5000
  desktopmanager process start notepad.exe --wait-for-window-ms 5000 --window-title "Untitled - Notepad" --require-window
  desktopmanager process start-and-wait notepad.exe --window-title "*Notepad*" --timeout-ms 5000 --capture-after --json
  desktopmanager process start-and-wait msedge.exe --window-title "*Edge*" --follow-process-family --timeout-ms 10000 --json
  desktopmanager process start code --arguments "." --working-directory C:\Support\GitHub\DesktopManager
""";
    }

    public static string GetScreenshotHelp() {
        return """
Screenshot commands:
  desktopmanager screenshot desktop [--monitor <index>] [--device-id <value>] [--device-name <value>] [--left <value> --top <value> --width <value> --height <value>] [--output <path>] [--json]
  desktopmanager screenshot window [selector] [--target <name>] [--active] [--output <path>] [--json]
  desktopmanager screenshot target <name> [selector] [--active] [--output <path>] [--json]

Examples:
  desktopmanager screenshot desktop
  desktopmanager screenshot desktop --monitor 0 --output .\monitor0.png
  desktopmanager screenshot window --active --output .\active-window.png
  desktopmanager screenshot window --process notepad --output .\notepad.png
  desktopmanager screenshot window --process msedge --target edge-editor-pane --output .\edge-editor-pane.png
  desktopmanager screenshot target edge-editor-pane --process msedge --json
""";
    }

    public static string GetLayoutHelp() {
        return """
Layout commands:
  desktopmanager layout save <name> [--json]
  desktopmanager layout apply <name> [--validate] [--json]
  desktopmanager layout assert <name> [--position-tolerance-px <value>] [--size-tolerance-px <value>] [--ignore-state] [--include-hidden] [--include-empty] [--capture-before] [--capture-after] [--artifact-directory <path>] [--json]
  desktopmanager layout list [--json]

Named layouts are stored under the current user's AppData profile.
""";
    }

    public static string GetSnapshotHelp() {
        return """
Snapshot commands:
  desktopmanager snapshot save <name> [--json]
  desktopmanager snapshot restore <name> [--validate] [--json]
  desktopmanager snapshot list [--json]

Snapshots currently store window layout state only. This command group is designed
to grow into broader desktop state capture later.
""";
    }

    public static string GetDiagnosticHelp() {
        return """
Diagnostic commands:
  desktopmanager diagnostic hosted-session [--artifact <path> | --artifact-directory <path> | --repository-root <path>] [--summary-only] [--json]

Examples:
  desktopmanager diagnostic hosted-session --summary-only
  desktopmanager diagnostic hosted-session --repository-root C:\Support\GitHub\DesktopManager
  desktopmanager diagnostic hosted-session --artifact C:\Support\GitHub\DesktopManager\Artifacts\HostedSessionTyping\sample.json --json

Notes:
  This command reads hosted-session typing diagnostics written under Artifacts\HostedSessionTyping.
  It prefers the companion .summary.txt artifact when one exists and falls back to the .json payload otherwise.
""";
    }

    public static string GetMcpHelp() {
        return """
MCP commands:
  desktopmanager mcp serve [--read-only] [--allow-mutations] [--allow-process <pattern>] [--deny-process <pattern>] [--allow-foreground-input] [--dry-run] [--json]

This command group hosts a stdio MCP server that exposes tools, resources, and prompts.
By default the server is read-only. Use --allow-mutations to enable mutating tools.
Use --allow-process and --deny-process to constrain live desktop mutations to specific process patterns.
Use --allow-foreground-input only when you intentionally want focused foreground fallback
for zero-handle UIA text or key actions. Use --dry-run to preview mutating calls safely.
""";
    }
}
