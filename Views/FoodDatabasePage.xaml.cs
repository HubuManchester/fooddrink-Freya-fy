using NutriLens.Models;
using NutriLens.Services;
using System.Collections.ObjectModel;

namespace NutriLens.Views;

public partial class FoodGroup : ObservableCollection<FoodItem>
{
    public string Key { get; }
    public string HeaderIcon { get; }
    public string HeaderColor { get; }
    public new int Count => this.Items.Count;

    private static readonly Dictionary<string, (string Icon, string Color)>
        CategoryMeta = new()
        {
            { "Meat",       ("🥩", "#FFEBEE") },
            { "Fish",       ("🐟", "#E0F7FA") },
            { "Vegetables", ("🥦", "#E8F5E9") },
            { "Fruits",     ("🍎", "#FFF9C4") },
            { "Dairy",      ("🥛", "#F3E5F5") },
            { "Grains",     ("🌾", "#FFF8E1") },
            { "Snacks",     ("🍫", "#FCE4EC") },
            { "Drinks",     ("🥤", "#E1F5FE") },
            { "Other",      ("🍳", "#F5F5F5") },
        };

    public FoodGroup(string key, IEnumerable<FoodItem> items) : base(items)
    {
        Key = key;
        var (icon, color) = CategoryMeta.TryGetValue(key, out var m)
            ? m : ("🍽️", "#EEEEEE");
        HeaderIcon = icon;
        HeaderColor = color;
    }
}

public partial class FoodDatabasePage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private List<FoodItem> _allFoods = [];
    private List<FoodItem> _filteredFoods = [];
    private string _selectedCategory = "All";

    private static readonly List<string> CategoryOrder =
    [
        "All", "Meat", "Fish", "Vegetables", "Fruits",
        "Dairy", "Grains", "Snacks", "Drinks", "Other"
    ];

    private static readonly Dictionary<string, string> CategoryIcons = new()
    {
        { "All",        "🍽️" },
        { "Meat",       "🥩" },
        { "Fish",       "🐟" },
        { "Vegetables", "🥦" },
        { "Fruits",     "🍎" },
        { "Dairy",      "🥛" },
        { "Grains",     "🌾" },
        { "Snacks",     "🍫" },
        { "Drinks",     "🥤" },
        { "Other",      "🍳" },
    };

    public FoodDatabasePage(DatabaseService databaseService)
    {
        InitializeComponent();
        _databaseService = databaseService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await SeedDefaultFoodsAsync();
        await LoadFoodsAsync();
        BuildCategoryNav();
    }

    /// <summary>
    /// Build the horizontal category navigation bar
    /// </summary>
    private void BuildCategoryNav()
    {
        CategoryNav.Children.Clear();

        foreach (var cat in CategoryOrder)
        {
            bool isSelected = cat == _selectedCategory;
            string icon = CategoryIcons.TryGetValue(cat, out var ic) ? ic : "🍽️";

            var frame = new Frame
            {
                BackgroundColor = isSelected
                    ? Color.FromArgb("#4CAF50")
                    : Colors.Transparent,
                CornerRadius = 0,
                Padding = new Thickness(0, 12),
                BorderColor = Colors.Transparent,
                HasShadow = false
            };

            var stack = new VerticalStackLayout
            {
                Spacing = 4,
                HorizontalOptions = LayoutOptions.Center
            };

            stack.Add(new Label
            {
                Text = icon,
                FontSize = 20,
                HorizontalOptions = LayoutOptions.Center
            });

            stack.Add(new Label
            {
                Text = cat == "Vegetables" ? "Vegs" :
                       cat == "Snacks" ? "Snack" : cat,
                FontSize = 10,
                HorizontalOptions = LayoutOptions.Center,
                TextColor = isSelected ? Colors.White : Colors.Gray,
                HorizontalTextAlignment = TextAlignment.Center
            });

            frame.Content = stack;

            var tap = new TapGestureRecognizer();
            string captured = cat;
            tap.Tapped += (s, e) => OnCategorySelected(captured);
            frame.GestureRecognizers.Add(tap);

            CategoryNav.Children.Add(frame);
        }
    }

    /// <summary>
    /// Filter foods when a category is selected
    /// </summary>
    private void OnCategorySelected(string category)
    {
        _selectedCategory = category;
        BuildCategoryNav();

        string query = SearchBar.Text?.ToLower() ?? "";

        _filteredFoods = [.. _allFoods.Where(f =>
            (category == "All" || f.Category == category) &&
            (string.IsNullOrEmpty(query) ||
             f.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             f.Category.Contains(query, StringComparison.OrdinalIgnoreCase)))];
                FoodList.ItemsSource = BuildGroups(_filteredFoods);
            }

    /// <summary>
    /// Seed the database with default food records on first launch
    /// </summary>
    private async Task SeedDefaultFoodsAsync()
    {
        const string seededKey = "default_foods_seeded_v2";
        if (Preferences.Default.Get(seededKey, false)) return;

        var defaults = new List<FoodItem>
{
        // Fruits
        new() { Name="Apple",           Category="Fruits",     Calories=52,  Protein=0.3,  Fat=0.2,  Sugar=10.4, Ingredients="apple" },
        new() { Name="Banana",          Category="Fruits",     Calories=89,  Protein=1.1,  Fat=0.3,  Sugar=12.2, Ingredients="banana" },
        new() { Name="Orange",          Category="Fruits",     Calories=47,  Protein=0.9,  Fat=0.1,  Sugar=9.4,  Ingredients="orange" },
        new() { Name="Watermelon",      Category="Fruits",     Calories=30,  Protein=0.6,  Fat=0.2,  Sugar=6.2,  Ingredients="watermelon" },
        new() { Name="Strawberry",      Category="Fruits",     Calories=32,  Protein=0.7,  Fat=0.3,  Sugar=4.9,  Ingredients="strawberry" },
        // Vegetables
        new() { Name="Broccoli",        Category="Vegetables", Calories=34,  Protein=2.8,  Fat=0.4,  Sugar=1.7,  Ingredients="broccoli" },
        new() { Name="Carrot",          Category="Vegetables", Calories=41,  Protein=0.9,  Fat=0.2,  Sugar=4.7,  Ingredients="carrot" },
        new() { Name="Spinach",         Category="Vegetables", Calories=23,  Protein=2.9,  Fat=0.4,  Sugar=0.4,  Ingredients="spinach" },
        new() { Name="Tomato",          Category="Vegetables", Calories=18,  Protein=0.9,  Fat=0.2,  Sugar=2.6,  Ingredients="tomato" },
        new() { Name="Cucumber",        Category="Vegetables", Calories=15,  Protein=0.7,  Fat=0.1,  Sugar=1.7,  Ingredients="cucumber" },
        // Meat
        new() { Name="Chicken Breast",  Category="Meat",       Calories=165, Protein=31.0, Fat=3.6,  Sugar=0.0,  Ingredients="chicken, salt, pepper" },
        new() { Name="Beef Steak",      Category="Meat",       Calories=250, Protein=26.0, Fat=15.0, Sugar=0.0,  Ingredients="beef, salt, butter, garlic" },
        new() { Name="Pork Belly",      Category="Meat",       Calories=518, Protein=9.3,  Fat=53.0, Sugar=0.0,  Ingredients="pork, soy sauce, garlic, ginger" },
        new() { Name="Lamb Chop",       Category="Meat",       Calories=294, Protein=25.0, Fat=21.0, Sugar=0.0,  Ingredients="lamb, rosemary, garlic, olive oil" },
        // Fish
        new() { Name="Salmon",          Category="Fish",       Calories=208, Protein=20.0, Fat=13.0, Sugar=0.0,  Ingredients="salmon, lemon, dill, butter" },
        new() { Name="Tuna",            Category="Fish",       Calories=132, Protein=28.0, Fat=1.0,  Sugar=0.0,  Ingredients="tuna, salt" },
        new() { Name="Shrimp",          Category="Fish",       Calories=99,  Protein=24.0, Fat=0.3,  Sugar=0.0,  Ingredients="shrimp, garlic, butter, lemon" },
        // Dairy
        new() { Name="Whole Milk",      Category="Dairy",      Calories=61,  Protein=3.2,  Fat=3.3,  Sugar=4.8,  Ingredients="milk" },
        new() { Name="Egg",             Category="Dairy",      Calories=155, Protein=13.0, Fat=11.0, Sugar=1.1,  Ingredients="egg" },
        new() { Name="Cheddar Cheese",  Category="Dairy",      Calories=402, Protein=25.0, Fat=33.0, Sugar=0.5,  Ingredients="milk, cheese culture, salt, enzymes" },
        new() { Name="Greek Yogurt",    Category="Dairy",      Calories=59,  Protein=10.0, Fat=0.4,  Sugar=3.2,  Ingredients="milk, live cultures" },
        // Grains
        new() { Name="White Rice",      Category="Grains",     Calories=130, Protein=2.7,  Fat=0.3,  Sugar=0.0,  Ingredients="white rice" },
        new() { Name="Brown Rice",      Category="Grains",     Calories=112, Protein=2.6,  Fat=0.9,  Sugar=0.0,  Ingredients="brown rice" },
        new() { Name="White Bread",     Category="Grains",     Calories=265, Protein=9.0,  Fat=3.2,  Sugar=5.0,  Ingredients="wheat flour, water, yeast, salt, sugar" },
        new() { Name="Oats",            Category="Grains",     Calories=389, Protein=17.0, Fat=7.0,  Sugar=1.0,  Ingredients="oats" },
        new() { Name="Pasta",           Category="Grains",     Calories=131, Protein=5.0,  Fat=1.1,  Sugar=0.6,  Ingredients="wheat flour, water, egg" },
        // Snacks
        new() { Name="Dark Chocolate",  Category="Snacks",     Calories=546, Protein=5.0,  Fat=31.0, Sugar=48.0, Ingredients="cocoa, sugar, cocoa butter, vanilla" },
        new() { Name="Potato Chips",    Category="Snacks",     Calories=536, Protein=7.0,  Fat=35.0, Sugar=0.4,  Ingredients="potato, vegetable oil, salt" },
        new() { Name="Peanuts",         Category="Snacks",     Calories=567, Protein=26.0, Fat=49.0, Sugar=4.7,  Ingredients="peanut, salt" },
        new() { Name="Almonds",         Category="Snacks",     Calories=579, Protein=21.0, Fat=50.0, Sugar=3.9,  Ingredients="almonds" },
        // Drinks
        new() { Name="Orange Juice",    Category="Drinks",     Calories=45,  Protein=0.7,  Fat=0.2,  Sugar=8.4,  Ingredients="orange" },
        new() { Name="Cola",            Category="Drinks",     Calories=42,  Protein=0.0,  Fat=0.0,  Sugar=10.6, Ingredients="water, sugar, caramel color, phosphoric acid, natural flavors, caffeine" },
        new() { Name="Coffee (Black)",  Category="Drinks",     Calories=2,   Protein=0.3,  Fat=0.0,  Sugar=0.0,  Ingredients="coffee, water" },
        new() { Name="Green Tea",       Category="Drinks",     Calories=1,   Protein=0.2,  Fat=0.0,  Sugar=0.0,  Ingredients="green tea leaves, water" },
        // New drinks
        new() { Name="Coconut Latte",   Category="Drinks",     Calories=180, Protein=2.5,  Fat=8.0,  Sugar=22.0, Ingredients="espresso, coconut milk, sugar syrup, ice" },
        new() { Name="Matcha Latte",    Category="Drinks",     Calories=160, Protein=3.0,  Fat=5.0,  Sugar=18.0, Ingredients="matcha powder, milk, sugar, water" },
        new() { Name="Brown Sugar Milk Tea", Category="Drinks", Calories=280, Protein=3.5, Fat=6.0,  Sugar=38.0, Ingredients="black tea, milk, brown sugar, tapioca pearls" },
        new() { Name="Lemon Tea",       Category="Drinks",     Calories=80,  Protein=0.2,  Fat=0.0,  Sugar=18.0, Ingredients="black tea, lemon, honey, water" },
        // Chinese dishes
        new() { Name="Fried Rice",      Category="Other",      Calories=163, Protein=4.5,  Fat=4.5,  Sugar=1.0,  Ingredients="white rice, egg, soy sauce, green onion, vegetable oil, salt" },
        new() { Name="Dumplings",       Category="Other",      Calories=193, Protein=8.0,  Fat=7.0,  Sugar=2.0,  Ingredients="wheat flour, pork, cabbage, ginger, soy sauce, sesame oil" },
        new() { Name="Spring Rolls",    Category="Other",      Calories=209, Protein=5.0,  Fat=10.0, Sugar=3.0,  Ingredients="wheat flour wrapper, pork, cabbage, carrot, vegetable oil" },
        new() { Name="Steamed Egg with Shrimp", Category="Other", Calories=95, Protein=12.0, Fat=4.5, Sugar=0.5, Ingredients="egg, shrimp, soy sauce, sesame oil, green onion, water" },
        new() { Name="Kung Pao Chicken", Category="Other",     Calories=175, Protein=14.0, Fat=9.0,  Sugar=5.0,  Ingredients="chicken, peanut, dried chili, soy sauce, vinegar, sugar, garlic, ginger" },
        new() { Name="Mapo Tofu",       Category="Other",      Calories=98,  Protein=6.0,  Fat=6.0,  Sugar=1.0,  Ingredients="tofu, pork mince, doubanjiang, soy sauce, garlic, ginger, Sichuan pepper, green onion" },
        new() { Name="Hot Dry Noodles", Category="Other",      Calories=152, Protein=5.0,  Fat=4.0,  Sugar=1.5,  Ingredients="wheat noodles, sesame paste, soy sauce, chili oil, green onion, garlic" },
        new() { Name="Braised Pork",    Category="Other",      Calories=320, Protein=15.0, Fat=22.0, Sugar=8.0,  Ingredients="pork belly, soy sauce, sugar, rice wine, ginger, star anise, cinnamon" },
        new() { Name="Egg Fried Rice",  Category="Other",      Calories=210, Protein=7.0,  Fat=8.0,  Sugar=1.0,  Ingredients="white rice, egg, green onion, soy sauce, vegetable oil, salt" },
        new() { Name="Tom Yum Soup",    Category="Other",      Calories=80,  Protein=6.0,  Fat=3.0,  Sugar=2.0,  Ingredients="shrimp, lemongrass, galangal, kaffir lime leaves, chili, fish sauce, lime juice, mushroom" },
    };

        foreach (var food in defaults)
            await _databaseService.SaveFoodAsync(food);

        Preferences.Default.Set(seededKey, true);
    }

    /// <summary>
    /// Load food records from the database and refresh the UI
    /// </summary>
    private async Task LoadFoodsAsync()
    {
        try
        {
            _allFoods = await _databaseService.GetAllFoodsAsync();
            _filteredFoods = _selectedCategory == "All"
                ? [.. _allFoods]
                : [.. _allFoods.Where(f => f.Category == _selectedCategory)];

            FoodList.ItemsSource = BuildGroups(_filteredFoods);
            UpdateStats();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load foods: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Update food statistics displayed on the page
    /// </summary>
    private void UpdateStats()
    {
        TotalFoodsLabel.Text = _allFoods.Count.ToString();
        AvgCaloriesLabel.Text = _allFoods.Count > 0
            ? $"{_allFoods.Average(f => f.Calories):F0}" : "0";
        CategoriesLabel.Text = _allFoods
            .Select(f => f.Category).Distinct().Count().ToString();
    }

    /// <summary>
    /// Group foods by category for CollectionView display
    /// </summary>
    private static List<FoodGroup> BuildGroups(List<FoodItem> foods)
    {
        var groups = new List<FoodGroup>();
        var knownCats = new HashSet<string>(CategoryOrder);

        foreach (var cat in CategoryOrder.Skip(1))
        {
            var items = foods.Where(f => f.Category == cat).ToList();
            if (items.Count > 0)
                groups.Add(new FoodGroup(cat, items));
        }

        foreach (var cat in foods
            .Select(f => f.Category).Distinct()
            .Where(c => !knownCats.Contains(c)))
        {
            var items = foods.Where(f => f.Category == cat).ToList();
            if (items.Count > 0)
                groups.Add(new FoodGroup(cat, items));
        }

        return groups;
    }

    /// <summary>
    /// Filter foods based on the search text input
    /// </summary>
    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        string query = e.NewTextValue?.ToLower() ?? "";

        _filteredFoods = [
            .. _allFoods.Where(f =>
        (_selectedCategory == "All" || f.Category == _selectedCategory) &&
        (string.IsNullOrEmpty(query) ||
         f.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
         f.Category.Contains(query, StringComparison.OrdinalIgnoreCase)))
        ];

        FoodList.ItemsSource = BuildGroups(_filteredFoods);
    }

    /// <summary>
    /// Open the add food page and save a new food item
    /// </summary>
    private async void OnAddFoodClicked(object sender, EventArgs e)
    {
        var page = new AddFoodItemPage(null);
        var tcs = new TaskCompletionSource<FoodItem?>();

        page.Disappearing += (s, args) => tcs.TrySetResult(page.Result);
        await Navigation.PushModalAsync(page);

        var food = await tcs.Task;
        if (food == null) return;

        try
        {
            bool success = await _databaseService.SaveFoodAsync(food);
            if (success)
            {
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
                await LoadFoodsAsync();
                BuildCategoryNav();
            }
            else
            {
                await DisplayAlert("Error", "Failed to save food.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to save: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Navigate to the detail page of the selected food item
    /// </summary>
    private async void OnFoodTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not FoodItem food) return;
        await Navigation.PushAsync(new FoodDetailPage(food));
    }

    /// <summary>
    /// Open the edit page and update an existing food item
    /// </summary>
    private async void OnEditFoodSwiped(object sender, EventArgs e)
    {
        FoodItem? existing = null;

        if (sender is SwipeItem si) existing = si.BindingContext as FoodItem;
        else if (sender is SwipeItemView siv) existing = siv.BindingContext as FoodItem;

        if (existing == null) return;

        var page = new AddFoodItemPage(existing);
        var tcs = new TaskCompletionSource<FoodItem?>();

        page.Disappearing += (s, args) => tcs.TrySetResult(page.Result);
        await Navigation.PushModalAsync(page);

        var food = await tcs.Task;
        if (food == null) return;

        try
        {
            bool success = await _databaseService.UpdateFoodAsync(food);
            if (success)
            {
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
                await LoadFoodsAsync();
                BuildCategoryNav();
            }
            else
            {
                await DisplayAlert("Error", "Failed to update food.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to update: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Delete a food item after user confirmation
    /// </summary>
    private async void OnDeleteFoodSwiped(object sender, EventArgs e)
    {
        FoodItem? food = null;

        if (sender is SwipeItem si) food = si.BindingContext as FoodItem;
        else if (sender is SwipeItemView siv) food = siv.BindingContext as FoodItem;

        if (food == null) return;

        bool confirm = await DisplayAlert("Delete Food",
            $"Delete {food.Name}?", "Delete", "Cancel");
        if (!confirm) return;

        bool success = await _databaseService.DeleteFoodAsync(food.Id);
        if (success)
        {
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
            await LoadFoodsAsync();
            BuildCategoryNav();
        }
        else
        {
            await DisplayAlert("Error", "Failed to delete food.", "OK");
        }
    }

    /// <summary>
    /// Refresh food data when pull-to-refresh is triggered
    /// </summary>
    private async void OnRefreshing(object sender, EventArgs e)
    {
        try
        {
            await LoadFoodsAsync();
            BuildCategoryNav();
        }
        finally
        {
            RefreshView.IsRefreshing = false;
        }
    }
}