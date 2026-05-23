namespace NutriLens.Views;

/// <summary>
/// Diary page - displays food entries with swipe to delete
/// and daily nutrition summary
/// </summary>
public partial class DiaryPage : ContentPage
{
    // Temporary list to store diary entries until database is connected
    private List<DiaryEntryDisplay> _entries = new List<DiaryEntryDisplay>();

    public DiaryPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Reload entries every time page appears
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadEntries();
    }

    /// <summary>
    /// Load diary entries and update summary totals
    /// </summary>
    private void LoadEntries()
    {
        try
        {
            // Update list
            DiaryList.ItemsSource = null;
            DiaryList.ItemsSource = _entries;

            // Show or hide empty state
            EmptyState.IsVisible = _entries.Count == 0;

            // Calculate totals
            UpdateTotals();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Error loading diary entries: {ex.Message}");
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
    /// Add a new food entry to diary (called from ScannerPage)
    /// </summary>
    public void AddEntry(string foodName, string mealType,
        double calories, double protein, double fat, double sugar)
    {
        var entry = new DiaryEntryDisplay
        {
            FoodName = foodName,
            MealType = mealType,
            Calories = calories,
            Protein = protein,
            Fat = fat,
            Sugar = sugar,
            Date = DateTime.Now
        };

        _entries.Add(entry);
        LoadEntries();
    }

    /// <summary>
    /// Delete entry on left swipe
    /// </summary>
    public void DeleteEntry(DiaryEntryDisplay entry)
    {
        try
        {
            if (entry == null) return;
            _entries.Remove(entry);
            LoadEntries();

            // Vibrate to confirm deletion
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Error deleting entry: {ex.Message}");
        }
    }
}

/// <summary>
/// Model for displaying diary entries in the list
/// </summary>
public class DiaryEntryDisplay
{
    public string FoodName { get; set; } = "";
    public string MealType { get; set; } = "";
    public double Calories { get; set; }
    public double Protein { get; set; }
    public double Fat { get; set; }
    public double Sugar { get; set; }
    public DateTime Date { get; set; }
}