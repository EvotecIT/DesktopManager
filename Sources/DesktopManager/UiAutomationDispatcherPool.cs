using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopManager;

/// <summary>Bounds native worker growth while isolating providers belonging to different processes.</summary>
internal sealed class UiAutomationDispatcherPool : IDisposable {
    internal const int MaximumWorkers = 16;
    private readonly object _sync = new();
    private readonly Dictionary<uint, Entry> _entries = new();
    private bool _disposed;

    internal T Invoke<T>(uint processId, Func<UiAutomationControlService, T> operation, int timeoutMilliseconds,
        Action<T>? abandonedResultHandler = null) {
        Entry entry;
        lock (_sync) {
            if (_disposed) { throw new ObjectDisposedException(nameof(UiAutomationDispatcherPool)); }
            if (!_entries.TryGetValue(processId, out entry!)) {
                if (_entries.Count >= MaximumWorkers) {
                    KeyValuePair<uint, Entry>? idle = _entries
                        .Where(pair => pair.Value.Users == 0 && pair.Value.Dispatcher.IsIdle &&
                            pair.Value.Dispatcher.SubscriptionCount == 0)
                        .Select(pair => (KeyValuePair<uint, Entry>?)pair).FirstOrDefault();
                    if (!idle.HasValue) {
                        throw new TimeoutException("The UI Automation worker limit is reached; the operation was not started.");
                    }
                    _entries.Remove(idle.Value.Key);
                    idle.Value.Value.Dispatcher.Dispose();
                }
                entry = new Entry(new UiAutomationStaDispatcher());
                _entries.Add(processId, entry);
            }
            entry.Users++;
        }
        try {
            return entry.Dispatcher.Invoke(operation, timeoutMilliseconds, abandonedResultHandler);
        } finally {
            lock (_sync) { entry.Users--; }
        }
    }

    internal IReadOnlyList<UiAutomationProviderHealth> GetHealth() {
        lock (_sync) {
            return _entries.Select(pair => new UiAutomationProviderHealth(pair.Key,
                pair.Value.Dispatcher.PendingCount, !pair.Value.Dispatcher.IsIdle,
                pair.Value.Dispatcher.IsBlocked)).ToArray();
        }
    }

    public void Dispose() {
        UiAutomationStaDispatcher[] workers;
        lock (_sync) {
            if (_disposed) { return; }
            _disposed = true;
            workers = _entries.Values.Select(entry => entry.Dispatcher).ToArray();
            _entries.Clear();
        }
        foreach (UiAutomationStaDispatcher worker in workers) { worker.Dispose(); }
    }

    private sealed class Entry {
        internal Entry(UiAutomationStaDispatcher dispatcher) { Dispatcher = dispatcher; }
        internal readonly UiAutomationStaDispatcher Dispatcher;
        internal int Users;
    }
}
