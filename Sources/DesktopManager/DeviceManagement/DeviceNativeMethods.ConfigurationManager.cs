using System.Runtime.InteropServices;
using System.Text;

namespace DesktopManager;

internal static partial class DeviceNativeMethods {
    internal const uint CrSuccess = 0x00000000;
    internal const uint CrNoMoreResourceDescriptors = 0x0000000F;
    internal const uint CrNeedRestart = 0x00000022;
    internal const uint CmLocateDevNodeNormal = 0x00000000;
    internal const uint CmLocateDevNodePhantom = 0x00000001;
    internal const uint CmDisableUiNotOk = 0x00000004;
    internal const uint CmDisablePersist = 0x00000008;
    internal const uint CmDisableAbsolute = 0x00000001;
    internal const uint CmReenumerateSynchronous = 0x00000001;
    internal const uint CmReenumerateAsynchronous = 0x00000004;
    internal const uint CmRemoveUiNotOk = 0x00000001;
    internal const uint CmRemoveNoRestart = 0x00000002;
    internal const uint AllocatedLogConfiguration = 0x00000002;
    internal const uint ResourceTypeAll = 0x00000000;
    internal const uint ResourceTypeMemory = 0x00000001;
    internal const uint ResourceTypeIo = 0x00000002;
    internal const uint ResourceTypeDma = 0x00000003;
    internal const uint ResourceTypeIrq = 0x00000004;
    internal const uint ResourceTypeBusNumber = 0x00000006;
    internal const uint ResourceTypeLargeMemory = 0x00000007;

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    internal static extern uint CM_Locate_DevNodeW(
        out uint deviceInstance,
        string? deviceId,
        uint flags);

    [DllImport("cfgmgr32.dll")]
    internal static extern uint CM_Get_DevNode_Status(
        out uint status,
        out uint problemNumber,
        uint deviceInstance,
        uint flags);

    [DllImport("cfgmgr32.dll")]
    internal static extern uint CM_Enable_DevNode(uint deviceInstance, uint flags);

    [DllImport("cfgmgr32.dll")]
    internal static extern uint CM_Disable_DevNode(uint deviceInstance, uint flags);

    [DllImport("cfgmgr32.dll")]
    internal static extern uint CM_Reenumerate_DevNode(uint deviceInstance, uint flags);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    internal static extern uint CM_Query_And_Remove_SubTreeW(
        uint deviceInstance,
        out uint vetoType,
        StringBuilder vetoName,
        uint vetoNameLength,
        uint flags);

    [DllImport("cfgmgr32.dll")]
    internal static extern uint CM_Get_First_Log_Conf(out IntPtr logConfiguration, uint deviceInstance, uint flags);

    [DllImport("cfgmgr32.dll")]
    internal static extern uint CM_Get_Next_Res_Des(
        out IntPtr nextResourceDescriptor,
        IntPtr resourceDescriptor,
        uint forResource,
        out uint resourceId,
        uint flags);

    [DllImport("cfgmgr32.dll")]
    internal static extern uint CM_Get_Res_Des_Data_Size(
        out uint size,
        IntPtr resourceDescriptor,
        uint flags);

    [DllImport("cfgmgr32.dll")]
    internal static extern uint CM_Get_Res_Des_Data(
        IntPtr resourceDescriptor,
        IntPtr buffer,
        uint bufferLength,
        uint flags);

    [DllImport("cfgmgr32.dll")]
    internal static extern uint CM_Free_Res_Des_Handle(IntPtr resourceDescriptor);

    [DllImport("cfgmgr32.dll")]
    internal static extern uint CM_Free_Log_Conf_Handle(IntPtr logConfiguration);

    [DllImport("cfgmgr32.dll")]
    internal static extern uint CM_MapCrToWin32Err(uint configurationManagerCode, uint defaultError);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    internal static extern uint CM_Get_Device_Interface_List_SizeW(
        out uint length,
        ref Guid interfaceClassGuid,
        string? deviceId,
        uint flags);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    internal static extern uint CM_Get_Device_Interface_ListW(
        ref Guid interfaceClassGuid,
        string? deviceId,
        StringBuilder buffer,
        uint bufferLength,
        uint flags);
}
