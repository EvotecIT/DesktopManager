using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace DesktopManager;

[SupportedOSPlatform("windows")]
internal sealed class AudioSessionNativeBackend : IAudioSessionBackend {
    public IReadOnlyList<IAudioApplicationSession> OpenSessions() {
        var sessions = new List<IAudioApplicationSession>();
        IMMDevice? device = null;
        object? managerObject = null;
        object? enumeratorObject = null;
        try {
            device = CoreAudioSessionsNative.OpenDefaultDevice(AudioDataFlow.Render, AudioRole.Multimedia);
            int result = CoreAudioSessionsNative.Activate(device, CoreAudioSessionsNative.SessionManagerInterfaceId, out IntPtr pointer);
            managerObject = CoreAudioSessionsNative.TakeReference(result, pointer);
            var manager = (IAudioSessionManager2)managerObject;
            result = manager.GetSessionEnumerator(out pointer);
            enumeratorObject = CoreAudioSessionsNative.TakeReference(result, pointer);
            var enumerator = (IAudioSessionEnumerator)enumeratorObject;
            Marshal.ThrowExceptionForHR(enumerator.GetCount(out int count));
            for (int index = 0; index < count; index++) {
                result = enumerator.GetSession(index, out pointer);
                if (result < 0) {
                    CoreAudioSessionsNative.ReleasePointer(pointer);
                    continue;
                }
                object control = CoreAudioSessionsNative.TakeReference(pointer);
                try {
                    sessions.Add(new NativeApplicationSession(control));
                } catch (Exception ex) when (ex is COMException || ex is InvalidCastException) {
                    NativeComActivation.Release(control);
                } catch {
                    NativeComActivation.Release(control);
                    throw;
                }
            }
            return sessions;
        } catch {
            foreach (IAudioApplicationSession session in sessions) {
                session.Dispose();
            }
            throw;
        } finally {
            NativeComActivation.Release(enumeratorObject);
            NativeComActivation.Release(managerObject);
            NativeComActivation.Release(device);
        }
    }

    public void Dispose() { }

    private sealed class NativeApplicationSession : IAudioApplicationSession {
        private object? _owner;
        private readonly IAudioSessionControl2 _control;
        private readonly ISimpleAudioVolume _volume;
        private readonly IAudioMeterInformation? _meter;

        internal NativeApplicationSession(object owner) {
            // Concrete casts root generated interface metadata for Native AOT and query one owned wrapper.
            _control = (IAudioSessionControl2)owner;
            _volume = (ISimpleAudioVolume)owner;
            _meter = owner as IAudioMeterInformation;
            _owner = owner;
        }

        public bool IsExpired {
            get {
                Marshal.ThrowExceptionForHR(_control.GetState(out int state));
                return state == 2;
            }
        }

        public AudioApplicationSessionState? Read() {
            Marshal.ThrowExceptionForHR(_control.GetState(out int state));
            if (state == 2) {
                return null;
            }
            Marshal.ThrowExceptionForHR(_control.GetProcessId(out uint processId));
            int systemResult = _control.IsSystemSoundsSession();
            Marshal.ThrowExceptionForHR(systemResult);
            bool isSystem = systemResult == 0;
            Marshal.ThrowExceptionForHR(_control.GetDisplayName(out string displayName));
            Marshal.ThrowExceptionForHR(_volume.GetMasterVolume(out float volume));
            Marshal.ThrowExceptionForHR(_volume.GetMute(out bool muted));
            string name = isSystem ? "system" : GetProcessName(checked((int)processId));
            return new AudioApplicationSessionState((int)processId, name, displayName, volume * 100f,
                muted, GetPeak(), state == 1, isSystem);
        }

        public float GetPeak() {
            if (_meter == null) {
                return 0f;
            }
            Marshal.ThrowExceptionForHR(_meter.GetPeakValue(out float peak));
            return CoreAudioSessionsNative.NormalizePeak(peak);
        }

        public void SetVolume(float scalar) {
            Guid context = Guid.Empty;
            Marshal.ThrowExceptionForHR(_volume.SetMasterVolume(scalar, ref context));
        }

        public void SetMute(bool muted) {
            Guid context = Guid.Empty;
            Marshal.ThrowExceptionForHR(_volume.SetMute(muted, ref context));
        }

        public void Dispose() {
            object? owner = _owner;
            _owner = null;
            NativeComActivation.Release(owner);
        }

        private static string GetProcessName(int processId) {
            // Resolve on each refresh: a persistent PID cache would misidentify a reused process ID.
            try {
                using Process process = Process.GetProcessById(processId);
                return process.ProcessName;
            } catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is Win32Exception) {
                return string.Empty;
            }
        }
    }
}
