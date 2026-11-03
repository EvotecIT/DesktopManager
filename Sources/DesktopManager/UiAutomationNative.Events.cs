#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace DesktopManager;

[GeneratedComInterface]
[Guid("146c3c17-f12e-4e22-8c27-f894b9b79c69")]
internal partial interface IUiaEventHandler {
    [PreserveSig] int HandleAutomationEvent(IntPtr sender, int id);
}

[GeneratedComInterface]
[Guid("40cd37d4-c756-4b0c-8c6f-bddfeeb13b50")]
internal partial interface IUiaPropertyHandler {
    [PreserveSig] int HandlePropertyChangedEvent(IntPtr sender, int id, UiaVariant value);
}

[GeneratedComInterface]
[Guid("e81d1b4e-11c5-42f8-9754-e7036c79f054")]
internal partial interface IUiaStructureHandler {
    [PreserveSig] int HandleStructureChangedEvent(IntPtr sender, int change, IntPtr runtimeId);
}

[GeneratedComClass]
internal sealed partial class UiaEventHandler : IUiaEventHandler {
    private readonly Action _signal;
    internal UiaEventHandler(Action signal) { _signal = signal; }
    public int HandleAutomationEvent(IntPtr sender, int id) { try { _signal(); } catch { } return 0; }
}

[GeneratedComClass]
internal sealed partial class UiaPropertyHandler : IUiaPropertyHandler {
    private readonly Action _signal;
    internal UiaPropertyHandler(Action signal) { _signal = signal; }
    public int HandlePropertyChangedEvent(IntPtr sender, int id, UiaVariant value) { try { _signal(); } catch { } return 0; }
}

[GeneratedComClass]
internal sealed partial class UiaStructureHandler : IUiaStructureHandler {
    private readonly Action _signal;
    internal UiaStructureHandler(Action signal) { _signal = signal; }
    public int HandleStructureChangedEvent(IntPtr sender, int change, IntPtr runtimeId) { try { _signal(); } catch { } return 0; }
}

internal sealed partial class UiAutomationControlService {
    private static unsafe IDisposable? SubscribeNative(UiaElement root, Action signal) {
        UiAutomationStaDispatcher dispatcher = UiAutomationStaDispatcher.Current!;
        IDisposable guard = CreateGuardedEventSignal(signal, out Action guardedSignal);
        if (!dispatcher.TryRetainSubscription()) { guard.Dispose(); return null; }
        IUIAutomation client = UiAutomationNative.Client;
        IUIAutomationElement element = root.Native;
        var cleanup = new List<Action>();
        var text = new UiaEventHandler(guardedSignal);
        var properties = new UiaPropertyHandler(guardedSignal);
        var structure = new UiaStructureHandler(CreateStructureChangedSignal(guardedSignal));
        // Each registration is independent: unsupported providers may still support another event family.
        try {
            client.AddAutomationEventHandler(20015, element, 7, IntPtr.Zero, text);
            cleanup.Add(() => client.RemoveAutomationEventHandler(20015, element, text));
        } catch { }
        try {
            int[] ids = { 30005, 30008, 30010, 30022, 30045, 30047, 30086, 30070, 30079, 30053, 30055 };
            fixed (int* pointer = ids) {
                client.AddPropertyChangedEventHandlerNativeArray(element, 7, IntPtr.Zero, properties, (IntPtr)pointer, ids.Length);
            }
            cleanup.Add(() => client.RemovePropertyChangedEventHandler(element, properties));
        } catch { }
        try {
            client.AddStructureChangedEventHandler(element, 7, IntPtr.Zero, structure);
            cleanup.Add(() => client.RemoveStructureChangedEventHandler(element, structure));
        } catch { }
        if (cleanup.Count != 0) { return new UiAutomationChangeSubscription(cleanup, guard); }
        guard.Dispose();
        dispatcher.ReleaseSubscription();
        return null;
    }
}
#endif
