namespace NutriLens.Views;

public partial class SettingsPage : ContentPage
{
    private List<string> _customAllergens = new();
    private bool _isInitializing = true;

    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings();
        _isInitializing = false;
    }

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
            ? new List<string>()
            : saved.Split(',').Where(s => !string.IsNullOrEmpty(s)).ToList();
        RefreshAllergenList();

        // Reminders
        WaterReminderSwitch.IsToggled =
            Preferences.Default.Get("water_reminder", false);
    }

    // ── Theme ─────────────────────────────────────────────────────────────────

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

    // ── Font Size ─────────────────────────────────────────────────────────────

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

    // ── Goals ─────────────────────────────────────────────────────────────────

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

    // ── Allergens ─────────────────────────────────────────────────────────────

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

    private void SaveAllergens()
    {
        Preferences.Default.Set("custom_allergens",
            string.Join(",", _customAllergens));
    }

    private void RefreshAllergenList()
    {
        CustomAllergenList.ItemsSource = null;
        CustomAllergenList.ItemsSource = _customAllergens.ToList();
    }

    // ── Reminders ─────────────────────────────────────────────────────────────

    private void OnWaterReminderToggled(object sender, ToggledEventArgs e)
    {
        Preferences.Default.Set("water_reminder", e.Value);
    }
}