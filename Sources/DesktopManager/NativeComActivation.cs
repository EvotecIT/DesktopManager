using System;
using System.Runtime.InteropServices;
#if NET8_0_OR_GREATER
using System.Runtime.InteropServices.Marshalling;
#endif

namespace DesktopManager;

/// <summary>
/// Activates and releases COM objects whose interfaces are declared for both interop models:
/// source-generated COM on .NET 8+ (trimming and Native AOT safe) and built-in COM on net472/netstandard2.0.
/// </summary>
internal static partial class NativeComActivation {
#if NET8_0_OR_GREATER
    // CLSCTX_SERVER, matching what built-in activation through Activator.CreateInstance requests.
    private const uint ClassContextServer = 0x15;
    private static readonly Guid UnknownInterfaceId = new("00000000-0000-0000-C000-000000000046");
    private static readonly StrategyBasedComWrappers ComWrappers = new();

    /// <summary>
    /// Creates a COM object as a unique wrapper so <see cref="Release"/> frees it deterministically.
    /// Casting the result to a generated interface queries the native object, as built-in COM does.
    /// </summary>
    internal static object CreateInstance(Guid classId) {
        Guid interfaceId = UnknownInterfaceId;
        Marshal.ThrowExceptionForHR(CoCreateInstance(in classId, IntPtr.Zero, ClassContextServer, in interfaceId, out IntPtr unknown));
        try {
            return ComWrappers.GetOrCreateObjectForComInstance(unknown, CreateObjectFlags.UniqueInstance);
        } finally {
            Marshal.Release(unknown);
        }
    }

    /// <summary>Releases a wrapper created by <see cref="CreateInstance"/>.</summary>
    /// <remarks>Shared wrappers returned through interface marshalling are released by their finalizer.</remarks>
    internal static void Release(object? value) {
        if (value is ComObject comObject) {
            comObject.FinalRelease();
            // The .NET 8 ComObject finalizer releases the instance again even after FinalRelease.
            GC.SuppressFinalize(comObject);
        }
    }

    [LibraryImport("ole32.dll")]
    private static partial int CoCreateInstance(in Guid classId, IntPtr outer, uint classContext, in Guid interfaceId, out IntPtr instance);
#else
    internal static object CreateInstance(Guid classId) {
        Type type = Type.GetTypeFromCLSID(classId, true)
            ?? throw new InvalidOperationException($"Windows did not expose COM class {classId}.");
        return Activator.CreateInstance(type)
            ?? throw new InvalidOperationException($"Windows did not create COM class {classId}.");
    }

    internal static void Release(object? value) {
        if (value != null && Marshal.IsComObject(value)) {
            Marshal.ReleaseComObject(value);
        }
    }
#endif
}
