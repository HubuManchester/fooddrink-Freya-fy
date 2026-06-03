namespace NutriLens.Views;

public partial class SettingsPage : ContentPage
{
    private List<string> _customAllergens = [];
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0044:Make field readonly", Justification = "Must be modified after initialization")]
    private bool _isInitializing = true;

    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings();
        _isInitializing = false;
    }

    /// <summary>
    /// Loads all saved user settings including theme,
    /// font size, nutrition goals, allergens, and reminders.
    /// </summary>
    private void LoadSettings()
    {
        // Theme
        string theme = Preferences.Default.Get("app_theme", "System");
        LightModeRadio.IsChecked = theme == "Light";
        DarkModeRadio.IsChecked = theme == "Dark";
        SystemModeRadio.IsChecked = theme == "System";

        // Font size
        double fontSize = Preferences.Default.Get("font_size", 16.0);
        FontSizeSlider.Value = fontSize;
        FontSizeLabel.Text = $"{(int)fontSize}";

        // Apply saved font size to global resource immediately
        if (Application.Current?.Resources != null)
            Application.Current.Resources["GlobalFontSize"] = fontSize;

        // Goals
        CalorieTargetEntry.Text = Preferences.Default.Get("calorie_target", "2000");
        WaterTargetEntry.Text = Preferences.Default.Get("water_target", "2000");

        // Allergens
        string saved = Preferences.Default.Get("custom_allergens", "");
        _customAllergens = string.IsNullOrEmpty(saved)
        ? []
        : [.. saved.Split(',', StringSplitOptions.RemoveEmptyEntries)];
        RefreshAllergenList();

        // Reminders
        WaterReminderSwitch.IsToggled =
            Preferences.Default.Get("water_reminder", false);
    }

    /// <summary>
    /// Handles theme selection changes and applies the selected theme.
    /// </summary>
    /// <param name="sender">The radio button that triggered the event.</param>
    /// <param name="e">Checked state information.</param>
    private void OnThemeChanged(object sender, CheckedChangedEventArgs e)
    {
        if (_isInitializing || !e.Value) return;

        if (sender == LightModeRadio)
        {
            Application.Current!.UserAppTheme = AppTheme.Light;
            Preferences.Default.Set("app_theme", "Light");
        }
        else if (sender == DarkModeRadio)
        {
            Application.Current!.UserAppTheme = AppTheme.Dark;
            Preferences.Default.Set("app_theme", "Dark");
        }
        else if (sender == SystemModeRadio)
        {
            Application.Current!.UserAppTheme = AppTheme.Unspecified;
            Preferences.Default.Set("app_theme", "System");
        }
    }

    /// <summary>
    /// Updates the global application font size when the slider value changes.
    /// </summary>
    /// <param name="sender">The font size slider.</param>
    /// <param name="e">Contains the new slider value.</param>
    private void OnFontSizeChanged(object sender, ValueChangedEventArgs e)
    {
        if (_isInitializing) return;

        double size = e.NewValue;

        // Update display label
        FontSizeLabel.Text = $"{(int)size}";

        // Update global resource - all labels with DynamicResource will
        // update automatically across the entire app
        if (Application.Current?.Resources != null)
            Application.Current.Resources["GlobalFontSize"] = size;

        Preferences.Default.Set("font_size", size);
    }

    /// <summary>
    /// Validates and saves the user's calorie and water intake goals.
    /// </summary>
    /// <param name="sender">The save button.</param>
    /// <param name="e">Event arguments.</param>
    private async void OnSaveGoalsClicked(object sender, EventArgs e)
    {
        if (!int.TryParse(CalorieTargetEntry.Text, out int cal)
            || cal < 500 || cal > 9000)
        {
            await DisplayAlert("Invalid",
                "Please enter a valid calorie target (500-9000).", "OK");
            return;
        }

        if (!int.TryParse(WaterTargetEntry.Text, out int water)
            || water < 500 || water > 10000)
        {
            await DisplayAlert("Invalid",
                "Please enter a valid water target (500-10000 ml).", "OK");
            return;
        }

        Preferences.Default.Set("calorie_target", cal.ToString());
        Preferences.Default.Set("water_target", water.ToString());

        await DisplayAlert("Saved", "Your nutrition goals have been saved.", "OK");
    }

    /// <summary>
    /// Adds a new custom allergen to the user's allergen list.
    /// </summary>
    /// <param name="sender">The add button.</param>
    /// <param name="e">Event arguments.</param>
    private async void OnAddCustomAllergenClicked(object sender, EventArgs e)
    {
        string allergen = CustomAllergenEntry.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(allergen))
        {
            await DisplayAlert("Empty", "Please enter an allergen name.", "OK");
            return;
        }

        if (_customAllergens.Contains(allergen, StringComparer.OrdinalIgnoreCase))
        {
            await DisplayAlert("Duplicate",
                "This allergen is already in the list.", "OK");
            return;
        }

        _customAllergens.Add(allergen);
        SaveAllergens();
        RefreshAllergenList();
        CustomAllergenEntry.Text = "";
    }

    /// <summary>
    /// Removes a selected allergen from the custom allergen list.
    /// </summary>
    /// <param name="sender">The swipe action item.</param>
    /// <param name="e">Event arguments.</param>
    private void OnRemoveAllergenClicked(object sender, EventArgs e)
    {
        if (sender is SwipeItemView siv &&
            siv.CommandParameter is string allergen)
        {
            _customAllergens.Remove(allergen);
            SaveAllergens();
            RefreshAllergenList();
        }
    }

    /// <summary>
    /// Saves the current custom allergen list to device preferences.
    /// </summary>
    private void SaveAllergens()
    {
        Preferences.Default.Set("custom_allergens",
            string.Join(",", _customAllergens));
    }

    /// <summary>
    /// Refreshes the allergen list displayed in the user interface.
    /// </summary>
    private void RefreshAllergenList()
    {
        CustomAllergenList.ItemsSource = null;
        CustomAllergenList.ItemsSource = _customAllergens.ToList();
    }

    /// <summary>
    /// Saves the user's water reminder preference.
    /// </summary>
    /// <param name="sender">The reminder switch.</param>
    /// <param name="e">Contains the new toggle state.</param>
    private void OnWaterReminderToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Default.Set("water_reminder", e.Value);
    }
}