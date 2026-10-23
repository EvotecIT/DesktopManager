using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopManager;

public sealed partial class WorkspaceRecoveryService {
    /// <summary>
    /// Retries only missing windows during startup. Successfully restored and ambiguous windows are
    /// never reapplied by later attempts. Cancellation stops subsequent attempts and wallpaper actions run once.
    /// </summary>
    public async Task<WorkspaceRestoreResult> RestoreAsync(WindowLayout layout, WorkspaceRestoreOptions? options = null,
            int maximumAttempts = 12, int retryDelaySeconds = 2, CancellationToken cancellationToken = default) {
        Validate(layout);
        if (maximumAttempts < 1 || maximumAttempts > 30) { throw new ArgumentOutOfRangeException(nameof(maximumAttempts)); }
        if (retryDelaySeconds < 1 || retryDelaySeconds > 10) { throw new ArgumentOutOfRangeException(nameof(retryDelaySeconds)); }
        options ??= new WorkspaceRestoreOptions();
        var result = new WorkspaceRestoreResult();
        var pending = layout.Windows.Select((window, index) => new KeyValuePair<int, WindowPosition>(index, window)).ToList();
        var outcomes = new WorkspaceWindowResult[layout.Windows.Count];
        for (int attempt = 0; attempt < maximumAttempts; attempt++) {
            cancellationToken.ThrowIfCancellationRequested();
            var candidate = new WindowLayout { Windows = pending.Select(item => item.Value).ToList(), Desktops = layout.Desktops };
            WorkspaceRestoreResult current = Restore(candidate, new WorkspaceRestoreOptions {
                RestoreVirtualDesktops = options.RestoreVirtualDesktops,
                RestoreApplicationPins = options.RestoreApplicationPins,
                ReopenExplorerFolders = options.ReopenExplorerFolders,
                RestoreWallpapers = attempt == 0 && options.RestoreWallpapers
            });
            result.Wallpapers.AddRange(current.Wallpapers);
            foreach (string warning in current.Warnings) { if (!result.Warnings.Contains(warning)) { result.Warnings.Add(warning); } }
            for (int index = 0; index < pending.Count; index++) { outcomes[pending[index].Key] = current.Windows[index]; }
            pending = pending.Where((item, index) => current.Windows[index].MatchStatus == WindowLayoutMatchStatus.Missing).ToList();
            if (pending.Count == 0 || attempt == maximumAttempts - 1) { break; }
            await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), cancellationToken);
        }
        result.Windows.AddRange(outcomes);
        result.Warnings.AddRange(layout.CaptureWarnings);
        return result;
    }

    /// <summary>
    /// Saves a periodic snapshot only when capture is complete, non-empty, and at least as populated
    /// as the previous snapshot. Startup hosts must suspend this until recovery has completed successfully.
    /// Returns false without replacing the previous file when the snapshot is incomplete.
    /// </summary>
    public bool TrySaveAutomatic(string path, WorkspaceCaptureOptions? options = null) {
        WindowLayout layout = Capture(options);
        if (layout.Windows.Count == 0 || layout.CaptureWarnings.Count != 0) { return false; }
        if (File.Exists(path)) {
            WindowLayout previous = Read(path);
            if (layout.Windows.Count < previous.Windows.Count) { return false; }
            if (previous.Desktops.Count > 0 && layout.Desktops.Count == 0) { return false; }
        }
        Write(path, layout);
        return true;
    }
}
