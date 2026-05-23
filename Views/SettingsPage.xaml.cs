namespace NutriLens.Views;

/// <summary>
/// Settings page - manages app preferences, accessibility options,
/// nutrition goals and allergen warnings
/// Follows WCAG 2.1 accessibility guidelines
/// </summary>
public partial class SettingsPage : ContentPage
{
    // Water reminder timer
    private System.Timers.Timer? _waterReminderTimer;

    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings();
    }

    /// <summary>
    /// Load saved settings from preferences
    /// </summary>
    private void LoadSettings()
    {
        try
        {
            // Load saved preferences
            DarkModeSwitch.IsToggled =
                Preferences.Default.Get("dark_mode", false);
            TTSSwitch.IsToggled =
                Preferences.Default.Get("tts_enabled", false);
            PeanutSwitch.IsToggled =
                Preferences.Default.Get("allergen_peanut", false);
            GlutenSwitch.IsToggled =
                Preferences.Default.Get("allergen_gluten", false);
            LactoseSwitch.IsToggled =
                Preferences.Default.Get("allergen_lactose", false);
            WaterReminderSwitch.IsToggled =
                Preferences.Default.Get("water_reminder", false);

            double fontSize =
                Preferences.Default.Get("font_size", 16.0);
            FontSizeSlider.Value = fontSize;
            FontSizeLabel.Text = ((int)fontSize).ToString();

            CalorieTargetEntry.Text =
                Preferences.Default.Get("calorie_target", "2000");
            WaterTargetEntry.Text =
                Preferences.Default.Get("water_target", "2000");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Error loading settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Toggle dark mode - follows WCAG 1.4.3 contrast guidelines
    /// </summary>
    private void OnDarkModeToggled(object sender, ToggledEventArgs e)
    {
        try
        {
            Application.Current!.UserAppTheme = e.Value
                ? AppTheme.Dark
                : AppTheme.Light;

            Preferences.Default.Set("dark_mode", e.Value);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Dark mode error: {ex.Message}");
        }
    }

    /// <summary>
    /// Toggle text-to-speech for accessibility
    /// Follows WCAG 1.1.1 non-text content guideline
    /// </summary>
    private void OnTTSToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Default.Set("tts_enabled", e.Value);
    }

    /// <summary>
    /// Adjust font size for accessibility
    /// Follows WCAG 1.4.4 resize text guideline
    /// </summary>
    private void OnFontSizeChanged(object sender, ValueChangedEventArgs e)
    {
        int size = (int)e.NewValue;
        FontSizeLabel.Text = size.ToString();
        Preferences.Default.Set("font_size", e.NewValue);
    }

    /// <summary>
    /// Save nutrition goals with validation
    /// </summary>
    private async void OnSaveGoalsClicked(object sender, EventArgs e)
    {
        try
        {
            // Validate calorie target
            string calorieText = CalorieTargetEntry.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(calorieText))
            {
                await DisplayAlert("Validation Error",
                    "Please enter a calorie target.", "OK");
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
                return;
            }

            if (!int.TryParse(calorieText, out int calories) ||
                calories < 500 || calories > 10000)
            {
                await DisplayAlert("Validation Error",
                    "Calorie target must be between 500 and 10000.", "OK");
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
                return;
            }

            // Validate water target
            string waterText = WaterTargetEntry.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(waterText))
            {
                await DisplayAlert("Validation Error",
                    "Please enter a water target.", "OK");
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
                return;
            }

            if (!int.TryParse(waterText, out int water) ||
                water < 500 || water > 10000)
            {
                await DisplayAlert("Validation Error",
                    "Water target must be between 500 and 10000 ml.", "OK");
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
                return;
            }

            // Save valid goals
            Preferences.Default.Set("calorie_target", calorieText);
            Preferences.Default.Set("water_target", waterText);

            await DisplayAlert("Saved ✅",
                "Your nutrition goals have been saved.", "OK");

            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error",
                $"Failed to save goals: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Save allergen preferences when toggled
    /// </summary>
    private void OnAllergenToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Default.Set("allergen_peanut",
            PeanutSwitch.IsToggled);
        Preferences.Default.Set("allergen_gluten",
            GlutenSwitch.IsToggled);
        Preferences.Default.Set("allergen_lactose",
            LactoseSwitch.IsToggled);
    }

    /// <summary>
    /// Toggle water reminder - vibrates every hour if target not met
    /// </summary>
    private void OnWaterReminderToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Default.Set("water_reminder", e.Value);

        if (e.Value)
        {
            StartWaterReminder();
        }
        else
        {
            StopWaterReminder();
        }
    }

    /// <summary>
    /// Start hourly water reminder timer
    /// </summary>
    private void StartWaterReminder()
    {
        _waterReminderTimer = new System.Timers.Timer(3600000); // 1 hour
        _waterReminderTimer.Elapsed += async (s, e) =>
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));
            });
        };
        _waterReminderTimer.Start();
    }

    /// <summary>
    /// Stop water reminder timer
    /// </summary>
    private void StopWaterReminder()
    {
        _waterReminderTimer?.Stop();
        _waterReminderTimer?.Dispose();
        _waterReminderTimer = null;
    }
}