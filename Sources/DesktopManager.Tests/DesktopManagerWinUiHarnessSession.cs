using System.Diagnostics;
using System.Text.Json;
using System.Threading;

namespace DesktopManager.Tests;

internal sealed class DesktopManagerWinUiHarnessSession : IDisposable {
    private const int LaunchTimeoutMilliseconds = 30000;
    private readonly string _sessionDirectory;
    private readonly string _statusFilePath;
    private readonly int _launcherProcessId;
    private readonly int _resolvedProcessId;
    private readonly IntPtr _windowHandle;

    private DesktopManagerWinUiHarnessSession(string sessionDirectory, string statusFilePath, string windowTitle, int launcherProcessId, int resolvedProcessId, IntPtr windowHandle) {
        _sessionDirectory = sessionDirectory;
        _statusFilePath = statusFilePath;
        WindowTitle = windowTitle;
        _launcherProcessId = launcherProcessId;
        _resolvedProcessId = resolvedProcessId;
        _windowHandle = windowHandle;
    }

    public string WindowTitle { get; }

    public int ProcessId => _resolvedProcessId;

    public IntPtr WindowHandle => _windowHandle;

    public static DesktopManagerWinUiHarnessSession Start(string scenario, string initialText = "seed") {
        if (string.IsNullOrWhiteSpace(scenario)) {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(scenario));
        }

        string windowTitle = "DesktopManager-WinUiHarness-" + scenario + "-" + Guid.NewGuid().ToString("N");
        string sessionDirectory = Path.Combine(Path.GetTempPath(), "DesktopManager.Tests", "WinUiHarness", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sessionDirectory);
        string statusFilePath = Path.Combine(sessionDirectory, "status.json");

        DesktopAutomationService automation = new();
        DesktopProcessLaunchInfo launch = automation.LaunchProcess(new DesktopProcessStartOptions {
            FilePath = RequireExecutablePath(),
            Arguments = BuildArguments(windowTitle, initialText, statusFilePath),
            WaitForWindowMilliseconds = LaunchTimeoutMilliseconds,
            WaitForWindowIntervalMilliseconds = 200,
            RequireWindow = true
        });

        int launcherProcessId = launch.ProcessId;
        int resolvedProcessId = launch.ResolvedProcessId ?? launch.ProcessId;
        if (resolvedProcessId <= 0) {
            throw new InvalidOperationException("Expected the WinUI harness launch to resolve a live process identifier.");
        }

        if (launch.MainWindow == null || launch.MainWindow.Handle == IntPtr.Zero) {
            throw new InvalidOperationException("Expected the WinUI harness launch to resolve a concrete main window handle.");
        }

        TestHelper.TrackProcessId(launcherProcessId);
        TestHelper.TrackProcessId(resolvedProcessId);

        return new DesktopManagerWinUiHarnessSession(sessionDirectory, statusFilePath, windowTitle, launcherProcessId, resolvedProcessId, launch.MainWindow.Handle);
    }

    public void Dispose() {
        KillProcessById(_resolvedProcessId);
        KillProcessById(_launcherProcessId);
        TryDeleteDirectory(_sessionDirectory);
    }

    public DesktopManagerWinUiHarnessStatus ReadStatus() {
        if (!File.Exists(_statusFilePath)) {
            throw new AssertInconclusiveException("The WinUI harness status file was not created.");
        }

        for (int attempt = 1; attempt <= 5; attempt++) {
            if (TryReadStatus(out DesktopManagerWinUiHarnessStatus? status) && status != null) {
                return status;
            }

            Thread.Sleep(50);
        }

        throw new AssertInconclusiveException("The WinUI harness status file could not be read.");
    }

    public DesktopManagerWinUiHarnessStatus WaitForStatus(Func<DesktopManagerWinUiHarnessStatus, bool> predicate, int timeoutMilliseconds, string failureMessage) {
        DateTime deadlineUtc = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);
        while (DateTime.UtcNow <= deadlineUtc) {
            if (TryReadStatus(out DesktopManagerWinUiHarnessStatus? status) && predicate(status!)) {
                return status!;
            }

            Thread.Sleep(100);
        }

        throw new AssertInconclusiveException(failureMessage);
    }

    public WindowQueryOptions CreateWindowQuery() {
        DesktopManagerWinUiHarnessStatus status = ReadStatus();
        return new WindowQueryOptions {
            Handle = _windowHandle,
            ProcessId = status.ProcessId == 0 ? _resolvedProcessId : status.ProcessId,
            TitlePattern = string.IsNullOrWhiteSpace(status.WindowTitle) ? WindowTitle : status.WindowTitle,
            IncludeHidden = false,
            IncludeCloaked = false,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        };
    }

    private bool TryReadStatus(out DesktopManagerWinUiHarnessStatus? status) {
        status = null;
        try {
            if (!File.Exists(_statusFilePath)) {
                return false;
            }

            using var stream = new FileStream(_statusFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            status = JsonSerializer.Deserialize<DesktopManagerWinUiHarnessStatus>(reader.ReadToEnd());
            return status != null;
        } catch {
            return false;
        }
    }

    private static string BuildArguments(string windowTitle, string initialText, string statusFilePath) {
        return "--title " + QuoteArgument(windowTitle) +
            " --text " + QuoteArgument(initialText) +
            " --status-file " + QuoteArgument(statusFilePath);
    }

    private static string QuoteArgument(string value) {
        return "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\"";
    }

    private static string RequireExecutablePath() {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null) {
            string debugCandidate = Path.Combine(current.FullName, "Sources", "DesktopManager.WinUiHarness", "bin", "Debug", "net10.0-windows10.0.19041.0", "win-x64", "DesktopManager.WinUiHarness.exe");
            if (File.Exists(debugCandidate)) {
                return debugCandidate;
            }

            string releaseCandidate = Path.Combine(current.FullName, "Sources", "DesktopManager.WinUiHarness", "bin", "Release", "net10.0-windows10.0.19041.0", "win-x64", "DesktopManager.WinUiHarness.exe");
            if (File.Exists(releaseCandidate)) {
                return releaseCandidate;
            }

            current = current.Parent;
        }

        throw new AssertInconclusiveException("DesktopManager.WinUiHarness.exe was not found. Build the DesktopManager.WinUiHarness project before running the live WinUI harness tests.");
    }

    private static void KillProcessById(int processId) {
        if (processId <= 0) {
            return;
        }

        try {
            using Process process = Process.GetProcessById(processId);
            TestHelper.SafeKillProcess(process);
        } catch {
            // Ignore cleanup failures for already exited processes.
        }
    }

    private static void TryDeleteDirectory(string path) {
        if (string.IsNullOrWhiteSpace(path)) {
            return;
        }

        try {
            if (Directory.Exists(path)) {
                Directory.Delete(path, recursive: true);
            }
        } catch {
            // Ignore cleanup failures for already removed temporary files.
        }
    }
}
