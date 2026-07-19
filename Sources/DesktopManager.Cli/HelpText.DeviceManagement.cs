namespace DesktopManager.Cli;

internal static partial class HelpText {
    public static string GetDeviceHelp() {
        return """
desktopmanager device - inspect and manage Plug and Play device instances

Read-only commands:
  device list [--instance-id <id>] [--device-id <id>] [--class <name>] [--class-guid <guid>]
              [--enumerator <name>] [--present|--non-present] [--problem|--no-problem]
              [--problem-code <code>] [--relations] [--stack] [--resources] [--interfaces] [--properties]
  device get --instance-id <id>
  device drivers --instance-id <id>
  device classes
  device containers [list filters]

State-changing commands (all require --confirm):
  device enable --instance-id <id> --confirm
  device disable --instance-id <id> [--temporary] [--force --expert] --confirm
  device restart --instance-id <id> --confirm
  device remove --instance-id <id> [--device-only | --expert] --confirm
  device scan [--instance-id <id>] [--asynchronous] --confirm
  device set-hardware-ids --instance-id ROOT\... --hardware-id <id> [--hardware-id <id>] --expert --confirm
  device set-class-filters --class-guid <guid> --kind Upper|Lower [--service <name>] --expert --confirm

Identifiers are exact; wildcards are rejected. Commands report RebootRequired but never reboot Windows.
""";
    }

    public static string GetDriverHelp() {
        return """
desktopmanager driver - inspect and manage Windows Driver Store packages

Read-only commands:
  driver list [--published-name oem42.inf] [--class-guid <guid>] [--files] [--devices]

State-changing commands (all require --confirm):
  driver stage --inf <path> --confirm
  driver install --inf <path> [--force --expert] --confirm
  driver update --inf <path> --hardware-id <id> [--force --expert] --confirm
  driver delete --published-name oem42.inf [--uninstall-devices --expert] [--force --expert] --confirm
  driver export --published-name oem42.inf --destination <path> [--overwrite --expert] --confirm
  driver rollback --instance-id <id> --confirm
  driver create-root --inf <path> --hardware-id <id> --expert --confirm

For delete, --force affects direct Driver Store deletion. When --uninstall-devices is used,
Windows reassigns affected devices before removing the package, so --force is redundant.
Published INF names and hardware identifiers are exact; wildcards are rejected. No command reboots Windows.
""";
    }
}
