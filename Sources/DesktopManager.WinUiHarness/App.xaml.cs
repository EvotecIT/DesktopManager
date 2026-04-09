namespace DesktopManager.WinUiHarness;

public partial class App : Application {
    private readonly WinUiHarnessOptions _options;
    private readonly MainPage _mainPage;

    public App(WinUiHarnessOptions options, MainPage mainPage) {
        InitializeComponent();
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _mainPage = mainPage ?? throw new ArgumentNullException(nameof(mainPage));
    }

    protected override Window CreateWindow(IActivationState? activationState) {
        return new Window(_mainPage) {
            Title = _options.Title
        };
    }
}
