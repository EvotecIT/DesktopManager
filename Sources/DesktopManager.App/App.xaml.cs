using Microsoft.UI.Xaml;
using System.Threading;

namespace DesktopManager.App;

/// <summary>
/// WinUI application entry object.
/// </summary>
public sealed partial class App : Application {
    private const string SingleInstanceMutexName = "Local\\Evotec.DesktopManager.App";
    private readonly Mutex _singleInstanceMutex;
    private readonly bool _ownsSingleInstance;
    private Window? _window;

    /// <summary>
    /// Initializes the DesktopManager application.
    /// </summary>
    public App() {
        _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out _ownsSingleInstance);
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override void OnLaunched(LaunchActivatedEventArgs args) {
        if (!_ownsSingleInstance) {
            Exit();
            return;
        }

        bool startMinimized = ShouldStartMinimized(args.Arguments, Environment.GetCommandLineArgs());
        _window = new MainWindow();
        _window.Activate();
        if (startMinimized && _window is MainWindow mainWindow) {
            mainWindow.HideToTrayAfterLaunch();
        }
    }

    private static bool ShouldStartMinimized(string? launchArguments, string[] processArguments) {
        if (processArguments.Any(IsMinimizedArgument)) {
            return true;
        }

        if (string.IsNullOrWhiteSpace(launchArguments)) {
            return false;
        }

        string[] parts = launchArguments.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Any(IsMinimizedArgument);
    }

    private static bool IsMinimizedArgument(string argument) {
        return string.Equals(argument, "--minimized", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(argument, "--minimized-to-tray", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(argument, "/minimized", StringComparison.OrdinalIgnoreCase);
    }
}
