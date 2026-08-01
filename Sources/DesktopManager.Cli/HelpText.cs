namespace DesktopManager.Cli;

internal static partial class HelpText {
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
  workstation Capture and restore cohesive workstation profiles
  audio      Inspect and configure Core Audio endpoints
  system     Inspect power/session state and perform explicit system actions
  personalization Capture, apply, and restore personalization state
  taskbar    Inspect and configure taskbars
  radio      Inspect and configure supported radios; opt in to experimental airplane mode
  wifi       List saved Wi-Fi profiles and connect an exact saved profile without scanning
  device     Inspect and manage exact Plug and Play device instances
  driver     Inspect and manage Windows Driver Store packages and device drivers
  virtual-desktop Use the supported Windows virtual-desktop window operations
  process    Start desktop applications
  screenshot Capture the desktop, monitors, or windows
  target     Save and resolve reusable window-relative targets
  visual     Save visual baselines and inspect screenshot text
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
  desktopmanager visual save editor-default --process notepad --client-area
  desktopmanager visual read-text --process notepad --client-area
  desktopmanager control-target save edge-address --control-type Edit --background-text --uia
  desktopmanager window move --title "Visual Studio Code" --x 0 --y 0 --width 1920 --height 1400
  desktopmanager monitor list --json
  desktopmanager monitor set-resolution --primary --width 2560 --height 1440 --orientation default
  desktopmanager workstation save --name coding
  desktopmanager audio list --active
  desktopmanager system power
  desktopmanager radio list
  desktopmanager wifi profiles
  desktopmanager device list --present
  desktopmanager driver list
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
  desktopmanager help workstation
  desktopmanager help audio
  desktopmanager help system
  desktopmanager help personalization
  desktopmanager help taskbar
  desktopmanager help radio
  desktopmanager help wifi
  desktopmanager help device
  desktopmanager help driver
  desktopmanager help virtual-desktop
  desktopmanager help process
  desktopmanager help screenshot
  desktopmanager help target
  desktopmanager help visual
  desktopmanager help control-target
  desktopmanager help layout
  desktopmanager help snapshot
  desktopmanager help diagnostic
  desktopmanager help workflow
  desktopmanager help mcp
""";
    }

    public static string GetWorkstationHelp() {
        return """
Workstation commands:
  desktopmanager workstation save --name <name>
  desktopmanager workstation list [--json]
  desktopmanager workstation show --name <name>
  desktopmanager workstation apply --name <name> [--allow-missing-monitors] [--skip-displays] [--skip-audio] [--skip-personalization] [--include-machine-policies] [--skip-taskbars] [--no-rollback]
  desktopmanager workstation delete --name <name>
""";
    }

    public static string GetAudioHelp() {
        return """
Audio commands:
  desktopmanager audio list [--flow <render|capture|all>] [--active] [--json]
  desktopmanager audio set-default --id <endpoint-id> [--role <console|multimedia|communications>]
  desktopmanager audio set-volume --id <endpoint-id> --volume <0-100>
  desktopmanager audio set-mute --id <endpoint-id> (--on | --off)
""";
    }

    public static string GetSystemHelp() {
        return """
System commands:
  desktopmanager system power
  desktopmanager system session
  desktopmanager system lock
  desktopmanager system keep-awake --seconds <1-86400> [--display] [--away-mode]
  desktopmanager system suspend [--hibernate] [--force] --confirm
  desktopmanager system sign-out [--force] --confirm
""";
    }

    public static string GetPersonalizationHelp() {
        return """
Personalization commands:
  desktopmanager personalization capture --name <name>
  desktopmanager personalization list [--json]
  desktopmanager personalization show --name <name>
  desktopmanager personalization restore --name <name> [--skip-machine-policies]
  desktopmanager personalization apply --file <settings.json>
  desktopmanager personalization delete --name <name>
""";
    }

    public static string GetTaskbarHelp() {
        return """
Taskbar commands:
  desktopmanager taskbar list
  desktopmanager taskbar set --monitor-index <index> [--show | --hide] [--position <left|top|right|bottom>]
  desktopmanager taskbar set-auto-hide (--on | --off)
""";
    }

    public static string GetRadioHelp() {
        return """
Radio commands:
  desktopmanager radio list
  desktopmanager radio set --kind <wifi|bluetooth|mobilebroadband|fm> --state <on|off> [--name <radio-name>]
  desktopmanager radio airplane get --experimental
  desktopmanager radio airplane set --state <enabled|disabled> --experimental
""";
    }

    public static string GetWifiHelp() {
        return """
Wi-Fi commands:
  desktopmanager wifi interfaces
  desktopmanager wifi profiles [--interface-id <guid>]
  desktopmanager wifi connect --profile <saved-profile-name> [--interface-id <guid>] [--timeout-ms <value>]

The profile commands use the Windows Native Wi-Fi saved-profile APIs. They do not scan nearby networks, return BSSIDs, expose profile XML or credentials, or query location-sensitive current-connection details. The connection timeout defaults to 30000 milliseconds and can be at most 2147483647 milliseconds. After a connection timeout, a later command in the same process waits for the earlier Windows attempt to finish before starting another one. If Windows never reports completion, the library releases the retained notification handle after two minutes and requires restarting the hosting process before another connection attempt.
""";
    }

    public static string GetVirtualDesktopHelp() {
        return """
Virtual desktop commands:
  desktopmanager virtual-desktop current --handle <window-handle>
  desktopmanager virtual-desktop id --handle <window-handle>
  desktopmanager virtual-desktop move --handle <window-handle> --desktop-id <guid>
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
  desktopmanager window place [selector] --placement <restore|maximize|left-half|right-half|exact-rectangle> [--monitor <index> | --monitor-target <current|top-left|top-right|bottom-left|bottom-right>] [--x <value>] [--y <value>] [--width <value>] [--height <value>] [--capture-before] [--capture-after] [--artifact-directory <path>] [--verify] [--verify-tolerance-px <value>] [--all] [--json]
  desktopmanager window click [selector] ((--x <value> --y <value> | --x-ratio <value> --y-ratio <value>) | --target <name> | --visual-baseline <name> | --ocr-text <text>) [--button <left|right>] [--activate] [--client-area] [--ocr-target <name>] [--ocr-contains] [--ocr-language <tag>] [--baseline-max-average-difference <value>] [--baseline-difference-threshold <value>] [--baseline-scan-step <value>] [--capture-before] [--capture-after] [--artifact-directory <path>] [--verify] [--verify-tolerance-px <value>] [--all] [--json]
  desktopmanager window drag [selector] (((--start-x <value> --start-y <value>) | (--start-x-ratio <value> --start-y-ratio <value>)) ((--end-x <value> --end-y <value>) | (--end-x-ratio <value> --end-y-ratio <value>)) | (--start-target <name> --end-target <name>) | (--start-visual-baseline <name> --end-visual-baseline <name>) | (--start-ocr-text <text> --end-ocr-text <text>)) [--button <left|right>] [--step-delay-ms <value>] [--activate] [--client-area] [--start-ocr-target <name>] [--end-ocr-target <name>] [--ocr-contains] [--ocr-language <tag>] [--baseline-max-average-difference <value>] [--baseline-difference-threshold <value>] [--baseline-scan-step <value>] [--capture-before] [--capture-after] [--artifact-directory <path>] [--verify] [--verify-tolerance-px <value>] [--all] [--json]
  desktopmanager window scroll [selector] ((--x <value> --y <value> | --x-ratio <value> --y-ratio <value>) | --target <name> | --visual-baseline <name> | --ocr-text <text>) --delta <value> [--activate] [--client-area] [--ocr-target <name>] [--ocr-contains] [--ocr-language <tag>] [--baseline-max-average-difference <value>] [--baseline-difference-threshold <value>] [--baseline-scan-step <value>] [--capture-before] [--capture-after] [--artifact-directory <path>] [--verify] [--verify-tolerance-px <value>] [--all] [--json]
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
  desktopmanager window wait-visual-change [selector] [--target <name>] [--client-area] [--timeout-ms <value>] [--interval-ms <value>] [--minimum-changed-ratio <value>] [--difference-threshold <value>] [--json]

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
  --wait-visual-change
  --visual-target <name>
  --visual-client-area
  --visual-timeout-ms <value>
  --visual-interval-ms <value>
  --minimum-changed-ratio <value>
  --difference-threshold <value>

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
  desktopmanager window click --process notepad --visual-baseline apply-button --client-area --baseline-max-average-difference 10
  desktopmanager window click --process notepad --ocr-text Apply --client-area
  desktopmanager window click --process msedge --ocr-text "Sign in" --ocr-contains --client-area
  desktopmanager window drag --process notepad --start-x 200 --start-y 200 --end-x 500 --end-y 200 --client-area
  desktopmanager window drag --process notepad --start-x-ratio 0.2 --start-y-ratio 0.2 --end-x-ratio 0.6 --end-y-ratio 0.2 --client-area
  desktopmanager window drag --process notepad --start-visual-baseline drag-source --end-visual-baseline drop-target --client-area
  desktopmanager window drag --process notepad --start-ocr-text "Drag Source" --end-ocr-text "Drop Target" --ocr-contains --client-area
  desktopmanager window wait-visual-change --process msedge --client-area --timeout-ms 5000
  desktopmanager window wait-visual-change --process msedge --target edge-editor-pane --minimum-changed-ratio 0.005 --timeout-ms 5000 --json
  desktopmanager window drag --process notepad --start-target editor-center --end-target editor-right
  desktopmanager window scroll --process notepad --x 200 --y 200 --delta -120 --client-area
  desktopmanager window scroll --process notepad --x-ratio 0.5 --y-ratio 0.5 --delta -120 --client-area
  desktopmanager window scroll --process notepad --target editor-center --delta -120
  desktopmanager window scroll --process notepad --visual-baseline editor-clean --delta -120 --client-area
  desktopmanager window scroll --process notepad --ocr-text "Page 1" --ocr-contains --client-area
  desktopmanager window type --active --text "Hello world"
  desktopmanager window type --process Devolutions.RemoteDesktopManager --text "safe probe" --foreground-input
  desktopmanager window type --process Devolutions.RemoteDesktopManager --text "safe probe" --physical-keys
  desktopmanager window type --process Devolutions.RemoteDesktopManager --text "safe probe" --hosted-session
  desktopmanager window type --process Devolutions.RemoteDesktopManager --text "Write-Host 'hi'`nGet-Date" --script --foreground-input --line-delay-ms 20
  desktopmanager window move --title "Visual Studio Code" --x 0 --y 0 --width 1920 --height 1400 --activate
  desktopmanager window move --title "Visual Studio Code" --x 0 --y 0 --width 1920 --height 1400 --verify --verify-tolerance-px 12
  desktopmanager window place --title "Remote Desktop Manager*" --placement maximize --monitor 1 --verify
  desktopmanager window place --title "Visual Studio Code*" --placement exact-rectangle --x -3840 --y 19 --width 1920 --height 2088 --verify
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
  --wait-visual-change waits for real pixel change after a mutation and can observe the whole window, the client area, or a saved visual target region.
  --visual-baseline resolves a saved visual region and clicks the center of the best live match before the action continues.
  The same baseline tuning flags also apply to visual-baseline drag and scroll targeting.
  --ocr-text resolves a visible text label through Windows OCR and clicks its best live match without requiring pre-saved targets.
  The same OCR anchor model also applies to drag and scroll targeting through `--start-ocr-text` / `--end-ocr-text` and `--ocr-text`.
""";
    }

    public static string GetDesktopHelp() {
        return """
Desktop commands:
  desktopmanager desktop background-color [--json]
  desktopmanager desktop set-background-color --color <decimal|0xRRGGBB|#RRGGBB> [--json]
  desktopmanager desktop wallpaper-position [--json]
  desktopmanager desktop set-wallpaper-position --position <center|tile|stretch|fit|fill|span> [--json]
  desktopmanager desktop slideshow [--json]
  desktopmanager desktop start-slideshow --image <path> [--image <path>...] [--shuffle] [--slideshow-tick <milliseconds>] [--json]
  desktopmanager desktop set-slideshow-options [--shuffle|--no-shuffle] [--slideshow-tick <milliseconds>] [--json]
  desktopmanager desktop stop-slideshow [--json]
  desktopmanager desktop advance-slideshow --direction <forward|backward> [--json]

Examples:
  desktopmanager desktop background-color
  desktopmanager desktop set-background-color --color 0x102040
  desktopmanager desktop wallpaper-position --json
  desktopmanager desktop set-wallpaper-position --position fill
  desktopmanager desktop slideshow --json
  desktopmanager desktop start-slideshow --image C:\Wallpapers\img1.jpg --image C:\Wallpapers\img2.jpg --shuffle --slideshow-tick 300000
  desktopmanager desktop set-slideshow-options --no-shuffle --slideshow-tick 600000
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

    public static string GetVisualHelp() {
        return """
Visual commands:
  desktopmanager visual save <name> [selector] [--target <name>] [--client-area] [--description <text>] [--json]
  desktopmanager visual get <name> [--json]
  desktopmanager visual list [--json]
  desktopmanager visual assert <name> [selector] [--target <name>] [--client-area] [--max-changed-ratio <value>] [--difference-threshold <value>] [--json]
  desktopmanager visual resolve <name> [selector] [--client-area] [--max-average-difference <value>] [--difference-threshold <value>] [--scan-step <value>] [--json]
  desktopmanager visual read-text [selector] [--target <name>] [--client-area] [--language <tag>] [--json]
  desktopmanager visual resolve-text <text> [selector] [--target <name>] [--client-area] [--contains] [--language <tag>] [--json]

Selectors:
  --title <pattern>
  --process <pattern>
  --class <pattern>
  --pid <id>
  --handle <value>
  --active

Examples:
  desktopmanager visual save editor-default --process notepad --client-area
  desktopmanager visual get editor-default --json
  desktopmanager visual list
  desktopmanager visual assert editor-default --process notepad --client-area --max-changed-ratio 0.005
  desktopmanager visual resolve editor-default --process notepad --client-area --max-average-difference 10 --json
  desktopmanager visual read-text --process notepad --client-area --json
  desktopmanager visual resolve-text APPLY --process notepad --client-area
  desktopmanager visual assert edge-editor-pane --process msedge --target edge-editor-pane --difference-threshold 18 --json

Notes:
  Visual baselines store a PNG image plus metadata under the current user's AppData profile.
  Saved baselines can reuse a named window target, the whole window, or the client area.
  Visual assertions compare sampled pixels, so use tighter ratios for stable UI and looser ratios for animated surfaces.
  Visual resolve searches a whole window or client area for the saved baseline image and returns the best sampled match coordinates.
  Visual read-text runs Windows OCR over the selected capture and returns line and word bounds relative to that capture.
  Visual resolve-text returns the best OCR match coordinates so agents can click visible UI by label without pre-saved targets.
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
  desktopmanager control observe [window-selector] [control-selector] [--expected-text <value>] [--max-text-length <value>] [--include-text-ranges] [--realize-virtualized-item] [--all] [--all-windows] [--json]
  desktopmanager control wait-observation [window-selector] [control-selector] [semantic-condition] [--timeout-ms <value>] [--interval-ms <value>] [--json]
  desktopmanager control diagnose [window-selector] ([control-selector] | --target <name>) [--sample-limit <value>] [--action-probe] [--all-windows] [--json]
  desktopmanager control exists [window-selector] ([control-selector] | --target <name>) [--all] [--all-windows] [--json]
  desktopmanager control assert-value [window-selector] ([control-selector] | --target <name>) --expected-value <value> [--contains] [--all] [--all-windows] [--json]
  desktopmanager control wait [window-selector] ([control-selector] | --target <name>) [--timeout-ms <value>] [--interval-ms <value>] [--all] [--all-windows] [--json]
  desktopmanager control click [window-selector] ([control-selector] | --target <name>) [--button <left|right>] [--capture-before] [--capture-after] [--artifact-directory <path>] [--all] [--all-windows] [--json]
  desktopmanager control set-text [window-selector] ([control-selector] | --target <name>) --text <value> [--allow-foreground-input] [--capture-before] [--capture-after] [--artifact-directory <path>] [--all] [--all-windows] [--json]
  desktopmanager control edit-text [window-selector] [control-selector] --text <value> [--mode <ReplaceDocument|ReplaceSelection|InsertAtCaret>] [--expected-fingerprint <sha256>] [--expected-edit-context-fingerprint <sha256>] [--allow-foreground-input] [--no-verify] [--json]
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
  --wait-visual-change
  --visual-target <name>
  --visual-client-area
  --visual-timeout-ms <value>
  --visual-interval-ms <value>
  --minimum-changed-ratio <value>
  --difference-threshold <value>

Semantic observation options:
  --expected-text <value>
  --ignore-case
  --max-text-length <value>
  --include-text-ranges
  --realize-virtualized-item
  --focused / --not-focused
  --checked / --unchecked
  --selected / --not-selected
  --expand-collapse-state <value>
  --complete-text / --truncated-text
  --minimum-range-value <value>
  --maximum-range-value <value>

Text edit options:
  --mode <ReplaceDocument|ReplaceSelection|InsertAtCaret>
  --expected-fingerprint <sha256>
  --expected-edit-context-fingerprint <sha256>
  --no-verify

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
  desktopmanager control observe --window-active --uia --control-type Document --expected-text "ready" --include-text-ranges --json
  desktopmanager control wait-observation --window-process myapp --uia --automation-id status --expected-text "Complete" --timeout-ms 10000 --json
  desktopmanager control click --window-process msedge --target edge-address
  desktopmanager control send-keys --window-title "Codex" --uia --control-type Button --text-pattern "Hide sidebar" --enabled --focusable --ensure-foreground --allow-foreground-input --keys VK_SPACE
  desktopmanager control set-text --window-active --class RichEditD2DPT --text "Hello world"
  desktopmanager control click --window-process notepad --class Edit
  desktopmanager control set-text --window-process notepad --class Edit --text "Hello world"
  desktopmanager control edit-text --window-active --uia --control-type Document --mode ReplaceSelection --text "replacement" --expected-fingerprint <sha256> --allow-foreground-input --json
  desktopmanager control send-keys --window-process notepad --class Edit --keys VK_CONTROL,VK_A

Notes:
  observe returns one provider-neutral shape for native and UI Automation controls: stable session identity, supported patterns, semantic state, bounded text, selection/caret ranges, and a complete-content fingerprint when available.
  Password controls fail closed: identity and capability metadata may be returned, but text, selection text, and fingerprints are suppressed.
  edit-text uses provider-safe setters first. Selection/caret edits require exact complete ranges; content and edit-context fingerprints prevent stale documents or moved ranges from being mutated.
  wait-observation subscribes to relevant UI Automation changes when supported and retains bounded polling as a compatibility fallback.
  Use --allow-foreground-input only when the control target is a zero-handle UIA surface that cannot be updated safely in the background.
  Saved control targets preserve capability hints such as background-text and UIA selection filters, which makes repeat automation less brittle.
  --wait-visual-change can confirm that a control mutation changed the visible parent window even when structural verification is weak.
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
  desktopmanager monitor advanced-color [--connected] [--primary] [--index <value>] [--device-id <value>] [--device-name <value>] [--json]
  desktopmanager monitor hdr [--connected] [--primary] [--index <value>] [--device-id <value>] [--device-name <value>] [--json]
  desktopmanager monitor wallpaper [--connected] [--primary] [--index <value>] [--device-id <value>] [--device-name <value>] [--json]
  desktopmanager monitor set-wallpaper [--wallpaper-path <path> | --url <value>] [--position <center|tile|stretch|fit|fill|span>] [--connected] [--primary] [--index <value>] [--device-id <value>] [--device-name <value>] [--json]
  desktopmanager monitor set-brightness --brightness <value> [--connected] [--primary] [--index <value>] [--device-id <value>] [--device-name <value>] [--json]
  desktopmanager monitor set-hdr --enable|--disable [--connected] [--primary] [--index <value>] [--device-id <value>] [--device-name <value>] [--json]
  desktopmanager monitor set-position --left <value> --top <value> [--connected] [--primary] [--index <value>] [--device-id <value>] [--device-name <value>] [--json]
  desktopmanager monitor set-resolution --width <value> --height <value> [--orientation <default|degrees90|degrees180|degrees270>] [--connected] [--primary] [--index <value>] [--device-id <value>] [--device-name <value>] [--json]
  desktopmanager monitor set-taskbar [--position <left|top|right|bottom>] [--show|--hide] [--connected] [--primary] [--index <value>] [--device-id <value>] [--device-name <value>] [--json]

Examples:
  desktopmanager monitor list
  desktopmanager monitor list --json
  desktopmanager monitor list --primary
  desktopmanager monitor brightness --primary
  desktopmanager monitor hdr --primary --json
  desktopmanager monitor wallpaper --index 1 --json
  desktopmanager monitor set-wallpaper --primary --wallpaper-path C:\Wallpapers\Aurora.jpg --position fill
  desktopmanager monitor set-brightness --primary --brightness 65
  desktopmanager monitor set-hdr --primary --enable
  desktopmanager monitor set-position --device-name \\.\DISPLAY2 --left 1920 --top 0
  desktopmanager monitor set-resolution --primary --width 2560 --height 1440 --orientation default
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
  desktopmanager mcp serve [--read-only] [--allow-mutations] [--allow-system-settings] [--allow-experimental] [--allow-process <pattern>] [--deny-process <pattern>] [--allow-foreground-input] [--dry-run] [--json]

This command group hosts a stdio MCP server that exposes tools, resources, and prompts.
System-wide settings mutations require both --allow-mutations and --allow-system-settings.
The undocumented global airplane-mode tools are hidden behind --allow-experimental.
By default the server is read-only. Use --allow-mutations to enable mutating tools.
Use --allow-process and --deny-process to constrain live desktop mutations to specific process patterns.
Use --allow-foreground-input only when you intentionally want focused foreground fallback
for zero-handle UIA text or key actions. Use --dry-run to preview mutating calls safely.
""";
    }
}
