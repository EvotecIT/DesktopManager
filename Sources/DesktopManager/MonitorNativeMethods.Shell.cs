using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DesktopManager;

public static partial class MonitorNativeMethods
{
    /// <summary>
    /// Sets or retrieves system-wide parameters, including desktop wallpaper.
    /// </summary>
    /// <param name="uiAction">The system-wide parameter to set or query.</param>
    /// <param name="uiParam">Parameter whose usage depends on <paramref name="uiAction"/>.</param>
    /// <param name="pvParam">Parameter whose usage and format depends on <paramref name="uiAction"/>.</param>
    /// <param name="fWinIni">Flags specifying if the user profile is updated.</param>
    /// <returns><c>true</c> if successful; otherwise <c>false</c>.</returns>
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, StringBuilder pvParam, uint fWinIni);

    /// <summary>
    /// Sets or retrieves system-wide parameters, including desktop wallpaper.
    /// </summary>
    /// <param name="uiAction">The system-wide parameter to set or query.</param>
    /// <param name="uiParam">Parameter whose usage depends on <paramref name="uiAction"/>.</param>
    /// <param name="pvParam">Parameter whose usage and format depends on <paramref name="uiAction"/>.</param>
    /// <param name="fWinIni">Flags specifying if the user profile is updated.</param>
    /// <returns><c>true</c> if successful; otherwise <c>false</c>.</returns>
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, string pvParam, uint fWinIni);

    /// <summary>
    /// Action code for setting the desktop wallpaper.
    /// </summary>
    public const uint SPI_SETDESKWALLPAPER = 0x0014;

    /// <summary>
    /// Action code for retrieving the desktop wallpaper.
    /// </summary>
    public const uint SPI_GETDESKWALLPAPER = 0x0073;

    /// <summary>
    /// Writes the new system-wide parameter to the user profile.
    /// </summary>
    public const uint SPIF_UPDATEINIFILE = 0x0001;

    /// <summary>
    /// Broadcasts the WM_SETTINGCHANGE message after updating the user profile.
    /// </summary>
    public const uint SPIF_SENDWININICHANGE = 0x0002;

    /// <summary>
    /// Creates an IShellItem from a file path.
    /// </summary>
    /// <param name="pszPath">Path to the file.</param>
    /// <param name="pbc">Reserved, pass IntPtr.Zero.</param>
    /// <param name="riid">The interface ID of the requested interface.</param>
    /// <param name="ppv">Receives the interface pointer.</param>
    /// <returns>HRESULT indicating success or failure.</returns>
    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int SHCreateItemFromParsingName(string pszPath, IntPtr pbc, ref Guid riid, out IntPtr ppv);

    /// <summary>
    /// Interface for an array of COM objects.
    /// </summary>
    [ComImport, Guid("92CA9DCD-5622-4bba-A805-5E9F541BD8C9"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IObjectArray
    {
        uint GetCount();
        [return: MarshalAs(UnmanagedType.Interface)] object GetAt(uint uiIndex, ref Guid riid);
    }

    /// <summary>
    /// Interface for a mutable collection of COM objects.
    /// </summary>
    [ComImport, Guid("5632b1a4-e38a-400a-928a-d4cd63230295"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IObjectCollection : IObjectArray
    {
        void AddObject([MarshalAs(UnmanagedType.Interface)] object pv);
        void AddFromArray(IObjectArray poaSource);
        void RemoveObjectAt(uint uiIndex);
        void Clear();
    }
}
