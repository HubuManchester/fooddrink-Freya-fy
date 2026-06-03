namespace NutriLens.Views;

public partial class MealSuggestionPage : ContentPage
{
    // emoji, name, calories, header color
    private static readonly (string Emoji, string Name, string Calories, string Color)[] Meals =
    [
        ("🥗", "Greek Salad with Grilled Chicken",   "350 kcal", "#4CAF50"),
        ("🍜", "Vegetable Stir Fry with Brown Rice", "420 kcal", "#FF9800"),
        ("🥙", "Wholemeal Wrap with Tuna & Salad",   "380 kcal", "#2196F3"),
        ("🍳", "Scrambled Eggs with Avocado Toast",  "450 kcal", "#FFC107"),
        ("🥣", "Oat Porridge with Berries & Honey",  "320 kcal", "#E91E63"),
        ("🍱", "Salmon with Broccoli & Quinoa",      "480 kcal", "#009688"),
        ("🥘", "Lentil Soup with Wholegrain Bread",  "390 kcal", "#795548"),
        ("🌮", "Black Bean Tacos with Fresh Salsa",  "410 kcal", "#FF5722"),
    ];

    private int _currentIndex;

    public MealSuggestionPage()
    {
        InitializeComponent();
        _currentIndex = new Random().Next(Meals.Length);
        UpdateDisplay();
    }

    /// <summary>
    /// Update the meal suggestion display with the current meal
    /// </summary>
    private void UpdateDisplay()
    {
        var meal = Meals[_currentIndex];

        EmojiLabel.Text = meal.Emoji;
        MealNameLabel.Text = meal.Name;
        CalorieLabel.Text = meal.Calories;
        HeaderGrid.BackgroundColor = Color.FromArgb(meal.Color);
    }

    /// <summary>
    /// Randomly select and display a new meal suggestion
    /// </summary>
    private void OnShuffleClicked(object sender, EventArgs e)
    {
        // Pick a different random meal
        int next;
        do { next = new Random().Next(Meals.Length); }
        while (next == _currentIndex && Meals.Length > 1);

        _currentIndex = next;
        UpdateDisplay();
    }

    /// <summary>
    /// Close the meal suggestion page
    /// </summary>
    private async void OnGotItClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    /// <summary>
    /// Close the page when the background is tapped
    /// </summary>
    private async void OnBackgroundTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}