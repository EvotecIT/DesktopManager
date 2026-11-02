using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if NET8_0_OR_GREATER
using System.Runtime.InteropServices.Marshalling;
#endif

namespace DesktopManager.Tests;

[TestClass]
public sealed class AudioEndpointWatcherLifetimeTests {
    [TestMethod]
    [TestCategory("ReadOnlyAudio")]
    public void RegisteredCallback_RemainsAliveUntilItIsUnregistered() {
        // Core Audio keeps a borrowed pointer, not an owning COM reference. The
        // registration itself must keep the callback target alive between events.
        WeakReference<AudioEndpointWatcher> registered = Register();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.IsTrue(registered.TryGetTarget(out AudioEndpointWatcher? watcher),
            "Core Audio still has a callback pointer, so its managed target must remain alive.");
        try {
#if NET8_0_OR_GREATER
            using var received = new ManualResetEventSlim();
            AudioEndpointChangedEventArgs? notification = null;
            watcher.Changed += (_, args) => { notification = args; received.Set(); };
            IntPtr callback = GetCallback(watcher);
            try {
                IntPtr entry = Marshal.ReadIntPtr(Marshal.ReadIntPtr(callback), 7 * IntPtr.Size);
                var changed = Marshal.GetDelegateForFunctionPointer<PropertyChanged>(entry);
                Assert.AreEqual(0, changed(callback, "synthetic-monitor-audio", new PropertyKey { FormatId = Guid.NewGuid(), PropertyId = 42 }));
                Assert.IsTrue(received.Wait(TimeSpan.FromSeconds(5)), "The native property callback must reach its subscriber.");
                Assert.AreEqual(AudioEndpointChangeKind.PropertyChanged, notification?.ChangeKind);
                Assert.AreEqual("synthetic-monitor-audio", notification?.DeviceId);
            } finally {
                Marshal.Release(callback);
            }
#endif
        } finally {
            watcher.Dispose();
            watcher.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<AudioEndpointWatcher> Register() => new(new AudioEndpointWatcher());

#if NET8_0_OR_GREATER
    private static unsafe IntPtr GetCallback(AudioEndpointWatcher watcher) =>
        (IntPtr)ComInterfaceMarshaller<IMMNotificationClient>.ConvertToUnmanaged(watcher);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int PropertyChanged(IntPtr callback, [MarshalAs(UnmanagedType.LPWStr)] string deviceId, PropertyKey key);
#endif
}
