namespace NutriLens.Views;

public partial class PlaceDetailPage : ContentPage
{
    private static readonly string[] HeroColors =
    {
        "#4CAF50", "#2196F3", "#FF9800", "#E91E63",
        "#9C27B0", "#00BCD4", "#FF5722", "#607D8B"
    };

    public PlaceDetailPage(NearbyPlace place)
    {
        InitializeComponent();
        PopulateUI(place);
    }

    private void PopulateUI(NearbyPlace place)
    {
        // Random hero color based on name hash for consistency
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
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}