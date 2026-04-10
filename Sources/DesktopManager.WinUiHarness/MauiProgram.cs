using Microsoft.Extensions.Logging;

namespace DesktopManager.WinUiHarness;

public static class MauiProgram {
    public static MauiApp CreateMauiApp() {
        WinUiHarnessOptions options = WinUiHarnessOptions.Parse(Environment.GetCommandLineArgs());

        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
