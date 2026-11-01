using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DesktopManager.AotSmoke;

internal static class Program {
    private static readonly string[] Areas = ["monitors", "wallpaper", "slideshow", "checkpoint", "clipboard", "explorer", "explorer-fixture", "virtual-desktops", "audio", "uia"];

    // Each native boundary runs in a disposable process: even a stuck COM server cannot hang the runner.
    [STAThread]
    private static int Main(string[] args) {
        if (!OperatingSystem.IsWindows() || RuntimeFeature.IsDynamicCodeSupported || RuntimeFeature.IsDynamicCodeCompiled) {
            Console.Error.WriteLine("FAIL: run the published Windows Native AOT executable, not dotnet run.");
            return 1;
        }
        if (args.Length == 2 && args[0] == "--worker" && Areas.Contains(args[1])) {
            try {
                SmokeChecks.Run(args[1]);
                Console.WriteLine($"PASS {args[1]}");
                return 0;
            } catch (UnavailableException ex) {
                Console.WriteLine($"UNAVAILABLE {args[1]}: {ex.Message}");
                return 2;
            } catch (Exception ex) {
                Console.Error.WriteLine($"FAIL {args[1]}: {ex.GetType().Name} HRESULT=0x{ex.HResult:X8}: {ex.Message}");
                Console.Error.WriteLine(ex.StackTrace);
                return 1;
            }
        }
        if (args.Length != 0) {
            Console.Error.WriteLine("Usage: DesktopManager.AotSmoke.exe");
            return 1;
        }
        int failures = 0, unavailable = 0, cleanupFailures = 0;
        foreach (string area in Areas) {
            string fixtureDirectory = Path.Combine(Path.GetTempPath(), "DesktopManager.AotSmoke-" + Guid.NewGuid().ToString("N"));
            using var worker = new Process {
                StartInfo = new ProcessStartInfo(Environment.ProcessPath!) {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    ArgumentList = { "--worker", area }
                }
            };
            worker.StartInfo.Environment[NativeFixtures.DirectoryVariable] = fixtureDirectory;
            Console.WriteLine($"BEGIN {area} (30 second process deadline)");
            bool exited = false;
            try {
                worker.Start();
                Task<string> output = worker.StandardOutput.ReadToEndAsync();
                Task<string> error = worker.StandardError.ReadToEndAsync();
                exited = worker.WaitForExit(30_000);
                if (!exited) {
                    worker.Kill(entireProcessTree: true);
                    exited = worker.WaitForExit(5_000);
                    Console.Error.WriteLine($"FAIL {area}: worker timed out; termination confirmed={exited}.");
                    failures++;
                } else if (worker.ExitCode == 2) {
                    unavailable++;
                } else if (worker.ExitCode != 0) {
                    failures++;
                }
                if (exited) {
                    Console.Write(output.GetAwaiter().GetResult());
                    Console.Error.Write(error.GetAwaiter().GetResult());
                }
            } catch (Exception ex) {
                Console.Error.WriteLine($"FAIL {area}: worker lifecycle: {ex.Message}");
                failures++;
            } finally {
                // A hung Shell call may skip the worker's finally. Clean only after confirmed exit.
                if (exited) {
                    try { NativeFixtures.CleanupDirectory(fixtureDirectory); }
                    catch (Exception ex) {
                        Console.Error.WriteLine($"FAIL {area}: owned fixture cleanup: {ex.Message}");
                        cleanupFailures++;
                    }
                } else if (Directory.Exists(fixtureDirectory)) {
                    Console.Error.WriteLine($"RETAINED owned fixture awaiting worker exit: {fixtureDirectory}");
                }
            }
        }
        Console.WriteLine($"Native AOT smoke: {Areas.Length - failures - unavailable} passed, {unavailable} unavailable, {failures} failed.");
        if (cleanupFailures > 0) { Console.Error.WriteLine($"Owned fixture cleanup failures: {cleanupFailures}."); }
        return failures > 0 || cleanupFailures > 0 ? 1 : unavailable > 0 ? 2 : 0;
    }
}

internal sealed class UnavailableException(string message) : Exception(message);
