using NutriLens.Models;
using NutriLens.Services;

namespace NutriLens.Views;

public partial class HomePage : ContentPage
{
    private int _currentWaterMl = 0;
    private int _targetWaterMl = 2000;
    private int _currentCalories = 0;
    private int _targetCalories = 2000;
    private DateTime _lastShakeTime = DateTime.MinValue;
    private AccelerometerData _lastAccelData;

    private readonly DatabaseService _databaseService;

    public HomePage(DatabaseService databaseService)
    {
        InitializeComponent();
        _databaseService = databaseService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _targetCalories = int.Parse(
            Preferences.Default.Get("calorie_target", "2000"));
        _targetWaterMl = int.Parse(
            Preferences.Default.Get("water_target", "2000"));

        await LoadTodayMealsAsync();
        UpdateWaterDisplay();
        StartShakeDetection();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopShakeDetection();
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
            System.Diagnostics.Debug.WriteLine($"Error loading meals: {ex.Message}");
        }
    }

    /// <summary>
    /// Start accelerometer shake detection
    /// </summary>
    private void StartShakeDetection()
    {
        try
        {
            if (!Accelerometer.Default.IsSupported) return;

            // Remove first to avoid duplicate handlers
            Accelerometer.Default.ReadingChanged -= OnAccelerometerReadingChanged;
            Accelerometer.Default.ReadingChanged += OnAccelerometerReadingChanged;

            if (!Accelerometer.Default.IsMonitoring)
                Accelerometer.Default.Start(SensorSpeed.Game);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Accelerometer start error: {ex.Message}");
        }
    }

    /// <summary>
    /// Stop accelerometer to save battery
    /// </summary>
    private void StopShakeDetection()
    {
        try
        {
            if (!Accelerometer.Default.IsSupported) return;

            Accelerometer.Default.ReadingChanged -= OnAccelerometerReadingChanged;

            if (Accelerometer.Default.IsMonitoring)
                Accelerometer.Default.Stop();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Accelerometer stop error: {ex.Message}");
        }
    }

    /// <summary>
    /// Detect shake by measuring acceleration delta
    /// </summary>
    private async void OnAccelerometerReadingChanged(
    object? sender, AccelerometerChangedEventArgs e)
    {
        var data = e.Reading;

        double delta =
            Math.Abs(data.Acceleration.X - _lastAccelData.Acceleration.X) +
            Math.Abs(data.Acceleration.Y - _lastAccelData.Acceleration.Y) +
            Math.Abs(data.Acceleration.Z - _lastAccelData.Acceleration.Z);

        _lastAccelData = data;

        if (delta > 3.5 && (DateTime.Now - _lastShakeTime).TotalSeconds > 2)
        {
            _lastShakeTime = DateTime.Now;

            try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(300)); }
            catch { }

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var page = new MealSuggestionPage();
                await Navigation.PushModalAsync(page);
            });
        }
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

        return meals[new Random().Next(meals.Length)];
    }

    /// <summary>
    /// Add 250ml water and update display
    /// </summary>
    private async void OnAddWaterClicked(object sender, EventArgs e)
    {
        _currentWaterMl += 250;
        UpdateWaterDisplay();

        try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(100)); }
        catch { }

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
        WaterProgress.Progress = Math.Min(
            (double)_currentWaterMl / _targetWaterMl, 1.0);
    }

    /// <summary>
    /// Update calories progress bar and label
    /// </summary>
    private void UpdateCaloriesDisplay()
    {
        CaloriesLabel.Text = $"{_currentCalories} / {_targetCalories} kcal";
        CaloriesProgress.Progress = Math.Min(
            (double)_currentCalories / _targetCalories, 1.0);
    }

    /// <summary>
    /// Calculate and display health score
    /// </summary>
    private void UpdateHealthScore()
    {
        if (_currentCalories == 0)
        {
            HealthScoreLabel.Text = "- / 10";
            HealthScoreLabel.TextColor = Colors.Gray;
            HealthAdviceLabel.Text = "Log your meals to get a health score";
            return;
        }

        var advice = new List<string>();

// ── Calorie score (0~7) ──────────────────────────────────────────
double calRatio = (double)_currentCalories / _targetCalories;

double calorieScore =
    Math.Max(0,
        7 - Math.Abs(calRatio - 1.0) * 7);

if (calRatio < 0.5)
{
    advice.Add("Very few calories logged today");
}
else if (calRatio > 1.2)
{
    advice.Add("Calories above target");
}

// ── Water score (0~3) ────────────────────────────────────────────
double waterRatio =
    (double)_currentWaterMl / _targetWaterMl;

double waterScore =
    Math.Min(waterRatio, 1.0) * 3;

if (waterRatio < 0.5)
{
    advice.Add("Drink more water");
}
else if (waterRatio < 1.0)
{
    advice.Add("Keep hydrating");
}

// ── Final score ──────────────────────────────────────────────────
int score = (int)Math.Round(
    calorieScore + waterScore);

score = Math.Clamp(score, 0, 10);

        HealthScoreLabel.Text = $"{score} / 10";

        if (score >= 9)
        {
            HealthScoreLabel.TextColor = Color.FromArgb("#2E7D32");
            HealthAdviceLabel.Text = "Excellent! Perfect balance today 🏆";
        }
        else if (score >= 7)
        {
            HealthScoreLabel.TextColor = Color.FromArgb("#388E3C");
            HealthAdviceLabel.Text = advice.Any()
                ? string.Join(" · ", advice)
                : "Good job! Keep it up 😊";
        }
        else if (score >= 5)
        {
            HealthScoreLabel.TextColor = Colors.Orange;
            HealthAdviceLabel.Text = string.Join(" · ", advice);
        }
        else if (score >= 3)
        {
            HealthScoreLabel.TextColor = Color.FromArgb("#E65100");
            HealthAdviceLabel.Text = string.Join(" · ", advice) + " 😟";
        }
        else
        {
            HealthScoreLabel.TextColor = Colors.Red;
            HealthAdviceLabel.Text = string.Join(" · ", advice) + " 😰";
        }
    }

    /// <summary>
    /// Navigate to Diary page (scanner is now inside Diary)
    /// </summary>
    private async void OnScanClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//DiaryPage");
    }

    /// <summary>
    /// Navigate to Diary page
    /// </summary>
    private async void OnDiaryClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//DiaryPage");
    }
}