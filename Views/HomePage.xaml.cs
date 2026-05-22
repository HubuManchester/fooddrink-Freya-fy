namespace NutriLens.Views;

/// <summary>
/// Home page - displays daily summary and quick actions
/// </summary>
public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Navigate to Scanner page
    /// </summary>
    private async void OnScanClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ScannerPage");
    }

    /// <summary>
    /// Navigate to Diary page
    /// </summary>
    private async void OnDiaryClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//DiaryPage");
    }
}