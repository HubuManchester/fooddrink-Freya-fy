namespace NutriLens.Views;

/// <summary>
/// Full screen image viewer with pinch-to-zoom and double-tap to reset
/// </summary>
public partial class FullScreenImagePage : ContentPage
{
    private double _currentScale = 1.0;
    private double _startScale = 1.0;

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
    /// Handle pinch gesture for zooming in and out
    /// </summary>
    private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        switch (e.Status)
        {
            case GestureStatus.Started:
                _startScale = _currentScale;
                FullImage.AnchorX = e.ScaleOrigin.X;
                FullImage.AnchorY = e.ScaleOrigin.Y;
                break;

            case GestureStatus.Running:
                double newScale = _startScale * e.Scale;
                _currentScale = Math.Clamp(newScale, MinScale, MaxScale);
                FullImage.Scale = _currentScale;
                break;

            case GestureStatus.Completed:
                if (_currentScale < 1.1)
                    ResetZoom();
                break;
        }
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
        FullImage.ScaleTo(1.0, 200, Easing.CubicOut);
        FullImage.TranslateTo(0, 0, 200, Easing.CubicOut);
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