using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DesktopManager;

/// <summary>
/// Provides opt-in access to the undocumented Windows global radio manager used by airplane mode.
/// This API is experimental because Microsoft does not document or guarantee the COM contract.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ExperimentalAirplaneModeService {
    private static readonly Guid RadioManagerClassId = new("581333F6-28DB-41BE-BC7A-FF201F12F3F6");
    private static readonly Guid RadioManagerInterfaceId = new("DB3AFBFB-08E6-46C6-AA70-BF9A34C30AB7");
    private const uint ClsctxLocalServer = 0x4;
    private const int RpcEChangedMode = unchecked((int)0x80010106);

    /// <summary>Gets the current global airplane-mode state.</summary>
    /// <returns>The current state reported by the undocumented Windows radio manager.</returns>
    public AirplaneModeState GetState() {
        return Execute(manager => ReadState(manager));
    }

    /// <summary>
    /// Applies an explicit global airplane-mode state and verifies the effective state afterward.
    /// </summary>
    /// <param name="state">The explicit state to apply.</param>
    /// <returns>The effective state reported after the request.</returns>
    public AirplaneModeState SetState(AirplaneModeState state) {
        if (state != AirplaneModeState.Enabled && state != AirplaneModeState.Disabled) {
            throw new ArgumentOutOfRangeException(nameof(state), state, "Airplane mode must be Enabled or Disabled.");
        }

        return Execute(manager => {
            IntPtr vtable = Marshal.ReadIntPtr(manager);
            IntPtr method = Marshal.ReadIntPtr(vtable, IntPtr.Size * 6);
            var setState = Marshal.GetDelegateForFunctionPointer<SetSystemRadioStateDelegate>(method);
            int result = setState(manager, (int)state);
            Marshal.ThrowExceptionForHR(result);

            AirplaneModeState effectiveState = ReadState(manager);
            if (effectiveState != state) {
                throw new InvalidOperationException($"Windows reported airplane mode as {effectiveState} after requesting {state}.");
            }
            return effectiveState;
        });
    }

    private static AirplaneModeState ReadState(IntPtr manager) {
        IntPtr vtable = Marshal.ReadIntPtr(manager);
        IntPtr method = Marshal.ReadIntPtr(vtable, IntPtr.Size * 5);
        var getState = Marshal.GetDelegateForFunctionPointer<GetSystemRadioStateDelegate>(method);
        int result = getState(manager, out int state, out _, out _);
        Marshal.ThrowExceptionForHR(result);
        if (state != (int)AirplaneModeState.Enabled && state != (int)AirplaneModeState.Disabled) {
            throw new InvalidOperationException($"Windows returned an unknown global radio state value: {state}.");
        }
        return (AirplaneModeState)state;
    }

    private static T Execute<T>(Func<IntPtr, T> action) {
        if (action == null) {
            throw new ArgumentNullException(nameof(action));
        }

        int initializeResult = CoInitializeEx(IntPtr.Zero, 0x2);
        bool uninitialize = initializeResult >= 0;
        if (initializeResult < 0 && initializeResult != RpcEChangedMode) {
            Marshal.ThrowExceptionForHR(initializeResult);
        }

        IntPtr manager = IntPtr.Zero;
        try {
            Guid classId = RadioManagerClassId;
            Guid interfaceId = RadioManagerInterfaceId;
            int result = CoCreateInstance(
                ref classId,
                IntPtr.Zero,
                ClsctxLocalServer,
                ref interfaceId,
                out manager);
            Marshal.ThrowExceptionForHR(result);
            if (manager == IntPtr.Zero) {
                throw new InvalidOperationException("Windows did not return a global radio manager instance.");
            }

            return action(manager);
        } finally {
            if (manager != IntPtr.Zero) {
                Marshal.Release(manager);
            }
            if (uninitialize) {
                CoUninitialize();
            }
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetSystemRadioStateDelegate(IntPtr manager, out int state, out int reserved1, out int reserved2);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int SetSystemRadioStateDelegate(IntPtr manager, int state);

    [DllImport("ole32.dll")]
    private static extern int CoInitializeEx(IntPtr reserved, uint coInit);

    [DllImport("ole32.dll")]
    private static extern void CoUninitialize();

    [DllImport("ole32.dll")]
    private static extern int CoCreateInstance(
        ref Guid classId,
        IntPtr outer,
        uint context,
        ref Guid interfaceId,
        out IntPtr instance);
}
