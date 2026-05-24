namespace NutriLens.Views;

/// <summary>
/// Settings page - manages app preferences, accessibility options,
/// nutrition goals and allergen warnings.
/// Follows WCAG 2.1 accessibility guidelines.
/// </summary>
public partial class SettingsPage : ContentPage
{
    // Water reminder timer
    private System.Timers.Timer? _waterReminderTimer;

    // Custom allergens list
    private List<string> _customAllergens = new List<string>();

    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings();
    }

    /// <summary>
    /// Load all saved settings from preferences on page load
    /// </summary>
    private void LoadSettings()
    {
        try
        {
            // Load dark mode
            DarkModeSwitch.IsToggled =
                Preferences.Default.Get("dark_mode", false);

            // Load TTS
            TTSSwitch.IsToggled =
                Preferences.Default.Get("tts_enabled", false);

            // Load water reminder
            WaterReminderSwitch.IsToggled =
                Preferences.Default.Get("water_reminder", false);

            // Load font size
            double fontSize =
                Preferences.Default.Get("font_size", 16.0);
            FontSizeSlider.Value = fontSize;
            FontSizeLabel.Text = ((int)fontSize).ToString();

            // Load nutrition goals
            CalorieTargetEntry.Text =
                Preferences.Default.Get("calorie_target", "2000");
            WaterTargetEntry.Text =
                Preferences.Default.Get("water_target", "2000");

            // Load custom allergens
            string saved =
                Preferences.Default.Get("custom_allergens", "");
            if (!string.IsNullOrEmpty(saved))
            {
                _customAllergens = saved.Split(',').ToList();
                UpdateCustomAllergenList();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Error loading settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Toggle dark mode
    /// Follows WCAG 1.4.3 contrast ratio guideline
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
    /// Toggle text-to-speech
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

        // Apply font size globally
        if (Application.Current?.Resources != null)
        {
            Application.Current.Resources["GlobalFontSize"] = (double)size;
        }
    }

    /// <summary>
    /// Save nutrition goals with full validation
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
                    "Calorie target must be between 500 and 10000 kcal.",
                    "OK");
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
                $"Goals saved!\nCalories: {calories} kcal\nWater: {water} ml",
                "OK");

            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error",
                $"Failed to save goals: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Add custom allergen to personal list
    /// </summary>
    private async void OnAddCustomAllergenClicked(object sender, EventArgs e)
    {
        try
        {
            string allergen = CustomAllergenEntry.Text?.Trim() ?? "";

            // Validate input
            if (string.IsNullOrEmpty(allergen))
            {
                await DisplayAlert("Validation Error",
                    "Please enter an allergen name.", "OK");
                return;
            }

            if (allergen.Length < 2)
            {
                await DisplayAlert("Validation Error",
                    "Allergen name must be at least 2 characters.", "OK");
                return;
            }

            // Check for duplicates
            if (_customAllergens.Any(a =>
                a.ToLower() == allergen.ToLower()))
            {
                await DisplayAlert("Already Added",
                    $"{allergen} is already in your allergen list.", "OK");
                return;
            }

            // Add allergen
            _customAllergens.Add(allergen);
            CustomAllergenEntry.Text = "";
            UpdateCustomAllergenList();

            // Save to preferences
            Preferences.Default.Set("custom_allergens",
                string.Join(",", _customAllergens));

            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(100));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error",
                $"Failed to add allergen: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Remove allergen on left swipe delete
    /// </summary>
    private void OnRemoveAllergenClicked(object sender, EventArgs e)
    {
        try
        {
            // Handle both SwipeItem and SwipeItemView
            string? allergen = null;

            if (sender is SwipeItemView swipeItemView)
            {
                allergen = swipeItemView.BindingContext as string;
            }
            else if (sender is SwipeItem swipeItem)
            {
                allergen = swipeItem.BindingContext as string;
            }

            if (allergen == null) return;

            _customAllergens.Remove(allergen);
            UpdateCustomAllergenList();

            Preferences.Default.Set("custom_allergens",
                string.Join(",", _customAllergens));

            try
            {
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Vibration error: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Error removing allergen: {ex.Message}");
        }
    }

    /// <summary>
    /// Refresh custom allergen list display
    /// </summary>
    private void UpdateCustomAllergenList()
    {
        CustomAllergenList.ItemsSource = null;
        CustomAllergenList.ItemsSource = _customAllergens;
    }

    /// <summary>
    /// Toggle hourly water reminder vibration
    /// </summary>
    private void OnWaterReminderToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Default.Set("water_reminder", e.Value);

        if (e.Value)
            StartWaterReminder();
        else
            StopWaterReminder();
    }

    /// <summary>
    /// Start hourly water reminder timer
    /// </summary>
    private void StartWaterReminder()
    {
        _waterReminderTimer = new System.Timers.Timer(3600000);
        _waterReminderTimer.Elapsed += async (s, e) =>
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                try
                {
                    Vibration.Default.Vibrate(
                        TimeSpan.FromMilliseconds(500));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Vibration error: {ex.Message}");
                }
            });
        };
        _waterReminderTimer.Start();
    }

    /// <summary>
    /// Stop and dispose water reminder timer
    /// </summary>
    private void StopWaterReminder()
    {
        _waterReminderTimer?.Stop();
        _waterReminderTimer?.Dispose();
        _waterReminderTimer = null;
    }
}