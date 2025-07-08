using System;
using System.Runtime.InteropServices;

namespace DesktopManager;

/// <summary>
/// Native device notification related platform invocations.
/// </summary>
public static partial class MonitorNativeMethods {
    /// <summary>Message indicating a device change.</summary>
    public const uint WM_DEVICECHANGE = 0x0219;

    /// <summary>Device arrival event.</summary>
    public const int DBT_DEVICEARRIVAL = 0x8000;

    /// <summary>Device removal event.</summary>
    public const int DBT_DEVICEREMOVECOMPLETE = 0x8004;

    /// <summary>Device interface class type.</summary>
    public const int DBT_DEVTYP_DEVICEINTERFACE = 0x00000005;

    /// <summary>GUID for monitor device interface notifications.</summary>
    public static readonly Guid GUID_DEVINTERFACE_MONITOR = new("E6F07B5F-EE97-4a90-B076-33F57BF4EAA7");

    /// <summary>
    /// Header for device broadcast messages.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DEV_BROADCAST_HDR {
        /// <summary>Structure size.</summary>
        public uint dbch_size;
        /// <summary>Device type.</summary>
        public uint dbch_devicetype;
        /// <summary>Reserved.</summary>
        public uint dbch_reserved;
    }

    /// <summary>
    /// Device interface notification structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DEV_BROADCAST_DEVICEINTERFACE {
        /// <summary>Structure size.</summary>
        public uint dbcc_size;
        /// <summary>Device type.</summary>
        public uint dbcc_devicetype;
        /// <summary>Reserved.</summary>
        public uint dbcc_reserved;
        /// <summary>Class GUID.</summary>
        public Guid dbcc_classguid;
        /// <summary>Device name.</summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string dbcc_name;
    }

    /// <summary>Registers for device notifications.</summary>
    /// <param name="hRecipient">Window handle.</param>
    /// <param name="notificationFilter">Filter structure pointer.</param>
    /// <param name="flags">Notification flags.</param>
    /// <returns>Registration handle.</returns>
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr notificationFilter, uint flags);

    /// <summary>Unregisters device notifications.</summary>
    /// <param name="handle">Registration handle.</param>
    /// <returns>True if successful.</returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterDeviceNotification(IntPtr handle);
}
