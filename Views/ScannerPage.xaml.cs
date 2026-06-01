using System.Text.Json;
using NutriLens.Models;
using NutriLens.Services;

namespace NutriLens.Views;

public partial class ScannerPage : ContentPage
{
    private string _currentFoodName = "";
    private double _currentCalories = 0;
    private double _currentProtein = 0;
    private double _currentFat = 0;
    private double _currentSugar = 0;

    private const string QwenApiKey = "sk-a34faf314c1744bd92dc2ddc3559de58";

    private readonly HttpClient _httpClient = new HttpClient();
    private readonly DatabaseService _databaseService;

    private bool _peanutAlert = false;
    private bool _glutenAlert = false;
    private bool _lactoseAlert = false;
    private List<string> _customAllergens = new List<string>();

    public ScannerPage(DatabaseService databaseService)
    {
        InitializeComponent();
        _databaseService = databaseService;
        LoadAllergenSettings();
    }

    private void LoadAllergenSettings()
    {
        _peanutAlert = Preferences.Default.Get("allergen_peanut", false);
        _glutenAlert = Preferences.Default.Get("allergen_gluten", false);
        _lactoseAlert = Preferences.Default.Get("allergen_lactose", false);

        string saved = Preferences.Default.Get("custom_allergens", "");
        if (!string.IsNullOrEmpty(saved))
            _customAllergens = saved.Split(',').ToList();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadAllergenSettings();
    }

    private void OnPhotoTabClicked(object sender, EventArgs e)
    {
        PhotoPanel.IsVisible = true;
        BarcodePanel.IsVisible = false;
        PhotoTabButton.BackgroundColor = Color.FromArgb("#4CAF50");
        PhotoTabButton.TextColor = Colors.White;
        BarcodeTabButton.BackgroundColor = Colors.Transparent;
        BarcodeTabButton.TextColor = Colors.Gray;
    }

    private void OnBarcodeTabClicked(object sender, EventArgs e)
    {
        PhotoPanel.IsVisible = false;
        BarcodePanel.IsVisible = true;
        BarcodeTabButton.BackgroundColor = Color.FromArgb("#2196F3");
        BarcodeTabButton.TextColor = Colors.White;
        PhotoTabButton.BackgroundColor = Colors.Transparent;
        PhotoTabButton.TextColor = Colors.Gray;
    }

    /// <summary>
    /// Take photo using device camera
    /// </summary>
    private async void OnTakePhotoClicked(object sender, EventArgs e)
    {
        try
        {
            var status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Permission Denied",
                    "Camera permission is required to scan food.", "OK");
                return;
            }

            var photo = await MediaPicker.CapturePhotoAsync();
            if (photo == null) return;

            await ProcessPhotoAsync(photo);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to capture photo: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Pick photo from device gallery
    /// </summary>
    private async void OnPickPhotoClicked(object sender, EventArgs e)
    {
        try
        {
            var photo = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Select a food photo"
            });

            if (photo == null) return;

            await ProcessPhotoAsync(photo);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to open gallery: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Shared: show image, call Qwen API, fetch nutrition
    /// </summary>
    private async Task ProcessPhotoAsync(FileResult photo)
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        ResultFrame.IsVisible = false;

        try
        {
            FoodImage.Source = ImageSource.FromFile(photo.FullPath);
            FoodImage.IsVisible = true;

            // Force layout update after image loads
            FoodImage.SizeChanged += OnFoodImageSizeChanged;

            using var stream = await photo.OpenReadAsync();
            var bytes = new byte[stream.Length];
            await stream.ReadAsync(bytes, 0, (int)stream.Length);
            string base64Image = Convert.ToBase64String(bytes);

            string foodName = await IdentifyFoodAsync(base64Image);

            if (!string.IsNullOrEmpty(foodName) && foodName != "unknown")
            {
                await FetchNutritionDataAsync(foodName);
            }
            else
            {
                await DisplayAlert("Not Recognised",
                    "Could not identify the food. Please try again or use barcode scan.",
                    "OK");
            }
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    private void OnFoodImageSizeChanged(object? sender, EventArgs e)
    {
        if (sender is not Image img) return;
        img.SizeChanged -= OnFoodImageSizeChanged;

        // Once image has actual size, remove any height constraints
        // so it displays at full natural height
        if (img.Height > 0)
        {
            FoodImage.HeightRequest = img.Height;
        }
    }


    /// <summary>
    /// Identify food from base64 image using Qwen Vision API
    /// </summary>
    private async Task<string> IdentifyFoodAsync(string base64Image)
    {
        try
        {
            var requestBody = new
            {
                model = "qwen-vl-plus",
                input = new
                {
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = new object[]
                            {
                                new { image = $"data:image/jpeg;base64,{base64Image}" },
                                new { text = "What food is in this image? Reply with only the food name in English, nothing else. If no food is visible, reply with 'unknown'." }
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {QwenApiKey}");

            var response = await _httpClient.PostAsync(
                "https://dashscope.aliyuncs.com/api/v1/services/aigc/multimodal-generation/generation",
                content);

            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Qwen error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                return "";
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(responseJson);

            string foodName = doc.RootElement
                .GetProperty("output")
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? "";

            return foodName.ToLower().Trim();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Qwen API error: {ex.Message}");
            return "";
        }
    }

    /// <summary>
    /// Open camera barcode scanner page
    /// </summary>
    private async void OnScanBarcodeClicked(object sender, EventArgs e)
    {
        var status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            await DisplayAlert("Permission Denied",
                "Camera permission is required to scan barcodes.", "OK");
            return;
        }

        var scannerPage = new BarcodeScannerPage();
        var tcs = new TaskCompletionSource<string?>();

        scannerPage.Disappearing += (s, args) =>
        {
            tcs.TrySetResult(scannerPage.ScannedBarcode);
        };

        await Navigation.PushModalAsync(scannerPage);

        string? barcode = await tcs.Task;
        if (string.IsNullOrEmpty(barcode)) return;

        ManualBarcodeEntry.Text = barcode;

        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        await FetchNutritionByBarcodeAsync(barcode);
        LoadingIndicator.IsVisible = false;
        LoadingIndicator.IsRunning = false;
    }

    /// <summary>
    /// Search by manually entered barcode
    /// </summary>
    private async void OnManualSearchClicked(object sender, EventArgs e)
    {
        string barcode = ManualBarcodeEntry.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(barcode))
        {
            await DisplayAlert("Validation Error", "Please enter a barcode number.", "OK");
            return;
        }

        if (barcode.Length < 8)
        {
            await DisplayAlert("Validation Error", "Barcode must be at least 8 digits.", "OK");
            return;
        }

        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        await FetchNutritionByBarcodeAsync(barcode);
        LoadingIndicator.IsVisible = false;
        LoadingIndicator.IsRunning = false;
    }

    /// <summary>
    /// Fetch nutrition by barcode - queries multiple databases in order
    /// </summary>
    private async Task FetchNutritionByBarcodeAsync(string barcode)
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;

        try
        {
            // Step 1: Open Food Facts global
            bool found = await TryOpenFoodFactsAsync(
                $"https://world.openfoodfacts.org/api/v0/product/{barcode}.json",
                barcode);
            if (found) return;

            // Step 2: Open Food Facts China
            found = await TryOpenFoodFactsAsync(
                $"https://cn.openfoodfacts.org/api/v0/product/{barcode}.json",
                barcode);
            if (found) return;

            // Step 3: UPC Item DB
            found = await TryUpcItemDbAsync(barcode);
            if (found) return;

            // Step 4: Not found anywhere - offer manual entry
            bool manualEntry = await DisplayAlert(
                "Product Not Found",
                $"Barcode {barcode} was not found in any database.\n\n" +
                "This may be a local or regional product. " +
                "Would you like to enter nutrition info manually?",
                "Enter Manually", "Cancel");

            if (manualEntry)
                await ShowManualNutritionEntryAsync(barcode);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Network error: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    /// <summary>
    /// Try fetching from an Open Food Facts endpoint (global or regional)
    /// </summary>
    private async Task<bool> TryOpenFoodFactsAsync(string url, string barcode)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add(
                "User-Agent", "NutriLens/1.0 (nutrilens@example.com)");

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return false;

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            int status = doc.RootElement.GetProperty("status").GetInt32();
            if (status == 0) return false;

            var product = doc.RootElement.GetProperty("product");

            // Try multiple name fields (Chinese products may use product_name_zh)
            string name = "Unknown";
            foreach (var field in new[] {
            "product_name_zh", "product_name_en",
            "product_name", "abbreviated_product_name" })
            {
                if (product.TryGetProperty(field, out var nameEl))
                {
                    string? val = nameEl.GetString();
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        name = val;
                        break;
                    }
                }
            }

            if (!product.TryGetProperty("nutriments", out var nutriments))
                return false;

            double calories = TryGetDouble(nutriments,
                "energy-kcal_100g", "energy-kcal", "energy_100g");
            // energy_100g is in kJ, convert if kcal not available
            if (calories == 0 && nutriments.TryGetProperty(
                "energy_100g", out var kjEl))
                calories = kjEl.GetDouble() / 4.184;

            double protein = TryGetDouble(nutriments,
                "proteins_100g", "protein_100g");
            double fat = TryGetDouble(nutriments,
                "fat_100g");
            double sugar = TryGetDouble(nutriments,
                "sugars_100g", "sugar_100g");

            DisplayNutrition(name, calories, protein, fat, sugar);
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(300));
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Try fetching from UPC Item DB (free, covers more Asian products)
    /// </summary>
    private async Task<bool> TryUpcItemDbAsync(string barcode)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();

            var response = await _httpClient.GetAsync(
                $"https://api.upcitemdb.com/prod/trial/lookup?upc={barcode}");

            if (!response.IsSuccessStatusCode) return false;

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("items", out var items))
                return false;
            if (items.GetArrayLength() == 0) return false;

            var item = items[0];
            string name = item.TryGetProperty("title", out var titleEl)
                ? titleEl.GetString() ?? "Unknown" : "Unknown";

            // UPC Item DB does not have detailed nutrition - show name only
            // and prompt for manual nutrition input
            bool enterNutrition = await DisplayAlert(
                "Product Found",
                $"Found: {name}\n\nNutrition data not available for this product. " +
                "Would you like to enter nutrition info manually?",
                "Enter Nutrition", "Skip");

            if (enterNutrition)
                await ShowManualNutritionEntryAsync(name);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Helper to safely read a double from nutriments, trying multiple keys
    /// </summary>
    private double TryGetDouble(JsonElement element, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (element.TryGetProperty(key, out var val))
            {
                if (val.ValueKind == JsonValueKind.Number)
                    return val.GetDouble();
                if (val.ValueKind == JsonValueKind.String &&
                    double.TryParse(val.GetString(), out double parsed))
                    return parsed;
            }
        }
        return 0;
    }

    /// <summary>
    /// Show manual nutrition entry dialog when product is not in any database
    /// </summary>
    private async Task ShowManualNutritionEntryAsync(string productName)
    {
        string name = await DisplayPromptAsync(
            "Product Name", "Confirm or edit the product name:",
            initialValue: productName, accept: "Next", cancel: "Cancel");
        if (name == null) return;

        string calStr = await DisplayPromptAsync(
            "Calories", "Enter calories (kcal per 100g):",
            keyboard: Keyboard.Numeric, accept: "Next", cancel: "Cancel");
        if (calStr == null) return;
        double.TryParse(calStr, out double calories);

        string proStr = await DisplayPromptAsync(
            "Protein", "Enter protein (g per 100g):",
            keyboard: Keyboard.Numeric, accept: "Next", cancel: "Cancel");
        if (proStr == null) return;
        double.TryParse(proStr, out double protein);

        string fatStr = await DisplayPromptAsync(
            "Fat", "Enter fat (g per 100g):",
            keyboard: Keyboard.Numeric, accept: "Next", cancel: "Cancel");
        if (fatStr == null) return;
        double.TryParse(fatStr, out double fat);

        string sugStr = await DisplayPromptAsync(
            "Sugar", "Enter sugar (g per 100g):",
            keyboard: Keyboard.Numeric, accept: "OK", cancel: "Skip");
        double.TryParse(sugStr, out double sugar);

        DisplayNutrition(name, calories, protein, fat, sugar);
        Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(300));
    }

    /// <summary>
    /// Fetch nutrition by food name - use Qwen AI to estimate,
    /// fallback to Open Food Facts
    /// </summary>
    private async Task FetchNutritionDataAsync(string foodName)
    {
        // First try Qwen AI nutrition estimate (works in China)
        bool found = await TryQwenNutritionAsync(foodName);
        if (found) return;

        // Fallback: Open Food Facts search
        found = await TryOpenFoodFactsSearchAsync(foodName);
        if (found) return;

        // Last resort: show manual entry
        bool manual = await DisplayAlert(
            "Nutrition Not Found",
            $"Could not get nutrition data for \"{foodName}\".\n" +
            "Would you like to enter it manually?",
            "Enter Manually", "Cancel");

        if (manual)
            await ShowManualNutritionEntryAsync(foodName);
    }

    /// <summary>
    /// Use Qwen AI to estimate nutrition for a food name
    /// </summary>
    private async Task<bool> TryQwenNutritionAsync(string foodName)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add(
                "Authorization", $"Bearer {QwenApiKey}");

            var requestBody = new
            {
                model = "qwen-turbo",
                input = new
                {
                    messages = new[]
                    {
                    new
                    {
                        role = "system",
                        content = "You are a nutrition expert. " +
                                  "Always respond in valid JSON only, no extra text."
                    },
                    new
                    {
                        role = "user",
                        content = $"Estimate the nutrition per 100g for: {foodName}\n" +
                                  "Respond with ONLY this JSON, no markdown:\n" +
                                  "{{\"name\":\"...\",\"calories\":0," +
                                  "\"protein\":0,\"fat\":0,\"sugar\":0}}"
                    }
                }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(
                json, System.Text.Encoding.UTF8, "application/json");

            var response = await client.PostAsync(
                "https://dashscope.aliyuncs.com/api/v1/services/aigc/" +
                "text-generation/generation",
                content, cts.Token);

            if (!response.IsSuccessStatusCode) return false;

            var responseJson = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"Qwen nutrition: {responseJson}");

            var doc = JsonDocument.Parse(responseJson);
            string text = doc.RootElement
                .GetProperty("output")
                .GetProperty("text")
                .GetString() ?? "";

            // Strip markdown code fences if present
            text = text.Trim();
            if (text.StartsWith("```"))
            {
                int start = text.IndexOf('{');
                int end = text.LastIndexOf('}');
                if (start >= 0 && end > start)
                    text = text.Substring(start, end - start + 1);
            }

            var result = JsonDocument.Parse(text);
            var root = result.RootElement;

            string name = root.TryGetProperty("name", out var n)
                ? n.GetString() ?? foodName : foodName;
            double calories = root.TryGetProperty("calories", out var c)
                ? c.GetDouble() : 0;
            double protein = root.TryGetProperty("protein", out var p)
                ? p.GetDouble() : 0;
            double fat = root.TryGetProperty("fat", out var f)
                ? f.GetDouble() : 0;
            double sugar = root.TryGetProperty("sugar", out var s)
                ? s.GetDouble() : 0;

            DisplayNutrition(name, calories, protein, fat, sugar);
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(300));
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Qwen nutrition error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Fallback: search Open Food Facts by food name
    /// </summary>
    private async Task<bool> TryOpenFoodFactsSearchAsync(string foodName)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var client = new HttpClient();

            var url = "https://world.openfoodfacts.org/cgi/search.pl?" +
                      $"search_terms={Uri.EscapeDataString(foodName)}" +
                      "&search_simple=1&action=process&json=1&page_size=1";

            var response = await client.GetAsync(url, cts.Token);
            if (!response.IsSuccessStatusCode) return false;

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var products = doc.RootElement.GetProperty("products");

            if (products.GetArrayLength() == 0) return false;

            var product = products[0];
            string name = product.TryGetProperty("product_name", out var nameEl)
                ? nameEl.GetString() ?? foodName : foodName;

            if (!product.TryGetProperty("nutriments", out var nutriments))
                return false;

            double calories = TryGetDouble(nutriments, "energy-kcal_100g");
            if (calories == 0)
            {
                double kj = TryGetDouble(nutriments, "energy_100g");
                if (kj > 0) calories = kj / 4.184;
            }

            double protein = TryGetDouble(nutriments, "proteins_100g");
            double fat = TryGetDouble(nutriments, "fat_100g");
            double sugar = TryGetDouble(nutriments, "sugars_100g");

            DisplayNutrition(name, calories, protein, fat, sugar);
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(300));
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"OpenFoodFacts search error: {ex.Message}");
            return false;
        }
    }

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

    private void CheckAllergens(string foodName)
    {
        var warnings = new List<string>();
        string nameLower = foodName.ToLower();

        if (_peanutAlert &&
            (nameLower.Contains("peanut") || nameLower.Contains("nut")))
            warnings.Add("Contains Peanuts");

        if (_glutenAlert &&
            (nameLower.Contains("wheat") || nameLower.Contains("bread") ||
             nameLower.Contains("pasta") || nameLower.Contains("flour")))
            warnings.Add("Contains Gluten");

        if (_lactoseAlert &&
            (nameLower.Contains("milk") || nameLower.Contains("cheese") ||
             nameLower.Contains("dairy") || nameLower.Contains("yogurt")))
            warnings.Add("Contains Lactose");

        foreach (var allergen in _customAllergens)
        {
            if (!string.IsNullOrEmpty(allergen) &&
                nameLower.Contains(allergen.ToLower()))
                warnings.Add($"Contains {allergen}");
        }

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

    private async void OnReadAloudClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentFoodName))
        {
            await DisplayAlert("No Data", "Please scan a food item first.", "OK");
            return;
        }

        string text = $"{_currentFoodName} contains " +
                      $"{_currentCalories:F0} calories, " +
                      $"{_currentProtein:F1} grams of protein, " +
                      $"{_currentFat:F1} grams of fat, " +
                      $"and {_currentSugar:F1} grams of sugar.";

        await TextToSpeech.Default.SpeakAsync(text);
    }

    private async void OnSaveBreakfastClicked(object sender, EventArgs e) =>
        await SaveFoodEntry("Breakfast");

    private async void OnSaveLunchClicked(object sender, EventArgs e) =>
        await SaveFoodEntry("Lunch");

    private async void OnSaveDinnerClicked(object sender, EventArgs e) =>
        await SaveFoodEntry("Dinner");

    private async Task SaveFoodEntry(string mealType)
    {
        if (string.IsNullOrEmpty(_currentFoodName))
        {
            await DisplayAlert("No Data", "Please scan a food item first.", "OK");
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
                await DisplayAlert("Saved",
                    $"{_currentFoodName} saved to {mealType}.", "OK");
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
            }
            else
            {
                await DisplayAlert("Error", "Failed to save entry. Please try again.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to save: {ex.Message}", "OK");
        }
    }
}