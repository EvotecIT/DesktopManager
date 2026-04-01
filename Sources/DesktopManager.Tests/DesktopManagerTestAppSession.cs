using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace DesktopManager.Tests;

internal sealed class DesktopManagerTestAppSession : IDisposable {
    private const int LaunchTimeoutMilliseconds = 20000;
    private const int CommandWriteRetryCount = 5;
    private const int MaxRetainedHostedSessionArtifacts = 12;
    private readonly string _sessionDirectory;
    private readonly string _statusFilePath;
    private readonly string _commandFilePath;
    private readonly int _launcherProcessId;
    private readonly int _resolvedProcessId;
    private readonly IntPtr _windowHandle;

    private DesktopManagerTestAppSession(string sessionDirectory, string statusFilePath, string commandFilePath, string windowTitle, int launcherProcessId, int resolvedProcessId, IntPtr windowHandle) {
        _sessionDirectory = sessionDirectory;
        _statusFilePath = statusFilePath;
        _commandFilePath = commandFilePath;
        WindowTitle = windowTitle;
        _launcherProcessId = launcherProcessId;
        _resolvedProcessId = resolvedProcessId;
        _windowHandle = windowHandle;
    }

    public string WindowTitle { get; }

    public int ProcessId => _resolvedProcessId;

    public IntPtr WindowHandle => _windowHandle;

    public WindowQueryOptions CreateWindowQuery() {
        return new WindowQueryOptions {
            Handle = _windowHandle,
            ProcessId = _resolvedProcessId,
            TitlePattern = WindowTitle,
            IncludeHidden = false,
            IncludeCloaked = false,
            IncludeOwned = true,
            IncludeEmptyTitles = true
        };
    }

    public WindowControlQueryOptions CreateEditorControlQuery() {
        return new WindowControlQueryOptions {
            ClassNamePattern = "*Edit*",
            SupportsBackgroundText = true
        };
    }

    public static DesktopManagerTestAppSession Start(string scenario, string initialText = "seed", string? surface = null) {
        if (string.IsNullOrWhiteSpace(scenario)) {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(scenario));
        }

        string windowTitle = "DesktopManager-TestApp-" + scenario + "-" + Guid.NewGuid().ToString("N");
        string sessionDirectory = Path.Combine(Path.GetTempPath(), "DesktopManager.Tests", "TestApp", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sessionDirectory);
        string statusFilePath = Path.Combine(sessionDirectory, "status.json");
        string commandFilePath = Path.Combine(sessionDirectory, "command.txt");
        var automation = new DesktopAutomationService();
        DesktopProcessLaunchInfo launch = automation.LaunchProcess(new DesktopProcessStartOptions {
            FilePath = RequireExecutablePath(),
            Arguments = BuildArguments(windowTitle, initialText, surface, statusFilePath, commandFilePath),
            WaitForInputIdleMilliseconds = 5000,
            WaitForWindowMilliseconds = LaunchTimeoutMilliseconds,
            WaitForWindowIntervalMilliseconds = 100,
            RequireWindow = true
        });

        int launcherProcessId = launch.ProcessId;
        int resolvedProcessId = launch.ResolvedProcessId ?? launch.ProcessId;
        if (resolvedProcessId <= 0) {
            throw new InvalidOperationException("Expected the test app launch to resolve a live process identifier.");
        }
        if (launch.MainWindow == null || launch.MainWindow.Handle == IntPtr.Zero) {
            throw new InvalidOperationException("Expected the test app launch to resolve a concrete main window handle.");
        }

        TestHelper.TrackProcessId(launcherProcessId);
        TestHelper.TrackProcessId(resolvedProcessId);
        return new DesktopManagerTestAppSession(sessionDirectory, statusFilePath, commandFilePath, windowTitle, launcherProcessId, resolvedProcessId, launch.MainWindow.Handle);
    }

    public void Dispose() {
        KillProcessById(_resolvedProcessId);
        KillProcessById(_launcherProcessId);
        TryDeleteDirectory(_sessionDirectory);
    }

    public void RequestFocusEditor() {
        WriteCommandFile("focus-editor");
    }

    public void RequestFocusCommandBar() {
        WriteCommandFile("focus-commandbar");
    }

    public void RequestFocusSecondary() {
        WriteCommandFile("focus-secondary");
    }

    public void RequestHoldEditorForeground(int durationMilliseconds) {
        WriteCommandFile("hold-editor-foreground:" + durationMilliseconds);
    }

    public void RequestHoldCommandBarForeground(int durationMilliseconds) {
        WriteCommandFile("hold-commandbar-foreground:" + durationMilliseconds);
    }

    public void RequestStopForegroundHold() {
        WriteCommandFile("stop-foreground-hold");
    }

    public DesktopManagerTestAppStatus ReadStatus() {
        if (!File.Exists(_statusFilePath)) {
            throw new AssertInconclusiveException("The desktop test app status file was not created.");
        }

        DesktopManagerTestAppStatus? status = JsonSerializer.Deserialize<DesktopManagerTestAppStatus>(File.ReadAllText(_statusFilePath));
        if (status == null) {
            throw new AssertInconclusiveException("The desktop test app status file could not be read.");
        }

        return status;
    }

    public DesktopManagerTestAppStatus WaitForStatus(Func<DesktopManagerTestAppStatus, bool> predicate, int timeoutMilliseconds, string failureMessage) {
        DateTime deadlineUtc = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);
        while (DateTime.UtcNow <= deadlineUtc) {
            if (TryReadStatus(out DesktopManagerTestAppStatus? status) && predicate(status!)) {
                return status!;
            }

            Thread.Sleep(100);
        }

        throw new AssertInconclusiveException(failureMessage);
    }

    public WindowInfo ResolveWindowInfo() {
        WindowInfo? window = new DesktopAutomationService().GetWindow(
            _windowHandle,
            includeHidden: true,
            includeCloaked: true,
            includeOwned: true,
            includeEmptyTitles: true);
        if (window == null) {
            throw new AssertInconclusiveException("The DesktopManager test app window could not be resolved by handle.");
        }

        return window;
    }

    public DesktopManagerTestAppStatus WaitForEditorForeground(int timeoutMilliseconds, string failureMessage) {
        return WaitForStatus(
            status => status.IsForegroundWindow &&
                string.Equals(status.ActiveSurface, "editor", StringComparison.OrdinalIgnoreCase),
            timeoutMilliseconds,
            failureMessage);
    }

    public DesktopManagerTestAppStatus FocusEditorWindow(int timeoutMilliseconds) {
        RequestFocusEditor();
        try {
            new DesktopAutomationService().FocusWindows(CreateWindowQuery());
        } catch (InvalidOperationException) {
            // The test app keeps retrying focus from its own UI thread.
        }

        return WaitForEditorForeground(timeoutMilliseconds, "The DesktopManager test app editor did not become the foreground target.");
    }

    public string WriteStatusArtifact(string reason, string? summaryText = null, string? summaryCategoryHint = null) {
        return WriteStatusArtifact(reason, null, null, summaryText, summaryCategoryHint);
    }

    public string WriteStatusArtifact(string reason, HostedSessionRetryHistoryReport? retryHistoryReport, string? policyReport = null, string? summaryText = null, string? summaryCategoryHint = null) {
        DesktopManagerTestAppStatus status = ReadStatus();
        string repositoryRoot = RequireRepositoryRoot();
        string artifactDirectory = Path.Combine(repositoryRoot, "Artifacts", "HostedSessionTyping");
        Directory.CreateDirectory(artifactDirectory);
        PruneHostedSessionArtifacts(artifactDirectory, MaxRetainedHostedSessionArtifacts - 1);

        string fileName =
            DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff") + "-" +
            SanitizeFileName(reason) + "-" +
            SanitizeFileName(WindowTitle) + ".json";
        string artifactPath = Path.Combine(artifactDirectory, fileName);

        HostedSessionDiagnosticArtifact artifact = BuildStatusArtifact(reason, status, retryHistoryReport, policyReport, summaryText);
        string json = JsonSerializer.Serialize(artifact, new JsonSerializerOptions {
            WriteIndented = true
        });
        File.WriteAllText(artifactPath, json);
        if (!string.IsNullOrWhiteSpace(summaryText)) {
            File.WriteAllText(GetSummaryArtifactPath(artifactPath, summaryCategoryHint), summaryText);
        }

        return artifactPath;
    }

    public static string GetSummaryArtifactPath(string artifactPath, string? categoryHint = null) {
        if (string.IsNullOrWhiteSpace(artifactPath)) {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(artifactPath));
        }

        string extension = string.IsNullOrWhiteSpace(categoryHint)
            ? ".summary.txt"
            : "." + SanitizeFileName(categoryHint ?? string.Empty) + ".summary.txt";
        return Path.ChangeExtension(artifactPath, extension);
    }

    internal static void PruneHostedSessionArtifacts(string artifactDirectory, int maxJsonArtifacts) {
        if (string.IsNullOrWhiteSpace(artifactDirectory) || !Directory.Exists(artifactDirectory)) {
            return;
        }

        if (maxJsonArtifacts < 0) {
            throw new ArgumentOutOfRangeException(nameof(maxJsonArtifacts));
        }

        string[] jsonArtifacts = Directory.GetFiles(artifactDirectory, "*.json", SearchOption.TopDirectoryOnly);
        if (jsonArtifacts.Length <= maxJsonArtifacts) {
            return;
        }

        Array.Sort(jsonArtifacts, CompareArtifactPathsByLastWriteDescending);
        for (int index = maxJsonArtifacts; index < jsonArtifacts.Length; index++) {
            foreach (string path in GetArtifactSetPaths(jsonArtifacts[index])) {
                TryDeleteFile(path);
            }
        }
    }

    internal static string[] GetArtifactSetPaths(string jsonArtifactPath) {
        if (string.IsNullOrWhiteSpace(jsonArtifactPath)) {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(jsonArtifactPath));
        }

        string directory = Path.GetDirectoryName(jsonArtifactPath) ?? string.Empty;
        string stem = Path.GetFileNameWithoutExtension(jsonArtifactPath);
        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(stem) || !Directory.Exists(directory)) {
            return new[] { jsonArtifactPath };
        }

        var paths = new List<string> { jsonArtifactPath };
        string summaryPattern = stem + "*.summary.txt";
        foreach (string summaryPath in Directory.GetFiles(directory, summaryPattern, SearchOption.TopDirectoryOnly)) {
            if (!ContainsPath(paths, summaryPath)) {
                paths.Add(summaryPath);
            }
        }

        return paths.ToArray();
    }

    internal static HostedSessionDiagnosticArtifact BuildStatusArtifact(string reason, DesktopManagerTestAppStatus status, HostedSessionRetryHistoryReport? retryHistoryReport = null, string? policyReport = null, string? summaryText = null) {
        if (string.IsNullOrWhiteSpace(reason)) {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(reason));
        }

        if (status == null) {
            throw new ArgumentNullException(nameof(status));
        }

        HostedSessionRetryHistoryReport resolvedRetryHistoryReport = retryHistoryReport ?? HostedSessionRetryHistoryReport.None;
        string resolvedPolicyReport = string.IsNullOrWhiteSpace(policyReport)
            ? HostedSessionDiagnosticFormatter.CreateExternalForegroundReport(status).ToPolicyReport()
            : policyReport ?? string.Empty;
        string resolvedSummaryText = string.IsNullOrWhiteSpace(summaryText)
            ? HostedSessionDiagnosticFormatter.BuildArtifactSummary(reason, status, resolvedRetryHistoryReport, resolvedPolicyReport)
            : summaryText ?? string.Empty;

        return new HostedSessionDiagnosticArtifact {
            Reason = reason,
            CreatedUtc = DateTime.UtcNow.ToString("O"),
            Summary = resolvedSummaryText,
            PolicyReport = resolvedPolicyReport,
            RetryHistoryReport = resolvedRetryHistoryReport,
            Status = status
        };
    }

    private bool TryReadStatus(out DesktopManagerTestAppStatus? status) {
        status = null;
        try {
            if (!File.Exists(_statusFilePath)) {
                return false;
            }

            status = JsonSerializer.Deserialize<DesktopManagerTestAppStatus>(File.ReadAllText(_statusFilePath));
            return status != null;
        } catch {
            return false;
        }
    }

    private void WriteCommandFile(string command) {
        for (int attempt = 1; attempt <= CommandWriteRetryCount; attempt++) {
            try {
                File.WriteAllText(_commandFilePath, command);
                return;
            } catch (IOException) when (attempt < CommandWriteRetryCount) {
                Thread.Sleep(50);
            }
        }

        File.WriteAllText(_commandFilePath, command);
    }

    private static string BuildArguments(string windowTitle, string initialText, string? surface, string statusFilePath, string commandFilePath) {
        string arguments = "--title " + QuoteArgument(windowTitle) + " --text " + QuoteArgument(initialText);
        if (!string.IsNullOrWhiteSpace(surface)) {
            arguments += " --surface " + QuoteArgument(surface!);
        }

        arguments += " --status-file " + QuoteArgument(statusFilePath);
        arguments += " --command-file " + QuoteArgument(commandFilePath);

        return arguments;
    }

    private static string QuoteArgument(string value) {
        return "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\"";
    }

    private static string RequireExecutablePath() {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null) {
            string preferred = Path.Combine(current.FullName, "Sources", "DesktopManager.TestApp", "bin", "Debug", GetPreferredTargetFramework(), "DesktopManager.TestApp.exe");
            if (File.Exists(preferred)) {
                return preferred;
            }

            string fallback = Path.Combine(current.FullName, "Sources", "DesktopManager.TestApp", "bin", "Debug", GetFallbackTargetFramework(), "DesktopManager.TestApp.exe");
            if (File.Exists(fallback)) {
                return fallback;
            }

            current = current.Parent;
        }

        throw new AssertInconclusiveException("DesktopManager.TestApp.exe was not found. Build the DesktopManager.TestApp project before running live typing tests.");
    }

    private static string RequireRepositoryRoot() {
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null) {
            if (Directory.Exists(Path.Combine(current.FullName, "Sources")) && Directory.Exists(Path.Combine(current.FullName, "Artifacts"))) {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new AssertInconclusiveException("Repository root could not be resolved for hosted-session diagnostics.");
    }

    private static string GetPreferredTargetFramework() {
#if NET10_0
        return "net10.0-windows";
#else
        return "net8.0-windows";
#endif
    }

    private static string GetFallbackTargetFramework() {
#if NET10_0
        return "net8.0-windows";
#else
        return "net10.0-windows";
#endif
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

    private static int CompareArtifactPathsByLastWriteDescending(string left, string right) {
        DateTime leftWriteTime = File.Exists(left) ? File.GetLastWriteTimeUtc(left) : DateTime.MinValue;
        DateTime rightWriteTime = File.Exists(right) ? File.GetLastWriteTimeUtc(right) : DateTime.MinValue;
        int comparison = rightWriteTime.CompareTo(leftWriteTime);
        if (comparison != 0) {
            return comparison;
        }

        return string.Compare(right, left, StringComparison.OrdinalIgnoreCase);
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

    private static void TryDeleteFile(string path) {
        if (string.IsNullOrWhiteSpace(path)) {
            return;
        }

        try {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        } catch {
            // Ignore cleanup failures for already removed temporary files.
        }
    }

    private static string SanitizeFileName(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return "status";
        }

        char[] invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);
        foreach (char character in value) {
            builder.Append(Array.IndexOf(invalid, character) >= 0 || char.IsWhiteSpace(character) ? '-' : character);
        }

        return builder.ToString().Trim('-');
    }

    private static bool ContainsPath(IReadOnlyList<string> paths, string candidate) {
        foreach (string path in paths) {
            if (string.Equals(path, candidate, StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }

        return false;
    }
}
