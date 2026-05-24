using NutriLens.Models;
using NutriLens.Services;

namespace NutriLens.Views;

/// <summary>
/// Home page - displays daily summary, health score, water intake and quick actions
/// </summary>
public partial class HomePage : ContentPage
{
    private int _currentWaterMl = 0;
    private int _targetWaterMl = 2000;
    private int _currentCalories = 0;
    private int _targetCalories = 2000;

    private readonly DatabaseService _databaseService;

    public HomePage(DatabaseService databaseService)
    {
        InitializeComponent();
        _databaseService = databaseService;
        StartShakeDetection();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Load targets from settings
        _targetCalories = int.Parse(
            Preferences.Default.Get("calorie_target", "2000"));
        _targetWaterMl = int.Parse(
            Preferences.Default.Get("water_target", "2000"));

        await LoadTodayMealsAsync();
        UpdateWaterDisplay();
    }

    /// <summary>
    /// Load today's meals from database and update home page
    /// </summary>
    private async Task LoadTodayMealsAsync()
    {
        try
        {
            var entries = await _databaseService.GetTodayEntriesAsync();

            var breakfast = entries.Where(e => e.MealType == "Breakfast").ToList();
            var lunch = entries.Where(e => e.MealType == "Lunch").ToList();
            var dinner = entries.Where(e => e.MealType == "Dinner").ToList();

            BreakfastLabel.Text = breakfast.Any()
                ? string.Join(", ", breakfast.Select(e => e.FoodName))
                : "No entries yet";

            LunchLabel.Text = lunch.Any()
                ? string.Join(", ", lunch.Select(e => e.FoodName))
                : "No entries yet";

            DinnerLabel.Text = dinner.Any()
                ? string.Join(", ", dinner.Select(e => e.FoodName))
                : "No entries yet";

            _currentCalories = (int)entries.Sum(e => e.Calories);
            UpdateCaloriesDisplay();
            UpdateHealthScore();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Error loading meals: {ex.Message}");
        }
    }

    /// <summary>
    /// Start listening for shake gesture using accelerometer
    /// </summary>
    private void StartShakeDetection()
    {
        try
        {
            if (Accelerometer.Default.IsSupported)
            {
                Accelerometer.Default.ShakeDetected += OnShakeDetected;
                Accelerometer.Default.Start(SensorSpeed.Game);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Accelerometer error: {ex.Message}");
        }
    }

    /// <summary>
    /// Stop accelerometer when page disappears to save battery
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        try
        {
            if (Accelerometer.Default.IsSupported)
            {
                Accelerometer.Default.ShakeDetected -= OnShakeDetected;
                Accelerometer.Default.Stop();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Accelerometer stop error: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle shake gesture - show random meal suggestion
    /// </summary>
    private async void OnShakeDetected(object? sender, EventArgs e)
    {
        try
        {
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(300));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Vibration error: {ex.Message}");
        }

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            string meal = GetRandomMeal();
            await DisplayAlert("🎲 Random Meal Suggestion", meal, "OK");
        });
    }

    /// <summary>
    /// Returns a random healthy meal suggestion
    /// </summary>
    private string GetRandomMeal()
    {
        var meals = new[]
        {
            "🥗 Greek Salad with grilled chicken - 350 kcal",
            "🍜 Vegetable stir fry with brown rice - 420 kcal",
            "🥙 Wholemeal wrap with tuna and salad - 380 kcal",
            "🍳 Scrambled eggs with avocado toast - 450 kcal",
            "🥣 Oat porridge with berries and honey - 320 kcal",
            "🍱 Salmon with steamed broccoli and quinoa - 480 kcal",
            "🥘 Lentil soup with wholegrain bread - 390 kcal",
            "🌮 Black bean tacos with fresh salsa - 410 kcal"
        };

        var random = new Random();
        return meals[random.Next(meals.Length)];
    }

    /// <summary>
    /// Add 250ml water and update display
    /// </summary>
    private async void OnAddWaterClicked(object sender, EventArgs e)
    {
        _currentWaterMl += 250;
        UpdateWaterDisplay();

        try
        {
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(100));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Vibration error: {ex.Message}");
        }

        if (_currentWaterMl == _targetWaterMl)
        {
            await DisplayAlert("🎉 Goal Reached!",
                "You have reached your daily water intake goal!", "Great!");
        }
        else if (_currentWaterMl > _targetWaterMl)
        {
            await DisplayAlert("💧 Over Target",
                $"You have exceeded your daily water goal by " +
                $"{_currentWaterMl - _targetWaterMl}ml", "OK");
        }

        UpdateHealthScore();
    }

    /// <summary>
    /// Update water intake progress bar and label
    /// </summary>
    private void UpdateWaterDisplay()
    {
        WaterLabel.Text = $"{_currentWaterMl} / {_targetWaterMl} ml";
        double progress = Math.Min(
            (double)_currentWaterMl / _targetWaterMl, 1.0);
        WaterProgress.Progress = progress;
    }

    /// <summary>
    /// Update calories progress bar and label
    /// </summary>
    private void UpdateCaloriesDisplay()
    {
        CaloriesLabel.Text = $"{_currentCalories} / {_targetCalories} kcal";
        double progress = Math.Min(
            (double)_currentCalories / _targetCalories, 1.0);
        CaloriesProgress.Progress = progress;
    }

    /// <summary>
    /// Calculate and display health score based on nutrition intake
    /// </summary>
    private void UpdateHealthScore()
    {
        // No food logged yet
        if (_currentCalories == 0)
        {
            HealthScoreLabel.Text = "- / 10";
            HealthScoreLabel.TextColor = Colors.Gray;
            HealthAdviceLabel.Text = "Start scanning food to get your score";
            return;
        }

        int score = 10;
        string advice = "";

        if (_currentCalories > _targetCalories * 1.2)
        {
            score -= 2;
            advice += "Calories too high. ";
        }

        if (_currentWaterMl < _targetWaterMl * 0.5)
        {
            score -= 2;
            advice += "Drink more water. ";
        }

        HealthScoreLabel.Text = $"{score} / 10";

        if (score >= 8)
        {
            HealthScoreLabel.TextColor = Colors.Green;
            HealthAdviceLabel.Text = "Great job! Keep it up 😊";
        }
        else if (score >= 5)
        {
            HealthScoreLabel.TextColor = Colors.Orange;
            HealthAdviceLabel.Text = advice.Trim();
        }
        else
        {
            HealthScoreLabel.TextColor = Colors.Red;
            HealthAdviceLabel.Text = advice.Trim() + " 😟";
        }
    }

    /// <summary>
    /// Navigate to Scanner page
    /// </summary>
    private async void OnScanClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ScannerPage");
    }

    /// <summary>
    /// Navigate to Diary page
    /// </summary>
    private async void OnDiaryClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//DiaryPage");
    }
}