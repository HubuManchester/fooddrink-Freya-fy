using ZXing.Net.Maui;

namespace NutriLens.Views;

public partial class BarcodeScannerPage : ContentPage
{
    public string? ScannedBarcode { get; private set; }

    private bool _scanned = false;

    public BarcodeScannerPage()
    {
        InitializeComponent();

        BarcodeReader.BarcodesDetected += OnBarcodesDetected;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _scanned = false;
    }

    private async void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        if (_scanned)
            return;

        var result = e.Results?.FirstOrDefault();

        if (result == null)
            return;

        _scanned = true;

        ScannedBarcode = result.Value;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await DisplayAlert("Scanned Barcode", ScannedBarcode, "OK");

            await Navigation.PopModalAsync();
        });
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        ScannedBarcode = null;

        await Navigation.PopModalAsync();
    }

    private bool _torchOn = false;

    private void OnTorchClicked(object sender, EventArgs e)
    {
        try
        {
            _torchOn = !_torchOn;
            BarcodeReader.IsTorchOn = _torchOn;

            TorchButton.Text = _torchOn ? "🔦 On" : "🔦 Torch";
            TorchButton.BackgroundColor = _torchOn
                ? Color.FromArgb("#FFC107")
                : Color.FromArgb("#555555");
            TorchButton.TextColor = _torchOn
                ? Colors.Black
                : Colors.White;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Torch error: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _scanned = false;
        BarcodeReader.IsDetecting = false;

        // Turn off torch when leaving
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