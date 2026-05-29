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
}