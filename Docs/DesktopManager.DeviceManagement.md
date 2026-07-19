# Device and Driver Management

DesktopManager provides one native Windows device-management owner for the workflows commonly handled by DevCon and PnPUtil. `DeviceManagementService` contains the validation and operation contract; the CLI, PowerShell module, and read-only MCP tools are adapters over that service.

The implementation uses documented Configuration Manager, SetupAPI, and NewDev entrypoints. It does not launch, parse, or depend on `devcon.exe` or `pnputil.exe`.

The complete surface requires Windows 10 version 1703 or later. Current .NET 8 and .NET 10 builds target Windows 10 version 2004 (build 19041) or later.

## What is covered

Read-only inventory includes:

- present and non-present Plug and Play device instances
- exact instance, hardware ID, compatible ID, class, class GUID, enumerator, presence, and problem filters
- device status and problem codes, capabilities, installed-driver metadata, hardware IDs, and compatible IDs
- parent, child, sibling, bus, removal, ejection, and power relations
- effective driver stacks, allocated resources, registered interfaces, and unified device properties
- compatible driver nodes with Windows rank, flags, INF, version, date, hardware ID, and compatible IDs
- device setup classes, default services, and upper/lower class filters
- Windows device containers
- third-party Driver Store packages, source metadata, package files, and devices currently using each package

State-changing workflows include:

- enable, disable, restart, uninstall, subtree removal, and hardware scanning
- stage, install, update, delete, uninstall, export, and roll back driver packages
- create ROOT-enumerated devices from an INF package
- replace hardware identifiers on ROOT-enumerated devices
- replace upper or lower device-class filter chains after validating every filter service

State-changing calls return `DesktopDeviceOperationResult` after validation and native preflight succeed. The result reports success separately from `Changed`: that value is `true` or `false` when DesktopManager observes the state transition and `null` when the native API does not prove the final state. It also carries native Configuration Manager or Win32 error information, identifies affected devices when known, and reports whether Windows requires a restart; `RebootRequired` is `null` when that native result cannot be read. Invalid targets, failed preflight, and unavailable native entrypoints remain exceptions. DesktopManager never restarts Windows automatically.

## DevCon and PnPUtil fit

DesktopManager can replace the normal local-machine DevCon and PnPUtil operator workflows through one typed API and two command surfaces. It is not a command-line syntax emulator and does not preserve their wildcard-heavy or automatically disruptive behavior.

| Existing workflow | DesktopManager surface |
| ----------------- | ---------------------- |
| `pnputil /enum-devices`, `/enum-devicetree`, `/enum-interfaces`, `/enum-classes`, `/enum-containers` | `desktopmanager device ...`, `Get-DesktopDevice*` |
| `pnputil /enum-drivers` with device/file details | `desktopmanager driver list`, `Get-DesktopDriverPackage` |
| `pnputil /enable-device`, `/disable-device`, `/restart-device`, `/remove-device`, `/scan-devices` | `desktopmanager device ...`, lifecycle PowerShell cmdlets |
| `pnputil /add-driver`, `/delete-driver`, `/export-driver` | `desktopmanager driver ...`, driver-package PowerShell cmdlets |
| DevCon `find`, `findall`, `hwids`, `status`, `resources`, `stack`, `drivernodes`, `classes`, `listclass` | typed device queries and detail switches |
| DevCon `enable`, `disable`, `restart`, `remove`, `rescan`, `update`, `install` | guarded device and driver mutations |
| DevCon `dp_add`, `dp_delete`, `dp_enum` | Driver Store package commands |
| DevCon `sethwid`, `classfilter` | high-confirmation ROOT hardware-ID and class-filter cmdlets |

Intentional boundaries:

- mutations use exact instance IDs, hardware IDs, and published names; wildcards are rejected
- all mutations require elevation
- CLI mutations require `--confirm`; force, subtree, ROOT-device, hardware-ID, and class-filter paths also require `--expert`
- PowerShell mutations support `-WhatIf` and `-Confirm`; disruptive and expert cmdlets use `ConfirmImpact.High`
- MCP exposes inventory only; it cannot enable, disable, remove, install, update, or delete devices or drivers
- reboot commands are omitted; callers decide when and how to restart after checking `RebootRequired`
- hardware-ID changes are limited to ROOT-enumerated devices
- ROOT-device creation installs the selected INF on the newly registered devnode only; it does not run the all-matching hardware-ID update path
- class-filter commands replace an explicit ordered chain; they do not emulate DevCon's incremental `+`, `-`, `!`, or `@` filter syntax
- package deletion maps `--uninstall-devices`/`-UninstallDevices` to native package uninstall, which reassigns affected devices before removing the package; `--force`/`-Force` only changes direct Driver Store deletion and is accepted as redundant when combined with uninstall
- management is local-machine only

## CLI

Read-only examples:

```text
desktopmanager device list --present --problem
desktopmanager device get --instance-id "PCI\VEN_1234&DEV_5678\1"
desktopmanager device drivers --instance-id "PCI\VEN_1234&DEV_5678\1"
desktopmanager device classes
desktopmanager driver list --published-name oem42.inf --files --devices
```

Mutation examples:

```text
desktopmanager device disable --instance-id "ROOT\EXAMPLE\0000" --confirm
desktopmanager device scan --confirm
desktopmanager driver stage --inf C:\Drivers\example.inf --confirm
desktopmanager driver update --inf C:\Drivers\example.inf --hardware-id "ROOT\EXAMPLE" --confirm
desktopmanager driver delete --published-name oem42.inf --uninstall-devices --expert --confirm
```

Use `desktopmanager help device` and `desktopmanager help driver` for the complete command list.

## PowerShell

```powershell
Get-DesktopDevice -Present -Problem
Get-DesktopDeviceDriver -InstanceId 'PCI\VEN_1234&DEV_5678\1'
Get-DesktopDriverPackage -PublishedInfName 'oem42.inf' -IncludeFiles -IncludeDevices

Disable-DesktopDevice -InstanceId 'ROOT\EXAMPLE\0000' -Confirm
Add-DesktopDriverPackage -InfPath 'C:\Drivers\example.inf' -Confirm
Update-DesktopDeviceDriver -InfPath 'C:\Drivers\example.inf' -HardwareId 'ROOT\EXAMPLE' -Confirm
Remove-DesktopDriverPackage -PublishedInfName 'oem42.inf' -UninstallDevices -Confirm
```

Use `-WhatIf` to validate targeting and confirmation behavior without reaching the native mutation.

## C#

```csharp
var devices = new DeviceManagementService().GetDevices(new DesktopDeviceQuery {
    Present = true,
    HasProblem = true,
    IncludeRelations = true
});
```

Call mutations only from an elevated process. The shared service validates exact targets and INF paths before invoking native Windows APIs.

## MCP

The read-only MCP catalog provides:

- `list_devices`
- `get_device`
- `list_device_drivers`
- `list_driver_packages`
- `list_device_classes`
- `list_device_containers`

These tools remain read-only even when the MCP host starts with mutation permissions. Device and driver mutation stays on the explicit CLI and PowerShell operator surfaces.

## Native API ownership

The shared implementation uses:

- Configuration Manager for devnode status, enable/disable, re-enumeration, subtree removal, interfaces, and allocated resources
- SetupAPI for device/property enumeration, compatible driver nodes, setup classes, INF inspection, Driver Store staging/deletion, ROOT-device registration, and class properties
- NewDev for driver/package installation, hardware-ID updates, device uninstall, package uninstall, and rollback

These are supported Windows API boundaries. See Microsoft's [Configuration Manager functions](https://learn.microsoft.com/windows-hardware/drivers/install/configuration-manager-functions), [SetupAPI functions](https://learn.microsoft.com/windows-hardware/drivers/install/setupapi), and [NewDev functions](https://learn.microsoft.com/windows-hardware/drivers/install/newdev-functions).
