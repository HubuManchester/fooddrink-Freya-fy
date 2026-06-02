namespace NutriLens.Views;

public partial class FullScreenImagePage : ContentPage
{
    private double _currentScale = 1.0;
    private const double MaxScale = 5.0;
    private const double MinScale = 0.5;
    private bool _isClosing = false;

    public FullScreenImagePage(ImageSource imageSource)
    {
        InitializeComponent();
        FullImage.Source = imageSource;

        Dispatcher.DispatchDelayed(TimeSpan.FromSeconds(3), () =>
        {
            _ = HintLabel.FadeTo(0, 500);
        });
    }

    /// <summary>
    /// Handles the pinch gesture with a robust matrix anchor algorithm supporting both zoom in and out.
    /// </summary>
    private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
    {
        switch (e.Status)
        {
            case GestureStatus.Started:
                FullImage.AnchorX = 0.5;
                FullImage.AnchorY = 0.5;
                break;

            case GestureStatus.Running:
                // 1. Calculate the target scale securely
                double targetScale = _currentScale * e.Scale;
                double newScale = Math.Clamp(targetScale, MinScale, MaxScale);

                // 2. Get viewport dimensions
                double viewW = FullImage.Width > 0 ? FullImage.Width : DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
                double viewH = FullImage.Height > 0 ? FullImage.Height : DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density;

                // 3. Robust focal point translation formula (Fixes the zoom-out lock bug)
                // Maps ScaleOrigin from [0, 1] to absolute pixels relative to center
                double pinchX = (e.ScaleOrigin.X - 0.5) * viewW;
                double pinchY = (e.ScaleOrigin.Y - 0.5) * viewH;

                // Adjust translation based on the actual scale change ratio of this single step
                double newTx = FullImage.TranslationX - (pinchX * (e.Scale - 1) * _currentScale);
                double newTy = FullImage.TranslationY - (pinchY * (e.Scale - 1) * _currentScale);

                // 4. Apply boundaries based on scale state
                if (newScale >= 1.0)
                {
                    ClampTranslation(newScale, viewW, viewH, ref newTx, ref newTy);
                }
                else
                {
                    newTx = double.Lerp(newTx, 0, 0.1);
                    newTy = double.Lerp(newTy, 0, 0.1);
                }

                // 5. Update render states
                FullImage.Scale = newScale;
                FullImage.TranslationX = newTx;
                FullImage.TranslationY = newTy;

                _currentScale = newScale;
                break;

            case GestureStatus.Completed:
                if (_currentScale < 1.0)
                {
                    ResetZoom();
                }
                break;
        }
    }

    /// <summary>
    /// Restricts image panning translation to stay within visible screen bounds.
    /// </summary>
    private static void ClampTranslation(double scale, double viewW, double viewH,
                                         ref double tx, ref double ty)
    {
        double maxTx = Math.Max(0, (viewW * scale - viewW) / 2.0);
        double maxTy = Math.Max(0, (viewH * scale - viewH) / 2.0);

        tx = Math.Clamp(tx, -maxTx, maxTx);
        ty = Math.Clamp(ty, -maxTy, maxTy);
    }

    /// <summary>
    /// Handles the double-tap gesture to trigger a viewport reset.
    /// </summary>
    private void OnDoubleTapped(object sender, TappedEventArgs e)
    {
        ResetZoom();
    }

    /// <summary>
    /// Smoothly animates the image scale and position back to their original states.
    /// </summary>
    private void ResetZoom()
    {
        _currentScale = 1.0;

        _ = Task.WhenAll(
            FullImage.ScaleTo(1.0, 250, Easing.CubicOut),
            FullImage.TranslateTo(0, 0, 250, Easing.CubicOut)
        );
    }

    /// <summary>
    /// Handles the close button tap event.
    /// </summary>
    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await SafePopModalAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = SafePopModalAsync();
        return true;
    }

    /// <summary>
    /// Performs a thread-safe page pop to prevent double-tap navigation crashes.
    /// </summary>
    private async Task SafePopModalAsync()
    {
        if (_isClosing) return;
        _isClosing = true;

        try
        {
            if (Navigation.ModalStack.Count > 0)
            {
                await Navigation.PopModalAsync();
            }
            else
            {
                await Navigation.PopAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Navigation Error] {ex.Message}");
        }
        finally
        {
            _isClosing = false;
        }
    }
}