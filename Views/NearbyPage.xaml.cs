namespace NutriLens.Views;

/// <summary>
/// Nearby page - uses GPS to find user location
/// and display nearby healthy restaurants
/// </summary>
public partial class NearbyPage : ContentPage
{
    // Current location coordinates
    private double _currentLat = 0;
    private double _currentLng = 0;

    public NearbyPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Get GPS location and find nearby restaurants
    /// </summary>
    private async void OnFindNearbyClicked(object sender, EventArgs e)
    {
        try
        {
            // Show loading
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            EmptyState.IsVisible = false;

            // Request location permission
            var status = await Permissions.RequestAsync
                <Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
            {
                await DisplayAlert("Permission Denied",
                    "Location permission is required to find nearby restaurants.",
                    "OK");
                return;
            }

            // Get current location
            var location = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Medium,
                    Timeout = TimeSpan.FromSeconds(10)
                });

            if (location == null)
            {
                await DisplayAlert("Location Error",
                    "Could not get your location. Please try again.", "OK");
                return;
            }

            // Save coordinates
            _currentLat = location.Latitude;
            _currentLng = location.Longitude;

            // Update location display
            CoordinatesLabel.Text =
                $"Lat: {_currentLat:F4}, Lng: {_currentLng:F4}";

            // Get address from coordinates
            var placemarks = await Geocoding.Default
                .GetPlacemarksAsync(_currentLat, _currentLng);

            var placemark = placemarks?.FirstOrDefault();
            if (placemark != null)
            {
                LocationLabel.Text =
                    $"{placemark.SubLocality ?? placemark.Locality}, " +
                    $"{placemark.CountryName}";
            }

            // Load nearby places
            LoadNearbyPlaces();
        }
        catch (FeatureNotSupportedException)
        {
            await DisplayAlert("Not Supported",
                "GPS is not supported on this device.", "OK");
        }
        catch (PermissionException)
        {
            await DisplayAlert("Permission Error",
                "Location permission was denied.", "OK");
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
    /// Load nearby healthy restaurants
    /// Uses sample data for demonstration
    /// In production would use Google Places API
    /// </summary>
    private void LoadNearbyPlaces()
    {
        try
        {
            // Sample nearby places data
            // In production: call Google Places API with _currentLat/_currentLng
            var places = new List<NearbyPlace>
            {
                new NearbyPlace
                {
                    Name = "Green Bowl",
                    Address = "123 High Street",
                    Distance = "0.2 km away"
                },
                new NearbyPlace
                {
                    Name = "Fresh & Healthy",
                    Address = "45 Market Square",
                    Distance = "0.5 km away"
                },
                new NearbyPlace
                {
                    Name = "The Salad Bar",
                    Address = "78 Church Road",
                    Distance = "0.8 km away"
                },
                new NearbyPlace
                {
                    Name = "Veggie Delight",
                    Address = "12 Park Lane",
                    Distance = "1.1 km away"
                },
                new NearbyPlace
                {
                    Name = "Nutrition Hub",
                    Address = "56 Queens Avenue",
                    Distance = "1.4 km away"
                }
            };

            PlacesList.ItemsSource = places;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Error loading nearby places: {ex.Message}");
        }
    }

    /// <summary>
    /// Show details when a nearby place is tapped
    /// </summary>
    private async void OnPlaceTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is NearbyPlace place)
        {
            await DisplayAlert(place.Name,
                $"Address: {place.Address}\n" +
                $"Distance: {place.Distance}\n" +
                $"Type: Healthy Restaurant",
                "Close");
        }
    }
}

/// <summary>
/// Model for nearby restaurant display
/// </summary>
public class NearbyPlace
{
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string Distance { get; set; } = "";
}