using Microsoft.Win32.SafeHandles;

namespace DesktopManager;

internal sealed class SafeDeviceInfoSetHandle : SafeHandleZeroOrMinusOneIsInvalid {
    private SafeDeviceInfoSetHandle() : base(ownsHandle: true) {
    }

    protected override bool ReleaseHandle() {
        return DeviceNativeMethods.SetupDiDestroyDeviceInfoList(handle);
    }
}

internal sealed class SafeInfHandle : SafeHandleZeroOrMinusOneIsInvalid {
    private SafeInfHandle() : base(ownsHandle: true) {
    }

    protected override bool ReleaseHandle() {
        DeviceNativeMethods.SetupCloseInfFile(handle);
        return true;
    }
}
