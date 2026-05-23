using NutriLens.Models;
using NutriLens.Services;

namespace NutriLens.Views;

/// <summary>
/// Diary page - displays food entries loaded from SQLite database
/// with swipe to delete and daily nutrition summary
/// </summary>
public partial class DiaryPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private List<DiaryEntry> _entries = new List<DiaryEntry>();

    public DiaryPage(DatabaseService databaseService)
    {
        InitializeComponent();
        _databaseService = databaseService;
    }

    /// <summary>
    /// Reload entries from database every time page appears
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadEntriesAsync();
    }

    /// <summary>
    /// Load today's diary entries from database
    /// </summary>
    private async Task LoadEntriesAsync()
    {
        try
        {
            _entries = await _databaseService.GetTodayEntriesAsync();

            DiaryList.ItemsSource = null;
            DiaryList.ItemsSource = _entries;

            EmptyState.IsVisible = _entries.Count == 0;

            UpdateTotals();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error",
                $"Failed to load diary: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Calculate and display total nutrition for today
    /// </summary>
    private void UpdateTotals()
    {
        double totalCalories = _entries.Sum(e => e.Calories);
        double totalProtein = _entries.Sum(e => e.Protein);
        double totalFat = _entries.Sum(e => e.Fat);

        TotalCaloriesLabel.Text = $"{totalCalories:F0}";
        TotalProteinLabel.Text = $"{totalProtein:F1}g";
        TotalFatLabel.Text = $"{totalFat:F1}g";
    }

    /// <summary>
    /// Delete entry on left swipe
    /// </summary>
    public async void DeleteEntry(DiaryEntry entry)
    {
        try
        {
            if (entry == null) return;

            bool confirm = await DisplayAlert("Delete Entry",
                $"Delete {entry.FoodName}?", "Delete", "Cancel");

            if (!confirm) return;

            bool success = await _databaseService.DeleteEntryAsync(entry.Id);

            if (success)
            {
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
                await LoadEntriesAsync();
            }
            else
            {
                await DisplayAlert("Error",
                    "Failed to delete entry.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error",
                $"Failed to delete: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Handle left swipe delete gesture
    /// </summary>
    private async void OnDeleteSwipe(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem &&
            swipeItem.BindingContext is DiaryEntry entry)
        {
            await Task.Run(() => DeleteEntry(entry));
        }
    }
}