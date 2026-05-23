using System.Text.Json;
using NutriLens.Models;
using NutriLens.Services;

namespace NutriLens.Views;

/// <summary>
/// Scanner page - handles photo capture, barcode scanning,
/// nutrition display, allergen warnings and TTS
/// </summary>
public partial class ScannerPage : ContentPage
{
    // Current scanned food data
    private string _currentFoodName = "";
    private double _currentCalories = 0;
    private double _currentProtein = 0;
    private double _currentFat = 0;
    private double _currentSugar = 0;

    // Services
    private readonly HttpClient _httpClient = new HttpClient();
    private readonly DatabaseService _databaseService;

    public ScannerPage(DatabaseService databaseService)
    {
        InitializeComponent();
        _databaseService = databaseService;
        LoadAllergenSettings();
    }

    // Allergen settings loaded from database
    private bool _peanutAlert = false;
    private bool _glutenAlert = false;
    private bool _lactoseAlert = false;

    /// <summary>
    /// Load allergen settings from user preferences
    /// </summary>
    private void LoadAllergenSettings()
    {
        _peanutAlert = Preferences.Default.Get("allergen_peanut", false);
        _glutenAlert = Preferences.Default.Get("allergen_gluten", false);
        _lactoseAlert = Preferences.Default.Get("allergen_lactose", false);
    }

    /// <summary>
    /// Reload allergen settings every time page appears
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadAllergenSettings();
    }

    /// <summary>
    /// Switch to Photo tab
    /// </summary>
    private void OnPhotoTabClicked(object sender, EventArgs e)
    {
        PhotoPanel.IsVisible = true;
        BarcodePanel.IsVisible = false;
        PhotoTabButton.BackgroundColor = Colors.Green;
        BarcodeTabButton.BackgroundColor = Color.FromArgb("#E0E0E0");
        BarcodeTabButton.TextColor = Colors.Black;
    }

    /// <summary>
    /// Switch to Barcode tab
    /// </summary>
    private void OnBarcodeTabClicked(object sender, EventArgs e)
    {
        PhotoPanel.IsVisible = false;
        BarcodePanel.IsVisible = true;
        BarcodeTabButton.BackgroundColor = Colors.Blue;
        BarcodeTabButton.TextColor = Colors.White;
        PhotoTabButton.BackgroundColor = Color.FromArgb("#E0E0E0");
        PhotoTabButton.TextColor = Colors.Black;
    }

    /// <summary>
    /// Take photo using device camera and identify food using Clarifai API
    /// </summary>
    private async void OnTakePhotoClicked(object sender, EventArgs e)
    {
        try
        {
            // Check camera permission
            var status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Permission Denied",
                    "Camera permission is required to scan food.", "OK");
                return;
            }

            // Capture photo
            var photo = await MediaPicker.CapturePhotoAsync();
            if (photo == null) return;

            // Show loading
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            ResultFrame.IsVisible = false;

            // Display captured image
            FoodImage.Source = ImageSource.FromFile(photo.FullPath);
            FoodImage.IsVisible = true;

            // Convert to base64 for API
            var stream = await photo.OpenReadAsync();
            var bytes = new byte[stream.Length];
            await stream.ReadAsync(bytes, 0, (int)stream.Length);
            string base64Image = Convert.ToBase64String(bytes);

            // Identify food using Clarifai
            string foodName = await IdentifyFoodAsync(base64Image);

            if (!string.IsNullOrEmpty(foodName))
            {
                await FetchNutritionDataAsync(foodName);
            }
            else
            {
                await DisplayAlert("Not Recognised",
                    "Could not identify the food. Please try again.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error",
                $"Failed to capture photo: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    /// <summary>
    /// Identify food from base64 image using Clarifai API
    /// </summary>
    private async Task<string> IdentifyFoodAsync(string base64Image)
    {
        try
        {
            var requestBody = new
            {
                user_app_id = new
                {
                    user_id = "clarifai",
                    app_id = "main"
                },
                inputs = new[]
                {
                    new
                    {
                        data = new
                        {
                            image = new { base64 = base64Image }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json,
                System.Text.Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add(
                "Authorization", "Key YOUR_CLARIFAI_API_KEY");

            var response = await _httpClient.PostAsync(
                "https://api.clarifai.com/v2/models/food-item-recognition/versions/1d5fd481165a4f8bab4b27d44bce8ad5/outputs",
                content);

            if (!response.IsSuccessStatusCode) return "";

            var responseJson = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(responseJson);

            var concepts = doc.RootElement
                .GetProperty("outputs")[0]
                .GetProperty("data")
                .GetProperty("concepts");

            return concepts[0].GetProperty("name").GetString() ?? "";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Clarifai API error: {ex.Message}");
            return "";
        }
    }

    /// <summary>
    /// Search nutrition by manually entered barcode
    /// </summary>
    private async void OnManualSearchClicked(object sender, EventArgs e)
    {
        string barcode = ManualBarcodeEntry.Text?.Trim() ?? "";

        // Validate input
        if (string.IsNullOrEmpty(barcode))
        {
            await DisplayAlert("Validation Error",
                "Please enter a barcode number.", "OK");
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
            return;
        }

        if (barcode.Length < 8)
        {
            await DisplayAlert("Validation Error",
                "Barcode must be at least 8 digits.", "OK");
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
            return;
        }

        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;

        await FetchNutritionByBarcodeAsync(barcode);

        LoadingIndicator.IsVisible = false;
        LoadingIndicator.IsRunning = false;
    }

    /// <summary>
    /// Scan barcode using device camera
    /// </summary>
    private async void OnScanBarcodeClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Barcode Scanner",
            "Point camera at barcode to scan.", "OK");
    }

    /// <summary>
    /// Fetch nutrition data by barcode from Open Food Facts API
    /// </summary>
    private async Task FetchNutritionByBarcodeAsync(string barcode)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"https://world.openfoodfacts.org/api/v0/product/{barcode}.json");

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlert("Error",
                    "Could not connect to nutrition database.", "OK");
                return;
            }

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            int status = doc.RootElement.GetProperty("status").GetInt32();
            if (status == 0)
            {
                await DisplayAlert("Not Found",
                    "Product not found in database.", "OK");
                return;
            }

            var product = doc.RootElement.GetProperty("product");
            string name = product.TryGetProperty("product_name",
                out var nameEl) ? nameEl.GetString() ?? "Unknown" : "Unknown";

            var nutriments = product.GetProperty("nutriments");
            double calories = nutriments.TryGetProperty("energy-kcal_100g",
                out var cal) ? cal.GetDouble() : 0;
            double protein = nutriments.TryGetProperty("proteins_100g",
                out var pro) ? pro.GetDouble() : 0;
            double fat = nutriments.TryGetProperty("fat_100g",
                out var f) ? f.GetDouble() : 0;
            double sugar = nutriments.TryGetProperty("sugars_100g",
                out var sug) ? sug.GetDouble() : 0;

            DisplayNutrition(name, calories, protein, fat, sugar);
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(300));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error",
                $"Network error: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Fetch nutrition data by food name from Open Food Facts API
    /// </summary>
    private async Task FetchNutritionDataAsync(string foodName)
    {
        try
        {
            var url = $"https://world.openfoodfacts.org/cgi/search.pl?" +
                      $"search_terms={Uri.EscapeDataString(foodName)}" +
                      $"&search_simple=1&action=process&json=1&page_size=1";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlert("Error",
                    "Could not connect to nutrition database.", "OK");
                return;
            }

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var products = doc.RootElement.GetProperty("products");

            if (products.GetArrayLength() == 0)
            {
                await DisplayAlert("Not Found",
                    $"No nutrition data found for {foodName}.", "OK");
                return;
            }

            var product = products[0];
            string name = product.TryGetProperty("product_name",
                out var nameEl) ? nameEl.GetString() ?? foodName : foodName;

            var nutriments = product.GetProperty("nutriments");
            double calories = nutriments.TryGetProperty("energy-kcal_100g",
                out var cal) ? cal.GetDouble() : 0;
            double protein = nutriments.TryGetProperty("proteins_100g",
                out var pro) ? pro.GetDouble() : 0;
            double fat = nutriments.TryGetProperty("fat_100g",
                out var f) ? f.GetDouble() : 0;
            double sugar = nutriments.TryGetProperty("sugars_100g",
                out var sug) ? sug.GetDouble() : 0;

            DisplayNutrition(name, calories, protein, fat, sugar);
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(300));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error",
                $"Network error: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Display nutrition information and check allergens
    /// </summary>
    private void DisplayNutrition(string name, double calories,
        double protein, double fat, double sugar)
    {
        _currentFoodName = name;
        _currentCalories = calories;
        _currentProtein = protein;
        _currentFat = fat;
        _currentSugar = sugar;

        FoodNameLabel.Text = name;
        CaloriesLabel.Text = $"{calories:F1} kcal";
        ProteinLabel.Text = $"{protein:F1}g";
        FatLabel.Text = $"{fat:F1}g";
        SugarLabel.Text = $"{sugar:F1}g";

        CheckAllergens(name);
        ResultFrame.IsVisible = true;
    }

    /// <summary>
    /// Check food against user allergen settings
    /// </summary>
    private void CheckAllergens(string foodName)
    {
        var warnings = new List<string>();
        string nameLower = foodName.ToLower();

        if (_peanutAlert &&
            (nameLower.Contains("peanut") || nameLower.Contains("nut")))
            warnings.Add("⚠️ Contains Peanuts");

        if (_glutenAlert &&
            (nameLower.Contains("wheat") || nameLower.Contains("bread") ||
             nameLower.Contains("pasta") || nameLower.Contains("flour")))
            warnings.Add("⚠️ Contains Gluten");

        if (_lactoseAlert &&
            (nameLower.Contains("milk") || nameLower.Contains("cheese") ||
             nameLower.Contains("dairy") || nameLower.Contains("yogurt")))
            warnings.Add("⚠️ Contains Lactose");

        if (warnings.Count > 0)
        {
            AllergenLabel.Text = string.Join("\n", warnings);
            AllergenFrame.IsVisible = true;
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(800));
        }
        else
        {
            AllergenFrame.IsVisible = false;
        }
    }

    /// <summary>
    /// Read nutrition information aloud using Text-to-Speech
    /// </summary>
    private async void OnReadAloudClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentFoodName))
        {
            await DisplayAlert("No Data",
                "Please scan a food item first.", "OK");
            return;
        }

        string text = $"{_currentFoodName} contains " +
                      $"{_currentCalories:F0} calories, " +
                      $"{_currentProtein:F1} grams of protein, " +
                      $"{_currentFat:F1} grams of fat, " +
                      $"and {_currentSugar:F1} grams of sugar.";

        await TextToSpeech.Default.SpeakAsync(text);
    }

    /// <summary>
    /// Save food as breakfast entry to database
    /// </summary>
    private async void OnSaveBreakfastClicked(object sender, EventArgs e)
    {
        await SaveFoodEntry("Breakfast");
    }

    /// <summary>
    /// Save food as lunch entry to database
    /// </summary>
    private async void OnSaveLunchClicked(object sender, EventArgs e)
    {
        await SaveFoodEntry("Lunch");
    }

    /// <summary>
    /// Save food as dinner entry to database
    /// </summary>
    private async void OnSaveDinnerClicked(object sender, EventArgs e)
    {
        await SaveFoodEntry("Dinner");
    }

    /// <summary>
    /// Save food entry to SQLite diary database
    /// </summary>
    private async Task SaveFoodEntry(string mealType)
    {
        if (string.IsNullOrEmpty(_currentFoodName))
        {
            await DisplayAlert("No Data",
                "Please scan a food item first.", "OK");
            return;
        }

        try
        {
            var entry = new DiaryEntry
            {
                FoodName = _currentFoodName,
                MealType = mealType,
                Calories = _currentCalories,
                Protein = _currentProtein,
                Fat = _currentFat,
                Sugar = _currentSugar,
                Date = DateTime.Now
            };

            bool success = await _databaseService.SaveEntryAsync(entry);

            if (success)
            {
                await DisplayAlert("Saved! ✅",
                    $"{_currentFoodName} saved to {mealType}.", "OK");
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
            }
            else
            {
                await DisplayAlert("Error",
                    "Failed to save entry. Please try again.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error",
                $"Failed to save: {ex.Message}", "OK");
        }
    }
}