#if NET8_0_OR_GREATER
using System.Runtime.InteropServices.Marshalling;
#endif

namespace DesktopManager;

/// <summary>Resolves a retained wallpaper Shell array in the current COM apartment.</summary>
#if NET8_0_OR_GREATER
[GeneratedComInterface]
[Guid("C03F6A43-65A4-9818-987E-E0B810D2A6F2")]
#else
[ComImport, Guid("C03F6A43-65A4-9818-987E-E0B810D2A6F2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
internal partial interface IAgileWallpaperReference {
    void Resolve(ref Guid interfaceId, out IntPtr pointer);
}
