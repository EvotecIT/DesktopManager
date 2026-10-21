using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopManager;

internal interface IDesktopCommandRunner {
    string? FindExecutable(params string[] names);
    string Run(string executable, params string[] arguments);
}

/// <summary>Runs desktop facilities without a shell, with bounded output and a fixed completion deadline.</summary>
internal sealed class DesktopCommandRunner : IDesktopCommandRunner {
    private const int MaximumOutputCharacters = 1024 * 1024;
    private const int TimeoutMilliseconds = 15000;

    public string? FindExecutable(params string[] names) {
        foreach (var name in names) {
            if (Path.IsPathRooted(name)) {
                if (File.Exists(name)) {
                    return name;
                }
                continue;
            }
            foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator)) {
                if (!string.IsNullOrWhiteSpace(directory)) {
                    var candidate = Path.Combine(directory, name);
                    if (File.Exists(candidate)) {
                        return candidate;
                    }
                }
            }
        }
        return null;
    }

    public string Run(string executable, params string[] arguments) {
        var elapsed = Stopwatch.StartNew();
        var start = new ProcessStartInfo(executable) {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        // The .NET Standard and .NET Framework contracts predate ArgumentList. This
        // quotes each argv element for ProcessStartInfo; no shell interprets the data.
        start.Arguments = string.Join(" ", Array.ConvertAll(arguments, QuoteArgument));
        using var process = Process.Start(start) ?? throw new InvalidOperationException("The desktop command could not be started.");
        var output = ReadBoundedAsync(process.StandardOutput);
        var error = ReadBoundedAsync(process.StandardError);
        var drains = Task.WhenAll(output, error);
        try {
            if (!process.WaitForExit(RemainingMilliseconds(elapsed))) {
                throw DeadlineExceeded();
            }
            // Descendants can retain stdout/stderr after the launched process exits.
            // Exit and both drains share one deadline; never join a live pipe task.
            using var drainDeadline = new CancellationTokenSource();
            var delay = Task.Delay(RemainingMilliseconds(elapsed), drainDeadline.Token);
            if (Task.WhenAny(drains, delay).GetAwaiter().GetResult() != drains) {
                throw DeadlineExceeded();
            }
            drainDeadline.Cancel();
            drains.GetAwaiter().GetResult();
            var result = output.GetAwaiter().GetResult();
            var diagnostic = error.GetAwaiter().GetResult();
            if (process.ExitCode != 0) {
                throw new InvalidOperationException("The desktop command failed (exit " + process.ExitCode + "): " + diagnostic.Trim());
            }
            return result;
        } finally {
            if (!drains.IsCompleted) {
                // Closing only our pipe endpoints releases pending reads even if a
                // descendant is still alive. Do not wait again after the deadline.
                process.StandardOutput.BaseStream.Dispose();
                process.StandardError.BaseStream.Dispose();
                _ = drains.ContinueWith(completed => { _ = completed.Exception; },
                    CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
            try {
                if (!process.HasExited) {
                    process.Kill();
                }
            } catch (InvalidOperationException) {
                // The owned child may have exited between HasExited and Kill.
            }
        }
    }

    private static int RemainingMilliseconds(Stopwatch elapsed) => Math.Max(0, TimeoutMilliseconds - (int)Math.Min(int.MaxValue, elapsed.ElapsedMilliseconds));
    private static TimeoutException DeadlineExceeded() => new TimeoutException("The desktop command exceeded its 15-second exit/output deadline.");

    internal static string QuoteArgument(string argument) {
        if (argument.IndexOf('\0') >= 0) {
            throw new ArgumentException("Process arguments cannot contain a null character.", nameof(argument));
        }
        var quoted = new StringBuilder("\"");
        var slashes = 0;
        foreach (var character in argument) {
            if (character == '\\') {
                slashes++;
                continue;
            }
            quoted.Append('\\', character == '"' ? slashes * 2 + 1 : slashes);
            quoted.Append(character);
            slashes = 0;
        }
        quoted.Append('\\', slashes * 2);
        return quoted.Append('"').ToString();
    }

    private static async Task<string> ReadBoundedAsync(StreamReader reader) {
        var text = new StringBuilder();
        var buffer = new char[4096];
        var exceeded = false;
        int count;
        while ((count = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0) {
            var remaining = MaximumOutputCharacters - text.Length;
            if (remaining < count) {
                exceeded = true;
            }
            if (remaining > 0) {
                text.Append(buffer, 0, Math.Min(count, remaining));
            }
        }
        if (exceeded) {
            throw new InvalidOperationException("The desktop command exceeded its output limit.");
        }
        return text.ToString();
    }
}
