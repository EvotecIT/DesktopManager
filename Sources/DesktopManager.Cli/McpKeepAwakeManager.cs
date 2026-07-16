using System;
using System.Threading;

namespace DesktopManager.Cli;

/// <summary>
/// Owns the single keep-awake lease associated with an MCP server process.
/// </summary>
internal static class McpKeepAwakeManager {
    private static readonly object Sync = new();
    private static KeepAwakeLease? _lease;
    private static Timer? _expirationTimer;
    private static DateTimeOffset? _expiresAt;
    private static long _generation;

    public static object Configure(bool enabled, KeepAwakeOptions options, int? durationSeconds) {
        if (enabled && durationSeconds.HasValue && (durationSeconds.Value <= 0 || durationSeconds.Value > 86400)) {
            throw new CommandLineException("Property 'durationSeconds' must be between 1 and 86400.");
        }

        lock (Sync) {
            StopCore();
            if (!enabled) {
                return CreateStateModel();
            }

            _lease = new SystemPowerService().CreateKeepAwakeLease(options);
            if (durationSeconds.HasValue) {
                _expiresAt = DateTimeOffset.UtcNow.AddSeconds(durationSeconds.Value);
                _expirationTimer = new Timer(HandleExpiration, _generation, TimeSpan.FromSeconds(durationSeconds.Value), Timeout.InfiniteTimeSpan);
            }

            return CreateStateModel();
        }
    }

    internal static object GetState() {
        lock (Sync) {
            return CreateStateModel();
        }
    }

    private static void HandleExpiration(object? state) {
        if (state is not long generation) {
            return;
        }

        lock (Sync) {
            if (generation != _generation) {
                return;
            }
            StopCore();
        }
    }

    private static object CreateStateModel() {
        return new {
            enabled = _lease != null,
            options = _lease?.Options ?? 0,
            expiresAt = _expiresAt
        };
    }

    private static void StopCore() {
        _generation++;
        _expirationTimer?.Dispose();
        _expirationTimer = null;
        _lease?.Dispose();
        _lease = null;
        _expiresAt = null;
    }
}
