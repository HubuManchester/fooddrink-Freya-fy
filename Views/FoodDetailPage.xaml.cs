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

    private void PopulateUI()
    {
        // Hero
        string heroColor = HeroColors.TryGetValue(_food.Category, out var hc)
            ? hc : "#4CAF50";
        HeroGrid.BackgroundColor = Color.FromArgb(heroColor);

        var (bgColor, icon) = CategoryMap.TryGetValue(
            _food.Category, out var cm) ? cm : ("#F5F5F5", "🍽️");
        IconFrame.BackgroundColor = Color.FromArgb(bgColor);
        IconLabel.Text = icon;

        FoodNameLabel.Text = _food.Name;
        CategoryLabel.Text = _food.Category;
        CaloriesValueLabel.Text = $"{_food.Calories:F0}";
        KjLabel.Text = $"{_food.Calories * 4.184:F0}";
        ProteinValueLabel.Text = $"{_food.Protein:F1}";
        FatValueLabel.Text = $"{_food.Fat:F1}";
        SugarValueLabel.Text = $"{_food.Sugar:F1}";
        TtsPreviewLabel.Text = BuildSpeechText();

        // Ingredients
        BuildIngredientsView();
    }

    private void BuildIngredientsView()
    {
        if (string.IsNullOrWhiteSpace(_food.Ingredients))
        {
            IngredientsCard.IsVisible = false;
            return;
        }

        IngredientsCard.IsVisible = true;
        IngredientsLayout.Children.Clear();

        // Load allergen settings
        bool peanutAlert = Preferences.Default.Get("allergen_peanut", false);
        bool glutenAlert = Preferences.Default.Get("allergen_gluten", false);
        bool lactoseAlert = Preferences.Default.Get("allergen_lactose", false);
        string savedCustom = Preferences.Default.Get("custom_allergens", "");
        var customAllergens = string.IsNullOrEmpty(savedCustom)
            ? new List<string>()
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
                    : Color.FromArgb("{AppThemeBinding Light=#F5F5F5, Dark=#2C2C2C}"),
                CornerRadius = 16,
                Padding = new Thickness(12, 6),
                BorderColor = isAllergen
                    ? Color.FromArgb("#FFCDD2")
                    : Colors.Transparent,
                Margin = new Thickness(4, 4),
                HasShadow = false
            };

            // Simpler approach for theme compatibility
            chip.BackgroundColor = isAllergen
                ? Color.FromArgb("#FFEBEE")
                : Color.FromArgb("#F0F0F0");

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

    private static bool CheckIngredientAllergen(
        string ingredient,
        bool peanutAlert, bool glutenAlert, bool lactoseAlert,
        List<string> customAllergens)
    {
        string lower = ingredient.ToLower();

        if (peanutAlert &&
            (lower.Contains("peanut") || lower.Contains("nut")))
            return true;

        if (glutenAlert &&
            (lower.Contains("wheat") || lower.Contains("flour") ||
             lower.Contains("bread") || lower.Contains("pasta") ||
             lower.Contains("noodle") || lower.Contains("gluten")))
            return true;

        if (lactoseAlert &&
            (lower.Contains("milk") || lower.Contains("cheese") ||
             lower.Contains("cream") || lower.Contains("butter") ||
             lower.Contains("dairy") || lower.Contains("yogurt") ||
             lower.Contains("lactose")))
            return true;

        foreach (var allergen in customAllergens)
            if (!string.IsNullOrEmpty(allergen) &&
                lower.Contains(allergen.ToLower()))
                return true;

        return false;
    }

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

    private void OnStopClicked(object sender, EventArgs e)
    {
        _cts?.Cancel();
        ResetTtsButtons();
    }

    private void ResetTtsButtons()
    {
        _isSpeaking = false;
        SpeakButton.IsEnabled = true;
        SpeakButton.BackgroundColor = Color.FromArgb("#FF9800");
        StopButton.IsEnabled = false;
        StopButton.BackgroundColor = Color.FromArgb("#9E9E9E");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cts?.Cancel();
        _cts?.Dispose();
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        _cts?.Cancel();
        await Navigation.PopAsync();
    }
}