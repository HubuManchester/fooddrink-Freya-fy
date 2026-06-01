namespace NutriLens.Views;

public partial class PlaceDetailPage : ContentPage
{
    private readonly NearbyPlace _place;
    private bool _isSpeaking = false;
    private CancellationTokenSource? _cts;

    private static readonly string[] HeroColors =
    {
        "#4CAF50", "#2196F3", "#FF9800", "#E91E63",
        "#9C27B0", "#00BCD4", "#FF5722", "#607D8B"
    };

    public PlaceDetailPage(NearbyPlace place)
    {
        InitializeComponent();
        _place = place;
        PopulateUI(place);
    }

    private void PopulateUI(NearbyPlace place)
    {
        int colorIndex = Math.Abs(place.Name.GetHashCode()) % HeroColors.Length;
        HeroGrid.BackgroundColor = Color.FromArgb(HeroColors[colorIndex]);

        NameLabel.Text = place.Name;
        AddressLabel.Text = place.Address;
        DistanceLabel.Text = $"📏 {place.Distance}";

        // Type badge
        if (!string.IsNullOrEmpty(place.Type))
        {
            TypeLabel.Text = place.Type;
            TypeBadge.IsVisible = true;
        }
        else
        {
            TypeBadge.IsVisible = false;
        }

        // Phone
        if (!string.IsNullOrEmpty(place.Tel))
        {
            PhoneLabel.Text = place.Tel;
            PhoneRow.IsVisible = true;
            HoursDivider.IsVisible = true;
        }
        else
        {
            PhoneRow.IsVisible = false;
            HoursDivider.IsVisible = false;
        }

        // Hours
        if (!string.IsNullOrEmpty(place.OpenTime))
        {
            HoursLabel.Text = place.OpenTime;
            HoursRow.IsVisible = true;
        }
        else
        {
            HoursRow.IsVisible = false;
        }

        // Rating
        if (!string.IsNullOrEmpty(place.Rating))
        {
            RatingLabel.Text = $"{place.Rating} / 5";
            StarsLabel.Text = place.RatingStars;
            RatingValueLabel.Text = $"{place.Rating} out of 5.0";
            RatingBadge.IsVisible = true;
            StarsCard.IsVisible = true;
        }
        else
        {
            RatingBadge.IsVisible = false;
            StarsCard.IsVisible = false;
        }

        // TTS preview text
        TtsPreviewLabel.Text = BuildSpeechText(place);
    }

    private static string BuildSpeechText(NearbyPlace place)
    {
        var parts = new List<string>();
        parts.Add(place.Name);

        if (!string.IsNullOrEmpty(place.Type))
            parts.Add($"Type: {place.Type}");

        if (!string.IsNullOrEmpty(place.Distance))
            parts.Add($"Distance: {place.Distance}");

        if (!string.IsNullOrEmpty(place.Address))
            parts.Add($"Address: {place.Address}");

        if (!string.IsNullOrEmpty(place.Rating))
            parts.Add($"Rating: {place.Rating} out of 5");

        if (!string.IsNullOrEmpty(place.OpenTime))
            parts.Add($"Opening hours: {place.OpenTime}");

        if (!string.IsNullOrEmpty(place.Tel))
            parts.Add($"Phone: {place.Tel}");

        return string.Join(". ", parts) + ".";
    }

    // ─── TTS ─────────────────────────────────────────────────────────────────

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
                BuildSpeechText(_place), options, _cts.Token);
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