using System.Runtime.InteropServices;
using System.Text;

namespace DesktopManager;

internal static partial class DeviceNativeMethods {
    internal const uint DigcfPresent = 0x00000002;
    internal const uint DigcfAllClasses = 0x00000004;
    internal const uint DigcfDeviceInterface = 0x00000010;
    internal const uint DibciNoInstallClass = 0x00000001;
    internal const uint DibciNoDisplayClass = 0x00000002;
    internal const uint SpditCompatibleDriver = 0x00000002;
    internal const uint DifInstallDevice = 0x00000002;
    internal const uint DifPropertyChange = 0x00000012;
    internal const uint DifRegisterDevice = 0x00000019;
    internal const uint DicsEnable = 0x00000001;
    internal const uint DicsDisable = 0x00000002;
    internal const uint DicsPropertyChange = 0x00000003;
    internal const uint DicsFlagGlobal = 0x00000001;
    internal const uint DiNeedRestart = 0x00000080;
    internal const uint DiNeedReboot = 0x00000100;
    internal const uint DiEnumSingleInf = 0x00010000;
    internal const uint DiQuietInstall = 0x00800000;
    internal const uint SpdrpHardwareId = 0x00000001;
    internal const uint DicdGenerateId = 0x00000001;
    internal const uint InfStyleWin4 = 0x00000002;
    internal const uint InfInfoNameIsAbsolute = 2;
    internal const uint SpostPath = 1;
    internal const uint SuoiForceDelete = 0x00000001;
    internal const int ErrorInsufficientBuffer = 122;
    internal const int ErrorNoMoreItems = 259;
    internal const int ErrorNotFound = 1168;
    internal const int SpapiErrorNoSuchDeviceInstance = unchecked((int)0xE000020B);
    internal const int MaxPath = 260;
    internal const int LineLength = 256;

    [StructLayout(LayoutKind.Sequential)]
    internal struct DevPropKey {
        internal Guid FormatId;
        internal uint PropertyId;

        internal DevPropKey(Guid formatId, uint propertyId) {
            FormatId = formatId;
            PropertyId = propertyId;
        }

        public override string ToString() {
            return $"{{{FormatId:D}}}:{PropertyId}";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SpDevInfoData {
        internal uint Size;
        internal Guid ClassGuid;
        internal uint DevInst;
        internal IntPtr Reserved;

        internal static SpDevInfoData Create() {
            return new SpDevInfoData { Size = (uint)Marshal.SizeOf(typeof(SpDevInfoData)) };
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SpDeviceInterfaceData {
        internal uint Size;
        internal Guid InterfaceClassGuid;
        internal uint Flags;
        internal IntPtr Reserved;

        internal static SpDeviceInterfaceData Create() {
            return new SpDeviceInterfaceData { Size = (uint)Marshal.SizeOf(typeof(SpDeviceInterfaceData)) };
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct SpDrvInfoData {
        internal uint Size;
        internal uint DriverType;
        internal IntPtr Reserved;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = LineLength)]
        internal string Description;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = LineLength)]
        internal string ManufacturerName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = LineLength)]
        internal string ProviderName;

        internal System.Runtime.InteropServices.ComTypes.FILETIME DriverDate;
        internal ulong DriverVersion;

        internal static SpDrvInfoData Create() {
            return new SpDrvInfoData { Size = (uint)Marshal.SizeOf(typeof(SpDrvInfoData)) };
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct SpDrvInfoDetailData {
        internal uint Size;
        internal System.Runtime.InteropServices.ComTypes.FILETIME InfDate;
        internal uint CompatibleIdsOffset;
        internal uint CompatibleIdsLength;
        internal IntPtr Reserved;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = LineLength)]
        internal string SectionName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxPath)]
        internal string InfFileName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = LineLength)]
        internal string DriverDescription;

        internal char HardwareId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SpDriverInstallParams {
        internal uint Size;
        internal uint Rank;
        internal uint Flags;
        internal IntPtr PrivateData;
        internal uint Reserved;

        internal static SpDriverInstallParams Create() {
            return new SpDriverInstallParams { Size = (uint)Marshal.SizeOf(typeof(SpDriverInstallParams)) };
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct SpDevInstallParams {
        internal uint Size;
        internal uint Flags;
        internal uint FlagsEx;
        internal IntPtr ParentWindow;
        internal IntPtr InstallMessageHandler;
        internal IntPtr InstallMessageHandlerContext;
        internal IntPtr FileQueue;
        internal UIntPtr ClassInstallReserved;
        internal uint Reserved;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxPath)]
        internal string DriverPath;

        internal static SpDevInstallParams Create() {
            return new SpDevInstallParams { Size = (uint)Marshal.SizeOf(typeof(SpDevInstallParams)) };
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SpClassInstallHeader {
        internal uint Size;
        internal uint InstallFunction;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SpPropChangeParams {
        internal SpClassInstallHeader Header;
        internal uint StateChange;
        internal uint Scope;
        internal uint HardwareProfile;

        internal static SpPropChangeParams Create(uint stateChange) {
            return new SpPropChangeParams {
                Header = new SpClassInstallHeader {
                    Size = (uint)Marshal.SizeOf(typeof(SpClassInstallHeader)),
                    InstallFunction = DifPropertyChange
                },
                StateChange = stateChange,
                Scope = DicsFlagGlobal
            };
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct SpOriginalFileInfo {
        internal uint Size;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxPath)]
        internal string OriginalInfName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxPath)]
        internal string OriginalCatalogName;

        internal static SpOriginalFileInfo Create() {
            return new SpOriginalFileInfo { Size = (uint)Marshal.SizeOf(typeof(SpOriginalFileInfo)) };
        }
    }

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern SafeDeviceInfoSetHandle SetupDiGetClassDevsW(
        IntPtr classGuid,
        string? enumerator,
        IntPtr parentWindow,
        uint flags);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern SafeDeviceInfoSetHandle SetupDiGetClassDevsW(
        ref Guid classGuid,
        string? enumerator,
        IntPtr parentWindow,
        uint flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupDiEnumDeviceInfo(
        SafeDeviceInfoSetHandle deviceInfoSet,
        uint memberIndex,
        ref SpDevInfoData deviceInfoData);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupDiOpenDeviceInfoW(
        SafeDeviceInfoSetHandle deviceInfoSet,
        string deviceInstanceId,
        IntPtr parentWindow,
        uint openFlags,
        ref SpDevInfoData deviceInfoData);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupDiGetDeviceInstanceIdW(
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref SpDevInfoData deviceInfoData,
        StringBuilder? deviceInstanceId,
        uint deviceInstanceIdSize,
        out uint requiredSize);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupDiGetDevicePropertyW(
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref SpDevInfoData deviceInfoData,
        ref DevPropKey propertyKey,
        out uint propertyType,
        IntPtr propertyBuffer,
        uint propertyBufferSize,
        out uint requiredSize,
        uint flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupDiGetDevicePropertyKeys(
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref SpDevInfoData deviceInfoData,
        IntPtr propertyKeyArray,
        uint propertyKeyCount,
        out uint requiredPropertyKeyCount,
        uint flags);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupDiBuildDriverInfoList(
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref SpDevInfoData deviceInfoData,
        uint driverType);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupDiDestroyDriverInfoList(
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref SpDevInfoData deviceInfoData,
        uint driverType);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupDiEnumDriverInfoW(
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref SpDevInfoData deviceInfoData,
        uint driverType,
        uint memberIndex,
        ref SpDrvInfoData driverInfoData);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupDiGetDriverInfoDetailW(
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref SpDevInfoData deviceInfoData,
        ref SpDrvInfoData driverInfoData,
        IntPtr driverInfoDetailData,
        uint driverInfoDetailDataSize,
        out uint requiredSize);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupDiGetDriverInstallParamsW(
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref SpDevInfoData deviceInfoData,
        ref SpDrvInfoData driverInfoData,
        ref SpDriverInstallParams driverInstallParams);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupDiSetClassInstallParamsW(
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref SpDevInfoData deviceInfoData,
        ref SpPropChangeParams classInstallParams,
        uint classInstallParamsSize);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupDiCallClassInstaller(
        uint installFunction,
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref SpDevInfoData deviceInfoData);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupDiGetDeviceInstallParamsW(
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref SpDevInfoData deviceInfoData,
        ref SpDevInstallParams deviceInstallParams);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupDiSetDeviceInstallParamsW(
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref SpDevInfoData deviceInfoData,
        ref SpDevInstallParams deviceInstallParams);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupDiSelectBestCompatDrv(
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref SpDevInfoData deviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupDiEnumDeviceInterfaces(
        SafeDeviceInfoSetHandle deviceInfoSet,
        IntPtr deviceInfoData,
        ref Guid interfaceClassGuid,
        uint memberIndex,
        ref SpDeviceInterfaceData deviceInterfaceData);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupDiGetDeviceInterfaceDetailW(
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref SpDeviceInterfaceData deviceInterfaceData,
        IntPtr deviceInterfaceDetailData,
        uint deviceInterfaceDetailDataSize,
        out uint requiredSize,
        ref SpDevInfoData deviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupDiBuildClassInfoList(
        uint flags,
        [Out] Guid[]? classGuidList,
        uint classGuidListSize,
        out uint requiredSize);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupDiClassNameFromGuidW(
        ref Guid classGuid,
        StringBuilder className,
        uint classNameSize,
        out uint requiredSize);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupDiGetClassDescriptionW(
        ref Guid classGuid,
        StringBuilder classDescription,
        uint classDescriptionSize,
        out uint requiredSize);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupDiGetClassPropertyW(
        ref Guid classGuid,
        ref DevPropKey propertyKey,
        out uint propertyType,
        IntPtr propertyBuffer,
        uint propertyBufferSize,
        out uint requiredSize,
        uint flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupDiSetClassPropertyW(
        ref Guid classGuid,
        ref DevPropKey propertyKey,
        uint propertyType,
        IntPtr propertyBuffer,
        uint propertyBufferSize,
        uint flags);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern SafeDeviceInfoSetHandle SetupDiCreateDeviceInfoList(
        ref Guid classGuid,
        IntPtr parentWindow);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupDiCreateDeviceInfoW(
        SafeDeviceInfoSetHandle deviceInfoSet,
        string deviceName,
        ref Guid classGuid,
        string? deviceDescription,
        IntPtr parentWindow,
        uint creationFlags,
        ref SpDevInfoData deviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupDiSetDeviceRegistryPropertyW(
        SafeDeviceInfoSetHandle deviceInfoSet,
        ref SpDevInfoData deviceInfoData,
        uint property,
        IntPtr propertyBuffer,
        uint propertyBufferSize);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupDiGetINFClassW(
        string infName,
        out Guid classGuid,
        StringBuilder className,
        uint classNameSize,
        out uint requiredSize);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupGetInfFileListW(
        string? directoryPath,
        uint infStyle,
        StringBuilder? returnBuffer,
        uint returnBufferSize,
        out uint requiredSize);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupGetInfDriverStoreLocationW(
        string fileName,
        IntPtr alternatePlatformInfo,
        string? localeName,
        StringBuilder? returnBuffer,
        uint returnBufferSize,
        out uint requiredSize);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupCopyOEMInfW(
        string sourceInfFileName,
        string? sourceMediaLocation,
        uint sourceMediaType,
        uint copyStyle,
        StringBuilder? destinationInfFileName,
        uint destinationInfFileNameSize,
        out uint requiredSize,
        IntPtr destinationInfFileNameComponent);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupUninstallOEMInfW(
        string infFileName,
        uint flags,
        IntPtr reserved);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupGetInfInformationW(
        string infSpec,
        uint searchControl,
        IntPtr returnBuffer,
        uint returnBufferSize,
        out uint requiredSize);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupQueryInfOriginalFileInformationW(
        IntPtr infInformation,
        uint infIndex,
        IntPtr alternatePlatformInfo,
        ref SpOriginalFileInfo originalFileInfo);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern SafeInfHandle SetupOpenInfFileW(
        string fileName,
        string? infClass,
        uint infStyle,
        out uint errorLine);

    [DllImport("setupapi.dll")]
    internal static extern void SetupCloseInfFile(IntPtr infHandle);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetupGetLineTextW(
        IntPtr context,
        SafeInfHandle infHandle,
        string section,
        string key,
        StringBuilder? returnBuffer,
        uint returnBufferSize,
        out uint requiredSize);
}
