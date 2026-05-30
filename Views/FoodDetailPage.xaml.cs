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
    }

    private string BuildSpeechText()
    {
        return $"{_food.Name}. Per 100 grams: " +
               $"{_food.Calories:F0} calories, " +
               $"{_food.Protein:F1} grams of protein, " +
               $"{_food.Fat:F1} grams of fat, " +
               $"and {_food.Sugar:F1} grams of sugar.";
    }

    private async void OnSpeakClicked(object sender, EventArgs e)
    {
        if (_isSpeaking) return;

        // Create a new CancellationTokenSource for this session
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        _isSpeaking = true;
        SpeakButton.IsEnabled = false;
        SpeakButton.BackgroundColor = Color.FromArgb("#BDBDBD");
        StopButton.IsEnabled = true;
        StopButton.BackgroundColor = Color.FromArgb("#F44336");

        try
        {
            // Pass CancellationToken via SpeechOptions overload
            var options = new SpeechOptions
            {
                Volume = 1.0f,
                Pitch = 1.0f
            };

            await TextToSpeech.Default.SpeakAsync(
                BuildSpeechText(), options, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Cancelled by user - expected, no error needed
        }
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
        // Cancel the token - this stops SpeakAsync mid-speech
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