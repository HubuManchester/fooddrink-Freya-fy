namespace NutriLens.Views;

/// <summary>
/// Diary page - displays food entries and nutrition history
/// </summary>
public partial class DiaryPage : ContentPage
{
    public DiaryPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Will load diary entries from database later
    }
}