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

        _window = new MainWindow();
        _window.Activate();
    }
}
