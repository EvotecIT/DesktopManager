using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DesktopManager.AotSmoke;

/// <summary>Owns offscreen, nonactivating native controls on a dedicated message-pump thread.</summary>
internal sealed class NativeWindowHarness : IDisposable {
    internal const string EditText = "DesktopManager Native AOT read-only value";
    internal const string ButtonText = "Native AOT button";
    private readonly ManualResetEventSlim _stop = new();
    private readonly TaskCompletionSource<bool> _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Thread _thread;

    internal IntPtr Handle { get; private set; }
    internal IntPtr EditHandle { get; private set; }
    internal IntPtr ButtonHandle { get; private set; }

    internal NativeWindowHarness() {
        _thread = new Thread(Pump) { IsBackground = true, Name = "AOT smoke native window" };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        try {
            if (!_ready.Task.Wait(TimeSpan.FromSeconds(5))) { throw new TimeoutException("Owned native harness did not start within five seconds."); }
            _ready.Task.GetAwaiter().GetResult();
        } catch {
            Dispose();
            throw;
        }
    }

    private void Pump() {
        try {
            // WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW. Built-in STATIC needs no managed callback.
            // Kept offscreen and shown with SW_SHOWNOACTIVATE so native UIA proxies can observe it.
            Handle = CreateWindowEx(0x08000080, "STATIC", "DesktopManager AOT smoke owned harness",
                0x80C00000, -32000, -32000, 360, 180, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            CheckHandle(Handle);
            EditHandle = CreateWindowEx(0, "EDIT", EditText, 0x50000080, 10, 10, 300, 25, Handle, new IntPtr(101), IntPtr.Zero, IntPtr.Zero);
            CheckHandle(EditHandle);
            ButtonHandle = CreateWindowEx(0, "BUTTON", ButtonText, 0x50000000, 10, 50, 180, 30, Handle, new IntPtr(102), IntPtr.Zero, IntPtr.Zero);
            CheckHandle(ButtonHandle);
            ShowWindow(Handle, 4); // SW_SHOWNOACTIVATE; never calls SetForegroundWindow or SetFocus.
            _ready.TrySetResult(true);
            while (!_stop.IsSet) {
                while (PeekMessage(out var message, IntPtr.Zero, 0, 0, 1)) {
                    TranslateMessage(ref message);
                    DispatchMessage(ref message);
                }
                _stop.Wait(10);
            }
        } catch (Exception ex) {
            _ready.TrySetException(ex);
        } finally {
            // Must destroy on the creating thread. Parent-process deadline also bounds stuck providers.
            if (Handle != IntPtr.Zero) { DestroyWindow(Handle); }
        }
    }

    public void Dispose() {
        _stop.Set();
        if (!_thread.Join(TimeSpan.FromSeconds(3))) { throw new TimeoutException("Owned harness thread did not stop; worker exit releases its windows."); }
        _stop.Dispose();
    }

    internal void SetOwnedEditText(string text) {
        if (!SetWindowText(EditHandle, text)) { throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not update owned smoke edit."); }
    }

    [DllImport("user32.dll", EntryPoint = "SetWindowTextW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowText(IntPtr window, string text);

    private static void CheckHandle(IntPtr handle) {
        if (handle == IntPtr.Zero) { throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not create owned native harness control."); }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Message {
        internal IntPtr Window;
        internal uint Id;
        internal UIntPtr WParam;
        internal IntPtr LParam;
        internal uint Time;
        internal int X;
        internal int Y;
        internal uint Private;
    }

    [DllImport("user32.dll", EntryPoint = "CreateWindowExW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(uint extendedStyle, string className, string title, uint style,
        int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr parameter);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(IntPtr window);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr window, int command);
    [DllImport("user32.dll", EntryPoint = "PeekMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PeekMessage(out Message message, IntPtr window, uint minimum, uint maximum, uint remove);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TranslateMessage(ref Message message);
    [DllImport("user32.dll", EntryPoint = "DispatchMessageW")]
    private static extern IntPtr DispatchMessage(ref Message message);
}
