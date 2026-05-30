using System.Text.Json;
using NutriLens.Models;
using NutriLens.Services;

namespace NutriLens.Views;

public partial class DiaryPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private List<DiaryEntry> _entries = new();

    // Current scan result
    private string _scanFoodName = "";
    private double _scanCalories = 0;
    private double _scanProtein = 0;
    private double _scanFat = 0;
    private double _scanSugar = 0;

    private const string QwenApiKey = "sk-a34faf314c1744bd92dc2ddc3559de58";

    // Allergen settings
    private bool _peanutAlert = false;
    private bool _glutenAlert = false;
    private bool _lactoseAlert = false;
    private List<string> _customAllergens = new();

    public DiaryPage(DatabaseService databaseService)
    {
        InitializeComponent();
        _databaseService = databaseService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        LoadAllergenSettings();
        await LoadEntriesAsync();
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

    // ─── Diary Load / Totals ──────────────────────────────────────────────────

    private async Task LoadEntriesAsync()
    {
        try
        {
            _entries = await _databaseService.GetTodayEntriesAsync();
            DiaryList.ItemsSource = null;
            DiaryList.ItemsSource = _entries;
            EmptyState.IsVisible = _entries.Count == 0;
            UpdateTotals();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load diary: {ex.Message}", "OK");
        }
    }

    private void UpdateTotals()
    {
        TotalCaloriesLabel.Text = $"{_entries.Sum(e => e.Calories):F0}";
        TotalProteinLabel.Text = $"{_entries.Sum(e => e.Protein):F1}g";
        TotalFatLabel.Text = $"{_entries.Sum(e => e.Fat):F1}g";
    }

    // ─── Delete (fixed: no Task.Run, direct await) ────────────────────────────

    private async void OnDeleteSwipe(object sender, EventArgs e)
    {
        if (sender is not SwipeItem swipeItem) return;
        if (swipeItem.BindingContext is not DiaryEntry entry) return;

        bool confirm = await DisplayAlert("Delete Entry",
            $"Delete {entry.FoodName}?", "Delete", "Cancel");
        if (!confirm) return;

        try
        {
            bool success = await _databaseService.DeleteEntryAsync(entry.Id);
            if (success)
            {
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
                await LoadEntriesAsync();
            }
            else
            {
                await DisplayAlert("Error", "Failed to delete entry.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to delete: {ex.Message}", "OK");
        }
    }

    // ─── Manual Add ───────────────────────────────────────────────────────────

    private async void OnAddEntryClicked(object sender, EventArgs e)
    {
        // Hide scanner if open
        ScannerPanel.IsVisible = false;
        ScanResultFrame.IsVisible = false;

        var popup = new AddFoodPopupPage();
        var tcs = new TaskCompletionSource<DiaryEntry?>();
        popup.Disappearing += (s, args) => tcs.TrySetResult(popup.Result);
        await Navigation.PushModalAsync(popup);

        var entry = await tcs.Task;
        if (entry == null) return;

        try
        {
            bool success = await _databaseService.SaveEntryAsync(entry);
            if (success)
            {
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
                await LoadEntriesAsync();
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

    // ─── Scanner Panel Toggle ─────────────────────────────────────────────────

    private void OnScanFoodClicked(object sender, EventArgs e)
    {
        ScannerPanel.IsVisible = !ScannerPanel.IsVisible;
        ScanResultFrame.IsVisible = false;
        PhotoPreviewFrame.IsVisible = false;

        // Default to photo tab
        PhotoPanel.IsVisible = true;
        BarcodePanel.IsVisible = false;
        PhotoTabBtn.BackgroundColor = Color.FromArgb("#4CAF50");
        PhotoTabBtn.TextColor = Colors.White;
        BarcodeTabBtn.BackgroundColor = Colors.Transparent;
        BarcodeTabBtn.TextColor = Colors.Gray;
    }

    private void OnPhotoTabClicked(object sender, EventArgs e)
    {
        PhotoPanel.IsVisible = true;
        BarcodePanel.IsVisible = false;
        PhotoTabBtn.BackgroundColor = Color.FromArgb("#4CAF50");
        PhotoTabBtn.TextColor = Colors.White;
        BarcodeTabBtn.BackgroundColor = Colors.Transparent;
        BarcodeTabBtn.TextColor = Colors.Gray;
    }

    private void OnBarcodeTabClicked(object sender, EventArgs e)
    {
        PhotoPanel.IsVisible = false;
        BarcodePanel.IsVisible = true;
        BarcodeTabBtn.BackgroundColor = Color.FromArgb("#2196F3");
        BarcodeTabBtn.TextColor = Colors.White;
        PhotoTabBtn.BackgroundColor = Colors.Transparent;
        PhotoTabBtn.TextColor = Colors.Gray;
    }

    // ─── Photo / Gallery ──────────────────────────────────────────────────────

    private async void OnTakePhotoClicked(object sender, EventArgs e)
    {
        try
        {
            var status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Permission Denied",
                    "Camera permission is required.", "OK");
                return;
            }
            var photo = await MediaPicker.CapturePhotoAsync();
            if (photo != null) await ProcessPhotoAsync(photo);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Camera error: {ex.Message}", "OK");
        }
    }

    private async void OnPickPhotoClicked(object sender, EventArgs e)
    {
        try
        {
            var photo = await MediaPicker.PickPhotoAsync(
                new MediaPickerOptions { Title = "Select a food photo" });
            if (photo != null) await ProcessPhotoAsync(photo);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Gallery error: {ex.Message}", "OK");
        }
    }

    private async Task ProcessPhotoAsync(FileResult photo)
    {
        ScanLoadingIndicator.IsVisible = true;
        ScanLoadingIndicator.IsRunning = true;
        ScanResultFrame.IsVisible = false;

        try
        {
            FoodImage.Source = ImageSource.FromFile(photo.FullPath);
            PhotoPreviewFrame.IsVisible = true;

            using var stream = await photo.OpenReadAsync();
            var bytes = new byte[stream.Length];
            await stream.ReadAsync(bytes, 0, (int)stream.Length);
            string base64 = Convert.ToBase64String(bytes);

            string foodName = await IdentifyFoodAsync(base64);

            if (!string.IsNullOrEmpty(foodName) && foodName != "unknown")
                await FetchNutritionDataAsync(foodName);
            else
                await DisplayAlert("Not Recognised",
                    "Could not identify the food. Try again or use barcode.", "OK");
        }
        finally
        {
            ScanLoadingIndicator.IsVisible = false;
            ScanLoadingIndicator.IsRunning = false;
        }
    }

    // ─── Barcode ──────────────────────────────────────────────────────────────

    private async void OnScanBarcodeClicked(object sender, EventArgs e)
    {
        var status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            await DisplayAlert("Permission Denied",
                "Camera permission is required.", "OK");
            return;
        }

        var scannerPage = new BarcodeScannerPage();
        var tcs = new TaskCompletionSource<string?>();
        scannerPage.Disappearing += (s, args) => tcs.TrySetResult(scannerPage.ScannedBarcode);
        await Navigation.PushModalAsync(scannerPage);

        string? barcode = await tcs.Task;
        if (string.IsNullOrEmpty(barcode)) return;

        ManualBarcodeEntry.Text = barcode;
        ScanLoadingIndicator.IsVisible = true;
        ScanLoadingIndicator.IsRunning = true;
        await FetchNutritionByBarcodeAsync(barcode);
        ScanLoadingIndicator.IsVisible = false;
        ScanLoadingIndicator.IsRunning = false;
    }

    private async void OnManualBarcodeSearchClicked(object sender, EventArgs e)
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

        ScanLoadingIndicator.IsVisible = true;
        ScanLoadingIndicator.IsRunning = true;
        await FetchNutritionByBarcodeAsync(barcode);
        ScanLoadingIndicator.IsVisible = false;
        ScanLoadingIndicator.IsRunning = false;
    }

    // ─── Qwen Vision ─────────────────────────────────────────────────────────

    private async Task<string> IdentifyFoodAsync(string base64Image)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {QwenApiKey}");

            var body = new
            {
                model = "qwen-vl-plus",
                input = new
                {
                    messages = new[]
                    {
                        new
                        {
                            role    = "user",
                            content = new object[]
                            {
                                new { image = $"data:image/jpeg;base64,{base64Image}" },
                                new { text  = "What food is in this image? Reply with only the food name in English, nothing else. If no food is visible, reply with 'unknown'." }
                            }
                        }
                    }
                }
            };

            var response = await client.PostAsync(
                "https://dashscope.aliyuncs.com/api/v1/services/aigc/multimodal-generation/generation",
                new StringContent(JsonSerializer.Serialize(body),
                    System.Text.Encoding.UTF8, "application/json"),
                cts.Token);

            if (!response.IsSuccessStatusCode) return "";

            var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return doc.RootElement
                .GetProperty("output").GetProperty("choices")[0]
                .GetProperty("message").GetProperty("content")[0]
                .GetProperty("text").GetString()?.ToLower().Trim() ?? "";
        }
        catch { return ""; }
    }

    // ─── Nutrition Fetch ─────────────────────────────────────────────────────

    private async Task FetchNutritionDataAsync(string foodName)
    {
        if (await TryQwenNutritionAsync(foodName)) return;
        if (await TryOpenFoodFactsSearchAsync(foodName)) return;

        bool manual = await DisplayAlert("Nutrition Not Found",
            $"Could not get nutrition data for \"{foodName}\".\nEnter manually?",
            "Enter Manually", "Cancel");
        if (manual) await ShowManualNutritionEntryAsync(foodName);
    }

    private async Task FetchNutritionByBarcodeAsync(string barcode)
    {
        try
        {
            if (await TryOpenFoodFactsAsync(
                $"https://world.openfoodfacts.org/api/v0/product/{barcode}.json")) return;
            if (await TryOpenFoodFactsAsync(
                $"https://cn.openfoodfacts.org/api/v0/product/{barcode}.json")) return;

            bool manual = await DisplayAlert("Product Not Found",
                $"Barcode {barcode} was not found.\nEnter nutrition manually?",
                "Enter Manually", "Cancel");
            if (manual) await ShowManualNutritionEntryAsync(barcode);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Network error: {ex.Message}", "OK");
        }
    }

    private async Task<bool> TryOpenFoodFactsAsync(string url)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "NutriLens/1.0");

            var response = await client.GetAsync(url, cts.Token);
            if (!response.IsSuccessStatusCode) return false;

            var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (doc.RootElement.GetProperty("status").GetInt32() == 0) return false;

            var product = doc.RootElement.GetProperty("product");
            string name = "Unknown";
            foreach (var field in new[] { "product_name_zh", "product_name_en", "product_name" })
            {
                if (product.TryGetProperty(field, out var nEl))
                {
                    string? v = nEl.GetString();
                    if (!string.IsNullOrWhiteSpace(v)) { name = v; break; }
                }
            }

            if (!product.TryGetProperty("nutriments", out var nm)) return false;

            double cal = TryGetDouble(nm, "energy-kcal_100g", "energy-kcal");
            if (cal == 0) { double kj = TryGetDouble(nm, "energy_100g"); if (kj > 0) cal = kj / 4.184; }

            ShowScanResult(name, cal,
                TryGetDouble(nm, "proteins_100g"),
                TryGetDouble(nm, "fat_100g"),
                TryGetDouble(nm, "sugars_100g"));
            return true;
        }
        catch { return false; }
    }

    private async Task<bool> TryQwenNutritionAsync(string foodName)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {QwenApiKey}");

            var body = new
            {
                model = "qwen-turbo",
                input = new
                {
                    messages = new[]
                    {
                        new { role="system", content="You are a nutrition expert. Always respond in valid JSON only, no extra text." },
                        new { role="user",   content=$"Estimate nutrition per 100g for: {foodName}\nRespond with ONLY this JSON:\n{{\"name\":\"...\",\"calories\":0,\"protein\":0,\"fat\":0,\"sugar\":0}}" }
                    }
                }
            };

            var response = await client.PostAsync(
                "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation",
                new StringContent(JsonSerializer.Serialize(body),
                    System.Text.Encoding.UTF8, "application/json"),
                cts.Token);

            if (!response.IsSuccessStatusCode) return false;

            var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            string text = doc.RootElement.GetProperty("output").GetProperty("text")
                             .GetString()?.Trim() ?? "";

            if (text.StartsWith("```"))
            {
                int s = text.IndexOf('{'), en = text.LastIndexOf('}');
                if (s >= 0 && en > s) text = text.Substring(s, en - s + 1);
            }

            var r = JsonDocument.Parse(text).RootElement;
            ShowScanResult(
                r.TryGetProperty("name", out var n) ? n.GetString() ?? foodName : foodName,
                r.TryGetProperty("calories", out var c) ? c.GetDouble() : 0,
                r.TryGetProperty("protein", out var p) ? p.GetDouble() : 0,
                r.TryGetProperty("fat", out var f) ? f.GetDouble() : 0,
                r.TryGetProperty("sugar", out var sg) ? sg.GetDouble() : 0);
            return true;
        }
        catch { return false; }
    }

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

            var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var products = doc.RootElement.GetProperty("products");
            if (products.GetArrayLength() == 0) return false;

            var product = products[0];
            string name = product.TryGetProperty("product_name", out var nEl)
                ? nEl.GetString() ?? foodName : foodName;

            if (!product.TryGetProperty("nutriments", out var nm)) return false;

            double cal = TryGetDouble(nm, "energy-kcal_100g");
            if (cal == 0) { double kj = TryGetDouble(nm, "energy_100g"); if (kj > 0) cal = kj / 4.184; }

            ShowScanResult(name, cal,
                TryGetDouble(nm, "proteins_100g"),
                TryGetDouble(nm, "fat_100g"),
                TryGetDouble(nm, "sugars_100g"));
            return true;
        }
        catch { return false; }
    }

    private double TryGetDouble(JsonElement element, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (element.TryGetProperty(key, out var val))
            {
                if (val.ValueKind == JsonValueKind.Number) return val.GetDouble();
                if (val.ValueKind == JsonValueKind.String &&
                    double.TryParse(val.GetString(), out double p)) return p;
            }
        }
        return 0;
    }

    // ─── Show Scan Result ────────────────────────────────────────────────────

    private void ShowScanResult(string name, double calories,
        double protein, double fat, double sugar)
    {
        _scanFoodName = name;
        _scanCalories = calories;
        _scanProtein = protein;
        _scanFat = fat;
        _scanSugar = sugar;

        ScanFoodNameLabel.Text = name;
        ScanCaloriesLabel.Text = $"{calories:F0}";
        ScanProteinLabel.Text = $"{protein:F1}g";
        ScanFatLabel.Text = $"{fat:F1}g";
        ScanSugarLabel.Text = $"{sugar:F1}g";
        ScanResultFrame.IsVisible = true;

        Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(300));
    }

    private async Task ShowManualNutritionEntryAsync(string productName)
    {
        string? name = await DisplayPromptAsync("Product Name",
            "Confirm or edit the product name:",
            initialValue: productName, accept: "Next", cancel: "Cancel");
        if (name == null) return;

        string? calStr = await DisplayPromptAsync("Calories",
            "Enter calories (kcal per 100g):",
            keyboard: Keyboard.Numeric, accept: "Next", cancel: "Cancel");
        if (calStr == null) return;
        double.TryParse(calStr, out double calories);

        string? proStr = await DisplayPromptAsync("Protein",
            "Enter protein (g per 100g):",
            keyboard: Keyboard.Numeric, accept: "Next", cancel: "Cancel");
        if (proStr == null) return;
        double.TryParse(proStr, out double protein);

        string? fatStr = await DisplayPromptAsync("Fat",
            "Enter fat (g per 100g):",
            keyboard: Keyboard.Numeric, accept: "Next", cancel: "Cancel");
        if (fatStr == null) return;
        double.TryParse(fatStr, out double fat);

        string? sugStr = await DisplayPromptAsync("Sugar",
            "Enter sugar (g per 100g):",
            keyboard: Keyboard.Numeric, accept: "OK", cancel: "Skip");
        double.TryParse(sugStr, out double sugar);

        ShowScanResult(name, calories, protein, fat, sugar);
    }

    // ─── Save Scan Result ────────────────────────────────────────────────────

    private async void OnScanSaveBreakfastClicked(object sender, EventArgs e) =>
        await SaveScanEntry("Breakfast");
    private async void OnScanSaveLunchClicked(object sender, EventArgs e) =>
        await SaveScanEntry("Lunch");
    private async void OnScanSaveDinnerClicked(object sender, EventArgs e) =>
        await SaveScanEntry("Dinner");
    private async void OnScanSaveSnackClicked(object sender, EventArgs e) =>
        await SaveScanEntry("Snack");

    private async Task SaveScanEntry(string mealType)
    {
        if (string.IsNullOrEmpty(_scanFoodName))
        {
            await DisplayAlert("No Data", "Please scan a food item first.", "OK");
            return;
        }

        try
        {
            var entry = new DiaryEntry
            {
                FoodName = _scanFoodName,
                MealType = mealType,
                Calories = _scanCalories,
                Protein = _scanProtein,
                Fat = _scanFat,
                Sugar = _scanSugar,
                Date = DateTime.Now
            };

            bool success = await _databaseService.SaveEntryAsync(entry);
            if (success)
            {
                await DisplayAlert("Saved", $"{_scanFoodName} saved to {mealType}.", "OK");
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
                ScannerPanel.IsVisible = false;
                ScanResultFrame.IsVisible = false;
                await LoadEntriesAsync();
            }
            else
            {
                await DisplayAlert("Error", "Failed to save entry.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to save: {ex.Message}", "OK");
        }
    }
}