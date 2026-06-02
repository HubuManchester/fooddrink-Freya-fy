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

        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<DiaryPage>();
        builder.Services.AddTransient<NearbyPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<FoodDatabasePage>();

        return builder.Build();
    }
}