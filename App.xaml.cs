namespace NutriLens;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Sync dark mode on startup
        bool darkMode = Preferences.Default.Get("dark_mode", false);
        UserAppTheme = darkMode ? AppTheme.Dark : AppTheme.Light;

        // Sync font size on startup
        double fontSize = Preferences.Default.Get("font_size", 16.0);
        if (Resources != null)
        {
            Resources["GlobalFontSize"] = fontSize;
        }

        MainPage = new AppShell();
    }
}