using ZXing.Net.Maui;

namespace NutriLens.Views;

public partial class BarcodeScannerPage : ContentPage
{
    public string? ScannedBarcode { get; private set; }
    private bool _scanned = false;
    private bool _torchOn = false;

    public BarcodeScannerPage()
    {
        InitializeComponent();
        BarcodeReader.BarcodesDetected += OnBarcodesDetected;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _scanned = false;
        BarcodeReader.IsDetecting = true;
    }

    /// <summary>
    /// Handle barcode detection and return the scanned value
    /// </summary>
    private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        // Guard: only process the first successful scan
        if (_scanned) return;

        var result = e.Results?.FirstOrDefault();
        if (result == null || string.IsNullOrWhiteSpace(result.Value)) return;

        _scanned = true;
        BarcodeReader.IsDetecting = false;   // stop scanning immediately

        string barcode = result.Value;
        ScannedBarcode = barcode;

        // Marshal back to UI thread — do NOT await DisplayAlert inside the callback
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Navigation.PopModalAsync();
        });
    }

    /// <summary>
    /// Cancel barcode scanning and close the scanner page
    /// </summary>
    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        ScannedBarcode = null;
        await Navigation.PopModalAsync();
    }

    /// <summary>
    /// Toggle device flashlight for barcode scanning
    /// </summary>
    private void OnTorchClicked(object? sender, EventArgs e)
    {
        try
        {
            _torchOn = !_torchOn;
            BarcodeReader.IsTorchOn = _torchOn;

            if (_torchOn)
            {
                TorchButton.Text = "🔦 Torch Off";
                TorchButton.BackgroundColor = Color.FromArgb("#FFC107");
                TorchButton.TextColor = Colors.Black;
                TorchButton.BorderColor = Colors.Transparent;
            }
            else
            {
                TorchButton.Text = "🔦 Torch On";
                TorchButton.BackgroundColor = Color.FromArgb("#80000000");
                TorchButton.TextColor = Colors.White;
                TorchButton.BorderColor = Colors.White;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Torch error: {ex.Message}");
        }
    }

    /// <summary>
    /// Stop barcode detection and turn off flashlight when leaving page
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _scanned = false;
        BarcodeReader.IsDetecting = false;

        try
        {
            if (_torchOn)
            {
                BarcodeReader.IsTorchOn = false;
                _torchOn = false;
            }
        }
        catch { }
    }
}