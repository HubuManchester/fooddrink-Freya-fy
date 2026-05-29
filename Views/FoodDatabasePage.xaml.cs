using NutriLens.Models;
using NutriLens.Services;
using System.Collections.ObjectModel;

namespace NutriLens.Views;

/// <summary>
/// Food group for CollectionView grouping
/// </summary>
public class FoodGroup : ObservableCollection<FoodItem>
{
    public string Key { get; }
    public string HeaderIcon { get; }
    public string HeaderColor { get; }
    public int Count => this.Items.Count;

    private static readonly Dictionary<string, (string Icon, string Color)> CategoryMeta = new()
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
        var (icon, color) = CategoryMeta.TryGetValue(key, out var m) ? m : ("🍽️", "#EEEEEE");
        HeaderIcon = icon;
        HeaderColor = color;
    }
}

public partial class FoodDatabasePage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private List<FoodItem> _allFoods = new();
    private List<FoodItem> _filteredFoods = new();

    // Category display order
    private static readonly List<string> CategoryOrder = new()
    {
        "Meat", "Fish", "Vegetables", "Fruits",
        "Dairy", "Grains", "Snacks", "Drinks", "Other"
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
    }

    private async Task SeedDefaultFoodsAsync()
    {
        const string seededKey = "default_foods_seeded_v1";
        if (Preferences.Default.Get(seededKey, false)) return;

        var defaults = new List<FoodItem>
        {
            new FoodItem { Name = "Apple",           Category = "Fruits",     Calories = 52,  Protein = 0.3,  Fat = 0.2,  Sugar = 10.4 },
            new FoodItem { Name = "Banana",          Category = "Fruits",     Calories = 89,  Protein = 1.1,  Fat = 0.3,  Sugar = 12.2 },
            new FoodItem { Name = "Orange",          Category = "Fruits",     Calories = 47,  Protein = 0.9,  Fat = 0.1,  Sugar = 9.4  },
            new FoodItem { Name = "Watermelon",      Category = "Fruits",     Calories = 30,  Protein = 0.6,  Fat = 0.2,  Sugar = 6.2  },
            new FoodItem { Name = "Strawberry",      Category = "Fruits",     Calories = 32,  Protein = 0.7,  Fat = 0.3,  Sugar = 4.9  },
            new FoodItem { Name = "Broccoli",        Category = "Vegetables", Calories = 34,  Protein = 2.8,  Fat = 0.4,  Sugar = 1.7  },
            new FoodItem { Name = "Carrot",          Category = "Vegetables", Calories = 41,  Protein = 0.9,  Fat = 0.2,  Sugar = 4.7  },
            new FoodItem { Name = "Spinach",         Category = "Vegetables", Calories = 23,  Protein = 2.9,  Fat = 0.4,  Sugar = 0.4  },
            new FoodItem { Name = "Tomato",          Category = "Vegetables", Calories = 18,  Protein = 0.9,  Fat = 0.2,  Sugar = 2.6  },
            new FoodItem { Name = "Cucumber",        Category = "Vegetables", Calories = 15,  Protein = 0.7,  Fat = 0.1,  Sugar = 1.7  },
            new FoodItem { Name = "Chicken Breast",  Category = "Meat",       Calories = 165, Protein = 31.0, Fat = 3.6,  Sugar = 0.0  },
            new FoodItem { Name = "Beef Steak",      Category = "Meat",       Calories = 250, Protein = 26.0, Fat = 15.0, Sugar = 0.0  },
            new FoodItem { Name = "Pork Belly",      Category = "Meat",       Calories = 518, Protein = 9.3,  Fat = 53.0, Sugar = 0.0  },
            new FoodItem { Name = "Lamb Chop",       Category = "Meat",       Calories = 294, Protein = 25.0, Fat = 21.0, Sugar = 0.0  },
            new FoodItem { Name = "Salmon",          Category = "Fish",       Calories = 208, Protein = 20.0, Fat = 13.0, Sugar = 0.0  },
            new FoodItem { Name = "Tuna",            Category = "Fish",       Calories = 132, Protein = 28.0, Fat = 1.0,  Sugar = 0.0  },
            new FoodItem { Name = "Shrimp",          Category = "Fish",       Calories = 99,  Protein = 24.0, Fat = 0.3,  Sugar = 0.0  },
            new FoodItem { Name = "Whole Milk",      Category = "Dairy",      Calories = 61,  Protein = 3.2,  Fat = 3.3,  Sugar = 4.8  },
            new FoodItem { Name = "Egg",             Category = "Dairy",      Calories = 155, Protein = 13.0, Fat = 11.0, Sugar = 1.1  },
            new FoodItem { Name = "Cheddar Cheese",  Category = "Dairy",      Calories = 402, Protein = 25.0, Fat = 33.0, Sugar = 0.5  },
            new FoodItem { Name = "Greek Yogurt",    Category = "Dairy",      Calories = 59,  Protein = 10.0, Fat = 0.4,  Sugar = 3.2  },
            new FoodItem { Name = "White Rice",      Category = "Grains",     Calories = 130, Protein = 2.7,  Fat = 0.3,  Sugar = 0.0  },
            new FoodItem { Name = "Brown Rice",      Category = "Grains",     Calories = 112, Protein = 2.6,  Fat = 0.9,  Sugar = 0.0  },
            new FoodItem { Name = "White Bread",     Category = "Grains",     Calories = 265, Protein = 9.0,  Fat = 3.2,  Sugar = 5.0  },
            new FoodItem { Name = "Oats",            Category = "Grains",     Calories = 389, Protein = 17.0, Fat = 7.0,  Sugar = 1.0  },
            new FoodItem { Name = "Pasta",           Category = "Grains",     Calories = 131, Protein = 5.0,  Fat = 1.1,  Sugar = 0.6  },
            new FoodItem { Name = "Noodles",         Category = "Grains",     Calories = 138, Protein = 4.5,  Fat = 0.6,  Sugar = 0.3  },
            new FoodItem { Name = "Dark Chocolate",  Category = "Snacks",     Calories = 546, Protein = 5.0,  Fat = 31.0, Sugar = 48.0 },
            new FoodItem { Name = "Potato Chips",    Category = "Snacks",     Calories = 536, Protein = 7.0,  Fat = 35.0, Sugar = 0.4  },
            new FoodItem { Name = "Peanuts",         Category = "Snacks",     Calories = 567, Protein = 26.0, Fat = 49.0, Sugar = 4.7  },
            new FoodItem { Name = "Almonds",         Category = "Snacks",     Calories = 579, Protein = 21.0, Fat = 50.0, Sugar = 3.9  },
            new FoodItem { Name = "Orange Juice",    Category = "Drinks",     Calories = 45,  Protein = 0.7,  Fat = 0.2,  Sugar = 8.4  },
            new FoodItem { Name = "Cola",            Category = "Drinks",     Calories = 42,  Protein = 0.0,  Fat = 0.0,  Sugar = 10.6 },
            new FoodItem { Name = "Coffee (Black)",  Category = "Drinks",     Calories = 2,   Protein = 0.3,  Fat = 0.0,  Sugar = 0.0  },
            new FoodItem { Name = "Green Tea",       Category = "Drinks",     Calories = 1,   Protein = 0.2,  Fat = 0.0,  Sugar = 0.0  },
            new FoodItem { Name = "Fried Rice",      Category = "Other",      Calories = 163, Protein = 4.5,  Fat = 4.5,  Sugar = 1.0  },
            new FoodItem { Name = "Hot Dry Noodles", Category = "Other",      Calories = 152, Protein = 5.0,  Fat = 4.0,  Sugar = 1.5  },
            new FoodItem { Name = "Dumplings",       Category = "Other",      Calories = 193, Protein = 8.0,  Fat = 7.0,  Sugar = 2.0  },
            new FoodItem { Name = "Kung Pao Chicken",Category = "Other",      Calories = 175, Protein = 14.0, Fat = 9.0,  Sugar = 5.0  },
            new FoodItem { Name = "Mapo Tofu",       Category = "Other",      Calories = 98,  Protein = 6.0,  Fat = 6.0,  Sugar = 1.0  },
            new FoodItem { Name = "Spring Rolls",    Category = "Other",      Calories = 209, Protein = 5.0,  Fat = 10.0, Sugar = 3.0  },
        };

        foreach (var food in defaults)
            await _databaseService.SaveFoodAsync(food);

        Preferences.Default.Set(seededKey, true);
    }

    private async Task LoadFoodsAsync()
    {
        try
        {
            _allFoods = await _databaseService.GetAllFoodsAsync();
            _filteredFoods = _allFoods.ToList();
            UpdateDisplay();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load foods: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Build grouped list and update stats
    /// </summary>
    private void UpdateDisplay()
    {
        var groups = BuildGroups(_filteredFoods);
        FoodList.ItemsSource = groups;

        TotalFoodsLabel.Text = _allFoods.Count.ToString();
        AvgCaloriesLabel.Text = _allFoods.Any()
            ? $"{_allFoods.Average(f => f.Calories):F0}" : "0";
        CategoriesLabel.Text = _allFoods
            .Select(f => f.Category).Distinct().Count().ToString();
    }

    /// <summary>
    /// Group foods by category in fixed order
    /// </summary>
    private List<FoodGroup> BuildGroups(List<FoodItem> foods)
    {
        var groups = new List<FoodGroup>();

        // First add categories in defined order
        foreach (var cat in CategoryOrder)
        {
            var items = foods.Where(f => f.Category == cat).ToList();
            if (items.Any())
                groups.Add(new FoodGroup(cat, items));
        }

        // Then any categories not in the defined order
        var knownCats = new HashSet<string>(CategoryOrder);
        var otherCats = foods
            .Select(f => f.Category)
            .Distinct()
            .Where(c => !knownCats.Contains(c));

        foreach (var cat in otherCats)
        {
            var items = foods.Where(f => f.Category == cat).ToList();
            if (items.Any())
                groups.Add(new FoodGroup(cat, items));
        }

        return groups;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        string query = e.NewTextValue?.ToLower() ?? "";

        _filteredFoods = string.IsNullOrEmpty(query)
            ? _allFoods.ToList()
            : _allFoods.Where(f =>
                f.Name.ToLower().Contains(query) ||
                f.Category.ToLower().Contains(query))
              .ToList();

        FoodList.ItemsSource = BuildGroups(_filteredFoods);
    }

    private async void OnAddFoodClicked(object sender, EventArgs e)
    {
        await ShowFoodDialog(null);
    }

    private async void OnFoodTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not FoodItem food) return;
        var detailPage = new FoodDetailPage(food);
        await Navigation.PushModalAsync(detailPage);
    }

    private async void OnEditFoodSwiped(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem &&
            swipeItem.BindingContext is FoodItem food)
        {
            await ShowFoodDialog(food);
        }
    }

    private async void OnDeleteFoodSwiped(object sender, EventArgs e)
    {
        if (sender is SwipeItem swipeItem &&
            swipeItem.BindingContext is FoodItem food)
        {
            bool confirm = await DisplayAlert("Delete Food",
                $"Delete {food.Name}?", "Delete", "Cancel");
            if (!confirm) return;

            bool success = await _databaseService.DeleteFoodAsync(food.Id);
            if (success)
            {
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
                await LoadFoodsAsync();
            }
            else
            {
                await DisplayAlert("Error", "Failed to delete food.", "OK");
            }
        }
    }

    private async Task ShowFoodDialog(FoodItem? existingFood)
    {
        var page = new AddFoodItemPage(existingFood);
        var tcs = new TaskCompletionSource<FoodItem?>();

        page.Disappearing += (s, args) => tcs.TrySetResult(page.Result);
        await Navigation.PushModalAsync(page);

        var food = await tcs.Task;
        if (food == null) return;

        try
        {
            bool success = existingFood != null
                ? await _databaseService.UpdateFoodAsync(food)
                : await _databaseService.SaveFoodAsync(food);

            if (success)
            {
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
                await LoadFoodsAsync();
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

    private async void OnRefreshing(object sender, EventArgs e)
    {
        try
        {
            await LoadFoodsAsync();
        }
        finally
        {
            RefreshView.IsRefreshing = false;
        }
    }
}