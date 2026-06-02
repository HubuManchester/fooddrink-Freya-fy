// FullScreenImagePage.xaml.cs
namespace NutriLens.Views;

/// <summary>
/// Full screen image viewer with smooth pinch-to-zoom and double-tap to reset
/// </summary>
public partial class FullScreenImagePage : ContentPage
{
    private double _currentScale = 1.0;
    private double _startScale = 1.0;

    // Translation at the moment the pinch starts
    private double _startTranslationX = 0.0;
    private double _startTranslationY = 0.0;

    private const double MinScale = 1.0;
    private const double MaxScale = 5.0;

    public FullScreenImagePage(ImageSource imageSource)
    {
        InitializeComponent();
        FullImage.Source = imageSource;

        // Hide hint after 3 seconds
        Dispatcher.DispatchDelayed(TimeSpan.FromSeconds(3), () =>
        {
            HintLabel.FadeTo(0, 500);
        });
    }

    /// <summary>
    /// Handle pinch gesture — smooth zoom anchored at the pinch midpoint
    /// </summary>
    private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        switch (e.Status)
        {
            case GestureStatus.Started:
                // Snapshot current state
                _startScale = _currentScale;
                _startTranslationX = FullImage.TranslationX;
                _startTranslationY = FullImage.TranslationY;

                // AnchorX/Y are in [0,1] relative to the view's own bounds.
                // Keep them at 0.5 (center) and compensate via Translation instead,
                // because changing Anchor mid-gesture causes a visual jump.
                FullImage.AnchorX = 0.5;
                FullImage.AnchorY = 0.5;
                break;

            case GestureStatus.Running:
                // 1. Clamp new scale
                double rawScale = _startScale * e.Scale;
                double newScale = Math.Clamp(rawScale, MinScale, MaxScale);

                // 2. Compute how much the scale changed from the start of this gesture
                double scaleDelta = newScale / _startScale;

                // 3. Pinch origin in [-0.5, 0.5] relative to view center
                double originX = e.ScaleOrigin.X - 0.5;
                double originY = e.ScaleOrigin.Y - 0.5;

                // 4. View size (use screen size as approximation when FullImage size not ready)
                double viewW = FullImage.Width > 0 ? FullImage.Width : DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
                double viewH = FullImage.Height > 0 ? FullImage.Height : DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density;

                // 5. Shift in device-independent pixels to keep pinch origin stationary
                double shiftX = originX * viewW * (1 - scaleDelta);
                double shiftY = originY * viewH * (1 - scaleDelta);

                double newTx = _startTranslationX + shiftX;
                double newTy = _startTranslationY + shiftY;

                // 6. Clamp translation so image never floats away from screen
                ClampTranslation(newScale, viewW, viewH, ref newTx, ref newTy);

                // 7. Apply — no animation during active gesture for zero latency
                FullImage.Scale = newScale;
                FullImage.TranslationX = newTx;
                FullImage.TranslationY = newTy;
                _currentScale = newScale;
                break;

            case GestureStatus.Completed:
                // Snap back to 1× if user only pinched a tiny bit
                if (_currentScale < 1.05)
                    ResetZoom();
                break;
        }
    }

    /// <summary>
    /// Keep the image within a sensible boundary so it can't be flung off-screen.
    /// </summary>
    private static void ClampTranslation(double scale, double viewW, double viewH,
                                         ref double tx, ref double ty)
    {
        // Extra pixels that stick out on each side after scaling
        double maxTx = viewW * (scale - 1) / 2.0;
        double maxTy = viewH * (scale - 1) / 2.0;

        tx = Math.Clamp(tx, -maxTx, maxTx);
        ty = Math.Clamp(ty, -maxTy, maxTy);
    }

    /// <summary>
    /// Double tap to reset zoom back to original size
    /// </summary>
    private void OnDoubleTapped(object sender, TappedEventArgs e)
    {
        ResetZoom();
    }

    /// <summary>
    /// Animate image back to original scale and position
    /// </summary>
    private void ResetZoom()
    {
        _currentScale = 1.0;
        _startTranslationX = 0.0;
        _startTranslationY = 0.0;
        FullImage.ScaleTo(1.0, 220, Easing.CubicOut);
        FullImage.TranslateTo(0, 0, 220, Easing.CubicOut);
    }

    /// <summary>
    /// Close full screen view
    /// </summary>
    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    /// <summary>
    /// Allow hardware back button to close
    /// </summary>
    protected override bool OnBackButtonPressed()
    {
        Navigation.PopModalAsync();
        return true;
    }
}