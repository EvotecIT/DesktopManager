using Microsoft.UI.Xaml;

namespace DesktopManager.App;

/// <summary>
/// WinUI application entry object.
/// </summary>
public sealed partial class App : Application {
    private Window? _window;

    /// <summary>
    /// Initializes the DesktopManager application.
    /// </summary>
    public App() {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override void OnLaunched(LaunchActivatedEventArgs args) {
        _window = new MainWindow();
        _window.Activate();
    }
}
