namespace NutriLens.Views;

/// <summary>
/// Settings page - manages app preferences and accessibility options
/// </summary>
public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Toggle dark mode on/off
    /// </summary>
    private void OnDarkModeToggled(object sender, ToggledEventArgs e)
    {
        Application.Current.UserAppTheme = e.Value
            ? AppTheme.Dark
            : AppTheme.Light;
    }

    /// <summary>
    /// Toggle text-to-speech on/off
    /// </summary>
    private void OnTTSToggled(object sender, ToggledEventArgs e)
    {
        // Will save TTS preference to database later
    }

    /// <summary>
    /// Adjust font size for accessibility
    /// </summary>
    private void OnFontSizeChanged(object sender, ValueChangedEventArgs e)
    {
        FontSizeLabel.Text = ((int)e.NewValue).ToString();
    }
}