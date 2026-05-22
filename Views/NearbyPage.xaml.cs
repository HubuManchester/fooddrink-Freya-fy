namespace NutriLens.Views;

/// <summary>
/// Nearby page - shows nearby healthy restaurants using GPS
/// </summary>
public partial class NearbyPage : ContentPage
{
    public NearbyPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Get current location and find nearby restaurants
    /// </summary>
    private async void OnFindNearbyClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Coming Soon", "GPS location will be implemented next.", "OK");
    }
}