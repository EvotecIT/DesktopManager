using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace DesktopManager;

internal static class NativeWifiMethods {
    internal const uint ErrorSuccess = 0;
    internal const uint ClientVersionLonghorn = 2;
    internal const int MaxNameLength = 256;
    internal const uint ProfileGroupPolicy = 0x00000001;
    internal const uint ProfileUser = 0x00000002;
    internal const uint NotificationSourceNone = 0x00000000;
    internal const uint NotificationSourceAcm = 0x00000008;
    internal const uint NotificationAcmConnectionComplete = 10;
    internal const uint NotificationAcmConnectionAttemptFail = 11;
    internal const int ConnectionNotificationProfileNameOffset = 4;
    internal const int ConnectionNotificationReasonCodeOffset = 560;
    internal const int ConnectionNotificationMinimumSize = ConnectionNotificationReasonCodeOffset + sizeof(uint);

    [DllImport("wlanapi.dll")]
    internal static extern uint WlanOpenHandle(
        uint clientVersion,
        IntPtr reserved,
        out uint negotiatedVersion,
        out SafeWlanClientHandle clientHandle);

    [DllImport("wlanapi.dll")]
    internal static extern uint WlanCloseHandle(IntPtr clientHandle, IntPtr reserved);

    [DllImport("wlanapi.dll")]
    internal static extern uint WlanEnumInterfaces(
        SafeWlanClientHandle clientHandle,
        IntPtr reserved,
        out IntPtr interfaceList);

    [DllImport("wlanapi.dll")]
    internal static extern uint WlanGetProfileList(
        SafeWlanClientHandle clientHandle,
        ref Guid interfaceId,
        IntPtr reserved,
        out IntPtr profileList);

    [DllImport("wlanapi.dll")]
    internal static extern uint WlanConnect(
        SafeWlanClientHandle clientHandle,
        ref Guid interfaceId,
        ref WlanConnectionParameters connectionParameters,
        IntPtr reserved);

    [DllImport("wlanapi.dll")]
    internal static extern uint WlanRegisterNotification(
        SafeWlanClientHandle clientHandle,
        uint notificationSource,
        [MarshalAs(UnmanagedType.Bool)] bool ignoreDuplicates,
        WlanNotificationCallback? callback,
        IntPtr callbackContext,
        IntPtr reserved,
        IntPtr previousNotificationSource);

    [DllImport("wlanapi.dll")]
    internal static extern void WlanFreeMemory(IntPtr memory);

    [DllImport("wlanapi.dll", CharSet = CharSet.Unicode)]
    internal static extern uint WlanReasonCodeToString(
        uint reasonCode,
        uint bufferSize,
        StringBuilder buffer,
        IntPtr reserved);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    internal delegate void WlanNotificationCallback(ref WlanNotificationData notification, IntPtr context);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct WlanInterfaceInfo {
        internal Guid InterfaceId;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxNameLength)]
        internal string? Description;

        internal WlanInterfaceState State;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct WlanProfileInfo {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxNameLength)]
        internal string? ProfileName;

        internal uint Flags;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct WlanConnectionParameters {
        internal WlanConnectionMode ConnectionMode;

        [MarshalAs(UnmanagedType.LPWStr)]
        internal string ProfileName;

        internal IntPtr Dot11Ssid;
        internal IntPtr DesiredBssidList;
        internal Dot11BssType BssType;
        internal uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WlanNotificationData {
        internal uint NotificationSource;
        internal uint NotificationCode;
        internal Guid InterfaceId;
        internal uint DataSize;
        internal IntPtr Data;
    }

    internal enum WlanInterfaceState {
        NotReady = 0,
        Connected = 1,
        AdHocNetworkFormed = 2,
        Disconnecting = 3,
        Disconnected = 4,
        Associating = 5,
        Discovering = 6,
        Authenticating = 7
    }

    internal enum WlanConnectionMode {
        Profile = 0
    }

    internal enum Dot11BssType {
        Any = 3
    }
}

internal sealed class SafeWlanClientHandle : SafeHandleZeroOrMinusOneIsInvalid {
    public SafeWlanClientHandle()
        : base(true) {
    }

    protected override bool ReleaseHandle() {
        return NativeWifiMethods.WlanCloseHandle(handle, IntPtr.Zero) == NativeWifiMethods.ErrorSuccess;
    }
}
