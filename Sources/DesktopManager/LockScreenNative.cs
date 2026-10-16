using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopManager;

/// <summary>
/// Calls the WinRT ABI directly so netstandard consumers do not need a managed WinRT projection.
/// Interface IDs and slots follow the Windows SDK windows.system.userprofile.idl, asyncinfo.idl,
/// and windows.foundation.idl. All owned pointers are released in their originating MTA.
/// </summary>
internal static class LockScreenNative {
    private static readonly Guid LockScreenStaticsId = new Guid("3EE9D3AD-B607-40AE-B426-7631D9821269");
    private static readonly Guid RandomAccessStreamId = new Guid("905A0FE1-BC53-11DF-8C49-001E4FC686DA");
    private static readonly Guid StreamId = new Guid("0000000C-0000-0000-C000-000000000046");
    private static readonly Guid AsyncInfoId = new Guid("00000036-0000-0000-C000-000000000046");
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(30);

    internal static void SetImage(string path) => RunInMta(() => {
        IntPtr factory = IntPtr.Zero;
        IntPtr stream = IntPtr.Zero;
        IntPtr randomAccess = IntPtr.Zero;
        IntPtr action = IntPtr.Zero;
        try {
            factory = Activate();
            // STGM_READ | STGM_SHARE_DENY_WRITE; no file creation or mutation.
            Check(SHCreateStreamOnFileEx(path, 0x20, 0, false, IntPtr.Zero, out stream));
            Guid iid = RandomAccessStreamId;
            Check(CreateRandomAccessStreamOverStream(stream, 0, ref iid, out randomAccess));
            Check(Method<SetStream>(factory, 9)(factory, randomAccess, out action));
            Complete(action);
            return true;
        } finally {
            Release(action);
            Release(randomAccess);
            Release(stream);
            Release(factory);
        }
    });

    internal static string GetImage() => RunInMta(() => {
        IntPtr factory = IntPtr.Zero;
        IntPtr randomAccess = IntPtr.Zero;
        IntPtr stream = IntPtr.Zero;
        string? path = null;
        try {
            factory = Activate();
            Check(Method<GetObject>(factory, 7)(factory, out randomAccess));
            Guid iid = StreamId;
            Check(CreateStreamOverRandomAccessStream(randomAccess, ref iid, out stream));
            path = Path.GetTempFileName();
            using (var file = new FileStream(path, FileMode.Create, FileAccess.Write)) {
                CopyToFile(stream, file);
            }
            return path;
        } catch {
            if (path != null) {
                File.Delete(path);
            }
            throw;
        } finally {
            Release(stream);
            Release(randomAccess);
            Release(factory);
        }
    });

    private static T RunInMta<T>(Func<T> operation) {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            throw new PlatformNotSupportedException("Lock screen images are only supported on Windows.");
        }
        // Keep an STA host (including Windows PowerShell) out of synchronous WinRT waits.
        return Task.Run(() => {
            Check(RoInitialize(1)); // RO_INIT_MULTITHREADED; S_FALSE is also balanced.
            try {
                return operation();
            } finally {
                RoUninitialize();
            }
        }).GetAwaiter().GetResult();
    }

    private static IntPtr Activate() {
        const string className = "Windows.System.UserProfile.LockScreen";
        Check(WindowsCreateString(className, className.Length, out IntPtr name));
        try {
            Guid iid = LockScreenStaticsId;
            Check(RoGetActivationFactory(name, ref iid, out IntPtr factory));
            return factory;
        } finally {
            WindowsDeleteString(name);
        }
    }

    private static void Complete(IntPtr action) {
        Guid iid = AsyncInfoId;
        IntPtr info;
#if NET10_0_OR_GREATER
        Check(Marshal.QueryInterface(action, in iid, out info));
#else
        Check(Marshal.QueryInterface(action, ref iid, out info));
#endif
        bool operationCompleted = false;
        try {
            var getStatus = Method<GetInt32>(info, 7);
            WaitForCompletion(() => {
                Check(getStatus(info, out int status));
                operationCompleted = status != 0;
                return status;
            }, () => Check(Method<Invoke>(info, 9)(info)), OperationTimeout);

            // GetResults carries the native error/cancellation HRESULT; returning from
            // SetImageStreamAsync alone does not mean the image was accepted.
            Check(Method<Invoke>(action, 8)(action));
        } finally {
            if (operationCompleted) {
                try {
                    Check(Method<Invoke>(info, 10)(info));
                } catch (Exception ex) {
                    // Preserve GetResults errors if close also fails.
                    DesktopManagerDiagnostics.Report($"Closing a lock screen operation failed: {ex.Message}");
                }
            } else {
                // IAsyncInfo::Close is invalid until the canceled operation reaches a terminal state.
                DesktopManagerDiagnostics.Report("A timed-out lock screen operation was canceled; its IAsyncInfo handle was released without Close.");
            }
            Release(info);
        }
    }

    internal static void WaitForCompletion(Func<int> getStatus, Action cancel, TimeSpan timeout) {
        var stopwatch = Stopwatch.StartNew();
        int status;
        do {
            status = getStatus();
            if (status != 0) {
                return;
            }
            if (stopwatch.Elapsed >= timeout) {
                try {
                    cancel();
                } catch (Exception ex) {
                    DesktopManagerDiagnostics.Report($"Canceling a timed-out lock screen operation failed: {ex.Message}");
                }
                throw new TimeoutException("The lock screen operation did not complete before the configured timeout.");
            }
            Thread.Sleep(10);
        } while (true);
    }

    private static void CopyToFile(IntPtr stream, FileStream file) {
        const int bufferSize = 64 * 1024;
        IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
        try {
            byte[] bytes = new byte[bufferSize];
            var read = Method<ReadStream>(stream, 3);
            while (true) {
                Check(read(stream, buffer, bufferSize, out uint count));
                if (count == 0) {
                    return;
                }
                Marshal.Copy(buffer, bytes, 0, checked((int)count));
                file.Write(bytes, 0, (int)count);
            }
        } finally {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static T Method<T>(IntPtr instance, int slot) where T : Delegate {
        IntPtr address = Marshal.ReadIntPtr(Marshal.ReadIntPtr(instance), slot * IntPtr.Size);
        return Marshal.GetDelegateForFunctionPointer<T>(address);
    }

    private static void Check(int hr) {
        if (hr < 0) {
            Marshal.ThrowExceptionForHR(hr);
        }
    }

    private static void Release(IntPtr value) {
        if (value != IntPtr.Zero) {
            Marshal.Release(value);
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetObject(IntPtr self, out IntPtr value);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int SetStream(IntPtr self, IntPtr value, out IntPtr action);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetInt32(IntPtr self, out int value);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int Invoke(IntPtr self);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int ReadStream(IntPtr self, IntPtr buffer, uint count, out uint read);

    [DllImport("combase.dll", ExactSpelling = true)]
    private static extern int RoInitialize(uint type);
    [DllImport("combase.dll", ExactSpelling = true)]
    private static extern void RoUninitialize();
    [DllImport("combase.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int WindowsCreateString(string source, int length, out IntPtr value);
    [DllImport("combase.dll", ExactSpelling = true)]
    private static extern int WindowsDeleteString(IntPtr value);
    [DllImport("combase.dll", ExactSpelling = true)]
    private static extern int RoGetActivationFactory(IntPtr name, ref Guid iid, out IntPtr factory);
    [DllImport("shlwapi.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int SHCreateStreamOnFileEx(string path, uint mode, uint attributes,
        [MarshalAs(UnmanagedType.Bool)] bool create, IntPtr template, out IntPtr stream);
    [DllImport("shcore.dll", ExactSpelling = true)]
    private static extern int CreateRandomAccessStreamOverStream(IntPtr stream, uint options, ref Guid iid, out IntPtr value);
    [DllImport("shcore.dll", ExactSpelling = true)]
    private static extern int CreateStreamOverRandomAccessStream(IntPtr stream, ref Guid iid, out IntPtr value);
}
