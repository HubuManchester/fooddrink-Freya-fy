using NutriLens.Models;

namespace NutriLens.Views;

public partial class FoodDetailPage : ContentPage
{
    private readonly FoodItem _food;
    private bool _isSpeaking = false;
    private CancellationTokenSource? _cts;

    private static readonly Dictionary<string, (string Color, string Icon)> CategoryMap = new()
    {
        { "Meat",       ("#FFEBEE", "🥩") },
        { "Fish",       ("#E0F7FA", "🐟") },
        { "Vegetables", ("#E8F5E9", "🥦") },
        { "Fruits",     ("#FFF9C4", "🍎") },
        { "Dairy",      ("#F3E5F5", "🥛") },
        { "Grains",     ("#FFF8E1", "🌾") },
        { "Snacks",     ("#FCE4EC", "🍫") },
        { "Drinks",     ("#E1F5FE", "🥤") },
        { "Other",      ("#F5F5F5", "🍳") },
    };

    private static readonly Dictionary<string, string> HeroColors = new()
    {
        { "Meat",       "#EF5350" },
        { "Fish",       "#26C6DA" },
        { "Vegetables", "#66BB6A" },
        { "Fruits",     "#FFCA28" },
        { "Dairy",      "#AB47BC" },
        { "Grains",     "#FFA726" },
        { "Snacks",     "#EC407A" },
        { "Drinks",     "#29B6F6" },
        { "Other",      "#78909C" },
    };

    public FoodDetailPage(FoodItem food)
    {
        InitializeComponent();
        _food = food;
        PopulateUI();
    }

    /// <summary>
    /// Populate all UI elements with food information
    /// </summary>
    private void PopulateUI()
    {
        // ── Hero background ───────────────────────────────────────────────
        bool hasPhoto = !string.IsNullOrEmpty(_food.ImagePath)
                        && File.Exists(_food.ImagePath);

        if (hasPhoto)
        {
            // Show the real food photo as the hero background
            HeroImage.Source = ImageSource.FromFile(_food.ImagePath);
            HeroImage.IsVisible = true;

            // Keep a subtle tinted fallback colour in case image loads slowly
            HeroGrid.BackgroundColor = Color.FromArgb("#333333");

            // Hide the generic category icon — the photo is enough
            IconFrame.IsVisible = false;
        }
        else
        {
            // No photo: use the coloured gradient hero with category icon
            string heroColor = HeroColors.TryGetValue(_food.Category, out var hc)
                ? hc : "#4CAF50";
            HeroGrid.BackgroundColor = Color.FromArgb(heroColor);

            var (bgColor, icon) = CategoryMap.TryGetValue(
                _food.Category, out var cm) ? cm : ("#F5F5F5", "🍽️");
            IconFrame.BackgroundColor = Color.FromArgb(bgColor);
            IconLabel.Text = icon;
            IconFrame.IsVisible = true;
            HeroImage.IsVisible = false;
        }

        // ── Text / nutrition ──────────────────────────────────────────────
        FoodNameLabel.Text = _food.Name;
        CategoryLabel.Text = _food.Category;
        CaloriesValueLabel.Text = $"{_food.Calories:F0}";
        KjLabel.Text = $"{_food.Calories * 4.184:F0}";
        ProteinValueLabel.Text = $"{_food.Protein:F1}";
        FatValueLabel.Text = $"{_food.Fat:F1}";
        SugarValueLabel.Text = $"{_food.Sugar:F1}";
        TtsPreviewLabel.Text = BuildSpeechText();

        // ── Ingredients ───────────────────────────────────────────────────
        BuildIngredientsView();
    }

    /// <summary>
    /// Build ingredient chips and highlight allergens
    /// </summary>
    private void BuildIngredientsView()
    {
        if (string.IsNullOrWhiteSpace(_food.Ingredients))
        {
            IngredientsCard.IsVisible = false;
            return;
        }

        IngredientsCard.IsVisible = true;
        IngredientsLayout.Children.Clear();

        bool peanutAlert = Preferences.Default.Get("allergen_peanut", false);
        bool glutenAlert = Preferences.Default.Get("allergen_gluten", false);
        bool lactoseAlert = Preferences.Default.Get("allergen_lactose", false);
        string savedCustom = Preferences.Default.Get("custom_allergens", "");
        var customAllergens = string.IsNullOrEmpty(savedCustom)
            ? []
            : savedCustom.Split(',').Where(s => !string.IsNullOrEmpty(s)).ToList();

        bool hasAllergenMatch = false;

        var ingredients = _food.Ingredients
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s));

        foreach (var ingredient in ingredients)
        {
            bool isAllergen = CheckIngredientAllergen(
                ingredient, peanutAlert, glutenAlert, lactoseAlert, customAllergens);

            if (isAllergen) hasAllergenMatch = true;

            var chip = new Frame
            {
                BackgroundColor = isAllergen
                    ? Color.FromArgb("#FFEBEE")
                    : Color.FromArgb("#F0F0F0"),
                CornerRadius = 16,
                Padding = new Thickness(12, 6),
                BorderColor = isAllergen
                    ? Color.FromArgb("#FFCDD2")
                    : Colors.Transparent,
                Margin = new Thickness(4, 4),
                HasShadow = false
            };

            var stack = new HorizontalStackLayout { Spacing = 4 };

            if (isAllergen)
            {
                stack.Add(new Label
                {
                    Text = "⚠️",
                    FontSize = 12,
                    VerticalOptions = LayoutOptions.Center
                });
            }

            stack.Add(new Label
            {
                Text = ingredient,
                FontSize = 13,
                FontAttributes = isAllergen ? FontAttributes.Bold : FontAttributes.None,
                TextColor = isAllergen
                    ? Color.FromArgb("#D32F2F")
                    : Color.FromArgb("#555555"),
                VerticalOptions = LayoutOptions.Center
            });

            chip.Content = stack;
            IngredientsLayout.Children.Add(chip);
        }

        AllergenHintLabel.IsVisible = hasAllergenMatch;
    }

    /// <summary>
    /// Check whether an ingredient matches enabled allergen filters
    /// </summary>
    private static bool CheckIngredientAllergen(
    string ingredient,
    bool peanutAlert, bool glutenAlert, bool lactoseAlert,
    List<string> customAllergens)
    {
        if (string.IsNullOrWhiteSpace(ingredient))
            return false;

        customAllergens ??= [];

        if (peanutAlert &&
            (ingredient.Contains("peanut", StringComparison.OrdinalIgnoreCase) ||
             ingredient.Contains("nut", StringComparison.OrdinalIgnoreCase)))
            return true;

        if (glutenAlert &&
            (ingredient.Contains("wheat", StringComparison.OrdinalIgnoreCase) ||
             ingredient.Contains("flour", StringComparison.OrdinalIgnoreCase) ||
             ingredient.Contains("bread", StringComparison.OrdinalIgnoreCase) ||
             ingredient.Contains("pasta", StringComparison.OrdinalIgnoreCase) ||
             ingredient.Contains("noodle", StringComparison.OrdinalIgnoreCase) ||
             ingredient.Contains("gluten", StringComparison.OrdinalIgnoreCase)))
            return true;

        if (lactoseAlert &&
            (ingredient.Contains("milk", StringComparison.OrdinalIgnoreCase) ||
             ingredient.Contains("cheese", StringComparison.OrdinalIgnoreCase) ||
             ingredient.Contains("cream", StringComparison.OrdinalIgnoreCase) ||
             ingredient.Contains("butter", StringComparison.OrdinalIgnoreCase) ||
             ingredient.Contains("dairy", StringComparison.OrdinalIgnoreCase) ||
             ingredient.Contains("yogurt", StringComparison.OrdinalIgnoreCase) ||
             ingredient.Contains("lactose", StringComparison.OrdinalIgnoreCase)))
            return true;

        foreach (var allergen in customAllergens)
        {
            if (!string.IsNullOrWhiteSpace(allergen) &&
                ingredient.Contains(allergen, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Build text-to-speech content for the current food item
    /// </summary>
    private string BuildSpeechText()
    {
        string text = $"{_food.Name}. Per 100 grams: " +
                      $"{_food.Calories:F0} calories, " +
                      $"{_food.Protein:F1} grams of protein, " +
                      $"{_food.Fat:F1} grams of fat, " +
                      $"and {_food.Sugar:F1} grams of sugar.";

        if (!string.IsNullOrWhiteSpace(_food.Ingredients))
            text += $" Ingredients include: {_food.Ingredients}.";

        return text;
    }

    /// <summary>
    /// Read food information aloud using text-to-speech
    /// </summary>
    private async void OnSpeakClicked(object sender, EventArgs e)
    {
        if (_isSpeaking) return;

        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        _isSpeaking = true;
        SpeakButton.IsEnabled = false;
        SpeakButton.BackgroundColor = Color.FromArgb("#BDBDBD");
        StopButton.IsEnabled = true;
        StopButton.BackgroundColor = Color.FromArgb("#F44336");

        try
        {
            var options = new SpeechOptions { Volume = 1.0f, Pitch = 1.0f };
            await TextToSpeech.Default.SpeakAsync(
                BuildSpeechText(), options, _cts.Token);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TTS error: {ex.Message}");
        }
        finally
        {
            ResetTtsButtons();
        }
    }

    /// <summary>
    /// Stop the current text-to-speech playback
    /// </summary>
    private void OnStopClicked(object sender, EventArgs e)
    {
        _cts?.Cancel();
        ResetTtsButtons();
    }

    /// <summary>
    /// Reset text-to-speech control button states
    /// </summary>
    private void ResetTtsButtons()
    {
        _isSpeaking = false;
        SpeakButton.IsEnabled = true;
        SpeakButton.BackgroundColor = Color.FromArgb("#FF9800");
        StopButton.IsEnabled = false;
        StopButton.BackgroundColor = Color.FromArgb("#9E9E9E");
    }

    /// <summary>
    /// Cancel speech resources when leaving the page
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cts?.Cancel();
        _cts?.Dispose();
    }

    /// <summary>
    /// Close the detail page and stop any active speech
    /// </summary>
    private async void OnCloseClicked(object sender, EventArgs e)
    {
        _cts?.Cancel();
        await Navigation.PopAsync();
    }
}