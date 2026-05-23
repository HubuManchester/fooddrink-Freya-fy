namespace NutriLens;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Sync dark mode on startup
        bool darkMode = Preferences.Default.Get("dark_mode", false);
        UserAppTheme = darkMode ? AppTheme.Dark : AppTheme.Light;

        MainPage = new AppShell();
    }
}