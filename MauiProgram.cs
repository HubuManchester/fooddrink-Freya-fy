using CommunityToolkit.Maui;
using ZXing.Net.Maui.Controls;
using NutriLens.Services;
using NutriLens.Views;

namespace NutriLens;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register database service as singleton
        builder.Services.AddSingleton<DatabaseService>();

        // Register pages with dependency injection
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<ScannerPage>();
        builder.Services.AddTransient<DiaryPage>();
        builder.Services.AddTransient<NearbyPage>();
        builder.Services.AddTransient<SettingsPage>();

        return builder.Build();
    }
}