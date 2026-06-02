using System.Text.Json;
using System.Text.RegularExpressions;

namespace NutriLens.Views;

public partial class NearbyPage : ContentPage
{
    private double _currentLat = 0;
    private double _currentLng = 0;
    private Dictionary<string, string> _translationCache = new();
    private List<NearbyPlace> _lastPlaces = new();

    private const string AmapApiKey = "ec1a12dd172e6945fe52a7067b0c4c26";
    private const string QwenApiKey = "sk-a34faf314c1744bd92dc2ddc3559de58";

    private static readonly int[] RadiusValues = { 500, 1000, 2000, 5000 };

    // Amap keyword search map
    #region API Query Mappings
    private static readonly Dictionary<string, string> KeywordQueryMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["restaurant"] = "\u9910\u5385",       // restaurant
            ["food"] = "\u9910\u5385",       // restaurant
            ["healthy"] = "\u5065\u5eb7\u9910", // healthy food
            ["salad"] = "\u6c99\u62c9",       // salad
            ["vegetarian"] = "\u7d20\u98df",       // vegetarian
            ["noodle"] = "\u9762\u9986",       // noodle bar
            ["hotpot"] = "\u706b\u9505",       // hot pot
            ["hot pot"] = "\u706b\u9505",       // hot pot
            ["sushi"] = "\u65e5\u672c\u6599\u7406", // Japanese cuisine
            ["korean"] = "\u97e9\u56fd\u6599\u7406", // Korean cuisine
            ["bbq"] = "\u70e7\u70e4",       // barbecue
            ["barbecue"] = "\u70e7\u70e4",       // barbecue
            ["buffet"] = "\u81ea\u52a9\u9910",  // buffet
            ["dessert"] = "\u751c\u54c1",       // dessert
            ["cafe"] = "\u548c\u5496\u5561\u5385", // cafe 
            ["coffee"] = "\u548c\u5496\u5561\u5385",
            ["snack"] = "\u5c0f\u5403",       // snacks
            ["seafood"] = "\u6d77\u9c9c",       // seafood
            ["dumpling"] = "\u9970\u5b50\u9986",  // dumpling house
            ["pizza"] = "\u6bd4\u8428",       // pizza
            ["burger"] = "\u6c49\u5821",       // burger
            ["thai"] = "\u6cf0\u56fd\u83dc",  // Thai cuisine
            ["indian"] = "\u5370\u5ea6\u83dc",  // Indian cuisine
        };

    // Amap type code -> English label
    // Keys are Unicode-escaped Chinese to keep code visually clean
    private static readonly Dictionary<string, string> TypeMap = new()
    {
        ["\u9910\u996e\u670d\u52a1"] = "Food & Dining",
        ["\u4e2d\u9910\u5385"] = "Chinese Restaurant",
        ["\u897f\u9910\u5385"] = "Western Restaurant",
        ["\u5feb\u9910"] = "Fast Food",
        ["\u548c\u5496\u5561\u5385"] = "Cafe",
        ["\u8336\u9986"] = "Tea House",
        ["\u706b\u9505"] = "Hot Pot",
        ["\u65e5\u672c\u6599\u7406"] = "Japanese",
        ["\u97e9\u56fd\u6599\u7406"] = "Korean",
        ["\u70e7\u70e4"] = "Barbecue",
        ["\u81ea\u52a9\u9910"] = "Buffet",
        ["\u7d20\u98df"] = "Vegetarian",
        ["\u6d77\u9c9c"] = "Seafood",
        ["\u5c0f\u5403"] = "Snacks",
        ["\u9762\u9986"] = "Noodle Bar",
        ["\u9970\u5b50\u9986"] = "Dumpling House",
        ["\u7cb5\u83dc"] = "Cantonese",
        ["\u5ddd\u83dc"] = "Sichuan",
        ["\u6e58\u83dc"] = "Hunan",
        ["\u4e1c\u5317\u83dc"] = "Northeast Chinese",
        ["\u6c99\u62c9"] = "Salad Bar",
        ["\u751c\u54c1"] = "Dessert",
        ["\u996e\u54c1"] = "Drinks",
        ["\u4fbf\u5229\u5e97"] = "Convenience Store",
        ["\u8d85\u5e02"] = "Supermarket",
        ["\u86cb\u7cd5"] = "Bakery",
        ["\u6bd4\u8428"] = "Pizza",
        ["\u6c49\u5821"] = "Burger",
        ["\u70b8\u9e21"] = "Fried Chicken",
        ["\u7c73\u7c89"] = "Rice Noodles",
        ["\u5305\u5b50"] = "Steamed Buns",
        ["\u7ca5"] = "Congee",
        ["\u53f0\u6e7e\u6599\u7406"] = "Taiwanese",
        ["\u4e1c\u5357\u4e9a\u83dc"] = "Southeast Asian",
        ["\u5370\u5ea6\u83dc"] = "Indian",
        ["\u6cf0\u56fd\u83dc"] = "Thai",
    };

    // Open time keyword replacements
    private static readonly Dictionary<string, string> TimeKeywordMap = new()
    {
        ["\u5468\u4e00"] = "Mon",
        ["\u5468\u4e8c"] = "Tue",
        ["\u5468\u4e09"] = "Wed",
        ["\u5468\u56db"] = "Thu",
        ["\u5468\u4e94"] = "Fri",
        ["\u5468\u516d"] = "Sat",
        ["\u5468\u65e5"] = "Sun",
        ["\u5468\u672b"] = "Weekends",
        ["\u5de5\u4f5c\u65e5"] = "Weekdays",
        ["\u81f3"] = " - ",
        ["\u6bcf\u5929"] = "Daily",
        ["\u5168\u5929"] = "All Day",
        ["\u8425\u4e1a\u4e2d"] = "Open Now",
        ["\u4f11\u606f"] = "Closed",
        ["\u8282\u5047\u65e5"] = "Holidays",
        ["\u4e0a\u5348"] = "AM",
        ["\u4e0b\u5348"] = "PM",
        ["\u665a\u4e0a"] = "Evening",
        ["\u65e9\u4e0a"] = "Morning",
    };
    #endregion

    public NearbyPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles pull-to-refresh action and refreshes nearby place data using cached translations when available.
    /// </summary>
    private async void OnPageRefreshing(object sender, EventArgs e)
    {
        try
        {
            if (_currentLat != 0 && _currentLng != 0)
            {
                // Refresh uses cached translations - just re-fetch POI data
                await SearchNearbyAsync(useCache: true);
            }
        }
        finally
        {
            PageRefreshView.IsRefreshing = false;
        }
    }

    /// <summary>
    /// Retrieves the user's current location and initiates a nearby search.
    /// </summary>
    private async void OnFindNearbyClicked(object sender, EventArgs e)
    {
        try
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            EmptyState.IsVisible = false;
            StatsFrame.IsVisible = false;
            PlacesList.ItemsSource = null;

            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Permission Denied",
                    "Location permission is required to find nearby restaurants.",
                    "OK");
                return;
            }

            var location = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Medium,
                    Timeout = TimeSpan.FromSeconds(15)
                });

            if (location == null)
            {
                await DisplayAlert("Location Error",
                    "Could not get your location. Please try again.", "OK");
                return;
            }

            _currentLat = location.Latitude;
            _currentLng = location.Longitude;
            CoordinatesLabel.Text = $"{_currentLat:F4}, {_currentLng:F4}";

            try
            {
                var placemarks = await Geocoding.Default
                    .GetPlacemarksAsync(_currentLat, _currentLng);
                var pm = placemarks?.FirstOrDefault();
                LocationLabel.Text = pm != null
                    ? $"{pm.SubLocality ?? pm.Locality}, {pm.AdminArea}"
                    : $"{_currentLat:F4}, {_currentLng:F4}";
            }
            catch
            {
                LocationLabel.Text = $"{_currentLat:F4}, {_currentLng:F4}";
            }

            await SearchNearbyAsync();
        }
        catch (FeatureNotSupportedException)
        {
            await DisplayAlert("Not Supported",
                "GPS is not supported on this device.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error",
                $"Failed to get location: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    /// <summary>
    /// Searches nearby places using the Amap API and updates the UI
    /// with translated and formatted results.
    /// </summary>
    /// <param name="useCache">
    /// Indicates whether cached translations should be reused.
    /// </param>
    private async Task SearchNearbyAsync(bool useCache = false)
    {
        try
        {
            string keyword = KeywordEntry.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(keyword)) keyword = "restaurant";

            string amapKeyword = KeywordQueryMap.TryGetValue(keyword, out var cn)
                ? cn : keyword;

            int radius = RadiusValues[
                RadiusPicker.SelectedIndex >= 0 ? RadiusPicker.SelectedIndex : 1];

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(15);

            string url = "https://restapi.amap.com/v3/place/around?" +
                         $"key={AmapApiKey}" +
                         $"&location={_currentLng:F6},{_currentLat:F6}" +
                         $"&keywords={Uri.EscapeDataString(amapKeyword)}" +
                         $"&radius={radius}" +
                         "&types=050000|050100|050200|050300" +
                         "&sortrule=distance" +
                         "&offset=20" +
                         "&page=1" +
                         "&extensions=all";

            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlert("Error", "Could not connect to map service.", "OK");
                return;
            }

            var doc = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync());
            string status = SafeGetString(doc.RootElement, "status");

            if (status != "1")
            {
                string info = SafeGetString(doc.RootElement, "info");
                await DisplayAlert("Search Failed",
                    $"Map API error: {info}\nPlease check your API key.", "OK");
                return;
            }

            var pois = doc.RootElement.GetProperty("pois");

            // Step 1: collect raw strings
            var rawNames = new List<string>();
            var rawAddrs = new List<string>();
            var rawTypes = new List<string>();
            var rawTimes = new List<string>();
            var rawRatings = new List<string>();
            var rawTels = new List<string>();
            var rawDists = new List<string>();

            foreach (var poi in pois.EnumerateArray())
            {
                rawNames.Add(SafeGetString(poi, "name"));
                rawAddrs.Add(SafeGetString(poi, "address"));
                rawTypes.Add(SafeGetString(poi, "type"));
                rawDists.Add(SafeGetString(poi, "distance"));
                rawTels.Add(SafeGetString(poi, "tel"));

                string openTime = "", rating = "";
                if (poi.TryGetProperty("biz_ext", out var biz)
                    && biz.ValueKind == JsonValueKind.Object)
                {
                    openTime = SafeGetString(biz, "open_time");
                    rating = SafeGetString(biz, "rating");
                }
                rawTimes.Add(openTime);
                rawRatings.Add(rating);
            }

            // Step 2: rule-based translations
            var partialTypes = rawTypes.Select(TranslateType).ToList();
            var partialTimes = rawTimes.Select(TranslateOpenTime).ToList();

            // Step 3: find strings still needing translation
            var needsAI = new HashSet<string>();
            for (int i = 0; i < rawNames.Count; i++)
            {
                if (HasChinese(rawNames[i])) needsAI.Add(rawNames[i]);
                if (HasChinese(rawAddrs[i])) needsAI.Add(rawAddrs[i]);
                if (HasChinese(partialTypes[i])) needsAI.Add(rawTypes[i]);
                if (HasChinese(partialTimes[i])) needsAI.Add(rawTimes[i]);
            }

            // Step 4: AI translate - use cache if refreshing
            if (!useCache)
            {
                var uncachedItems = needsAI
                    .Where(s => !_translationCache.ContainsKey(s))
                    .ToList();

                if (uncachedItems.Count > 0)
                {
                    LoadingIndicator.IsVisible = true;
                    LoadingIndicator.IsRunning = true;

                    var newTranslations = await TranslateBatchAsync(uncachedItems);

                    // Merge into cache
                    foreach (var kv in newTranslations)
                        _translationCache[kv.Key] = kv.Value;

                    LoadingIndicator.IsVisible = false;
                    LoadingIndicator.IsRunning = false;
                }
            }
            else
            {
                // On refresh: translate only truly new items not in cache
                // Keep LoadingIndicator hidden - RefreshView handles the spinner
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;

                var uncachedItems = needsAI
                    .Where(s => !_translationCache.ContainsKey(s))
                    .ToList();

                if (uncachedItems.Count > 0)
                {
                    var newTranslations = await TranslateBatchAsync(uncachedItems);
                    foreach (var kv in newTranslations)
                        _translationCache[kv.Key] = kv.Value;
                }
            }

            // Step 5: build final list using cache
            var places = new List<NearbyPlace>();
            var poisList = pois.EnumerateArray().ToList();

            for (int i = 0; i < poisList.Count; i++)
            {
                string name = HasChinese(rawNames[i])
                    ? Translate(rawNames[i], _translationCache)
                    : rawNames[i];

                string address = HasChinese(rawAddrs[i])
                    ? Translate(rawAddrs[i], _translationCache)
                    : rawAddrs[i];

                string typeEn = HasChinese(partialTypes[i])
                    ? Translate(rawTypes[i], _translationCache)
                    : partialTypes[i];

                string openTimeEn = HasChinese(partialTimes[i])
                    ? Translate(rawTimes[i], _translationCache)
                    : partialTimes[i];

                string distDisplay = "";
                if (int.TryParse(rawDists[i], out int distM))
                    distDisplay = distM >= 1000
                        ? $"{distM / 1000.0:F1} km"
                        : $"{distM} m";

                double ratingVal = 0;
                string stars = "";
                if (double.TryParse(rawRatings[i], out ratingVal) && ratingVal > 0)
                {
                    int full = (int)Math.Floor(ratingVal);
                    stars = new string('★', Math.Clamp(full, 0, 5)) +
                            new string('☆', Math.Clamp(5 - full, 0, 5));
                }

                string tel = Regex.Replace(rawTels[i] ?? "", @"[\u4e00-\u9fff]", "");

                places.Add(new NearbyPlace
                {
                    Name = string.IsNullOrWhiteSpace(name) ? "Unknown" : name,
                    Address = string.IsNullOrEmpty(address)
                                    ? "Address not available" : address,
                    Distance = distDisplay,
                    Type = typeEn,
                    HasType = !string.IsNullOrEmpty(typeEn),
                    Rating = ratingVal > 0 ? $"{ratingVal:F1}" : "",
                    RatingStars = stars,
                    Tel = tel,
                    OpenTime = openTimeEn
                });
            }

            _lastPlaces = places;
            PlacesList.ItemsSource = null;
            PlacesList.ItemsSource = places;
            StatsFrame.IsVisible = true;
            EmptyState.IsVisible = places.Count == 0;
            ResultCountLabel.Text = places.Count.ToString();
            NearestLabel.Text = places.FirstOrDefault()?.Distance ?? "-";

            int withHours = places.Count(p => !string.IsNullOrEmpty(p.OpenTime));
            OpenNowLabel.Text = withHours > 0 ? withHours.ToString() : "-";

            if (places.Count == 0)
                await DisplayAlert("No Results",
                    "No places found. Try a different keyword or larger radius.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Search failed: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Opens the detail page for the selected nearby place.
    /// </summary>
    private async void OnPlaceTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not NearbyPlace place) return;
        await Navigation.PushAsync(new PlaceDetailPage(place));
    }

    /// <summary>
    /// Translates a batch of Chinese text strings into English using the Qwen language model.
    /// </summary>
    private async Task<Dictionary<string, string>> TranslateBatchAsync(
        List<string> texts)
    {
        var result = new Dictionary<string, string>();

        var toTranslate = texts
            .Where(t => !string.IsNullOrWhiteSpace(t) && HasChinese(t))
            .Distinct()
            .ToList();

        if (toTranslate.Count == 0) return result;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add(
                "Authorization", $"Bearer {QwenApiKey}");

            string inputText = string.Join("\n",
                toTranslate.Select((t, i) => $"{i + 1}. {t}"));

            var body = new
            {
                model = "qwen-turbo",
                input = new
                {
                    messages = new[]
                    {
                        new
                        {
                            role    = "system",
                            content = "You are a translator. Translate each " +
                                      "numbered Chinese item to natural English. " +
                                      "Keep brand names and proper nouns as-is. " +
                                      "Reply ONLY with the numbered list in the " +
                                      "same format, no extra text."
                        },
                        new { role = "user", content = inputText }
                    }
                }
            };

            var response = await client.PostAsync(
                "https://dashscope.aliyuncs.com/api/v1/services/aigc/" +
                "text-generation/generation",
                new System.Net.Http.StringContent(
                    JsonSerializer.Serialize(body),
                    System.Text.Encoding.UTF8,
                    "application/json"),
                cts.Token);

            if (!response.IsSuccessStatusCode) return result;

            var doc = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync());
            string output = doc.RootElement
                .GetProperty("output")
                .GetProperty("text")
                .GetString() ?? "";

            var lines = output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => Regex.Replace(l.Trim(), @"^\d+\.\s*", ""))
                .ToList();

            for (int i = 0; i < Math.Min(toTranslate.Count, lines.Count); i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                    result[toTranslate[i]] = lines[i].Trim();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Translation error: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Determines whether a string contains Chinese characters.
    /// </summary>
    private static bool HasChinese(string text) =>
        !string.IsNullOrEmpty(text) &&
        text.Any(c => c >= 0x4E00 && c <= 0x9FFF);

    private static string Translate(
        string original, Dictionary<string, string> map) =>
        string.IsNullOrEmpty(original) ? original
        : map.TryGetValue(original, out var t) ? t : original;

    private static string TranslateType(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";

        foreach (var seg in raw.Split(new[] { ';', '|' },
            StringSplitOptions.RemoveEmptyEntries).Reverse())
        {
            string s = seg.Trim();
            if (TypeMap.TryGetValue(s, out var en)) return en;
            foreach (var kv in TypeMap)
                if (s.Contains(kv.Key)) return kv.Value;
        }

        return HasChinese(raw) ? "" : raw;
    }

    /// <summary>
    /// Converts Chinese business-hour descriptions into English.
    /// </summary>
    private static string TranslateOpenTime(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";

        string result = raw;
        foreach (var kv in TimeKeywordMap)
            result = result.Replace(kv.Key, kv.Value);
        return result.Trim();
    }

    /// <summary>
    /// Safely retrieves a property value from a JSON element and converts it to a string representation.
    /// </summary>
    private static string SafeGetString(JsonElement element, string key)
    {
        if (!element.TryGetProperty(key, out var prop)) return "";

        return prop.ValueKind switch
        {
            JsonValueKind.String => prop.GetString() ?? "",
            JsonValueKind.Array =>
                string.Join(", ",
                    prop.EnumerateArray()
                        .Select(x => x.ValueKind == JsonValueKind.String
                            ? x.GetString() ?? "" : x.ToString())
                        .Where(s => !string.IsNullOrEmpty(s))),
            JsonValueKind.Number => prop.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => ""
        };
    }
}

public class NearbyPlace
{
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string Distance { get; set; } = "";
    public string Type { get; set; } = "";
    public bool HasType { get; set; }
    public string Rating { get; set; } = "";
    public string RatingStars { get; set; } = "";
    public string Tel { get; set; } = "";
    public string OpenTime { get; set; } = "";
}