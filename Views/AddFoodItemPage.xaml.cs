using NutriLens.Models;

namespace NutriLens.Views;

public partial class AddFoodItemPage : ContentPage
{
    public FoodItem? Result { get; private set; }
    private readonly FoodItem? _existing;

    public AddFoodItemPage(FoodItem? existing = null)
    {
        InitializeComponent();
        _existing = existing;

        if (existing != null)
        {
            TitleLabel.Text = "Edit Food";
            NameEntry.Text = existing.Name;
            CaloriesEntry.Text = existing.Calories.ToString("F0");
            ProteinEntry.Text = existing.Protein.ToString("F1");
            FatEntry.Text = existing.Fat.ToString("F1");
            SugarEntry.Text = existing.Sugar.ToString("F1");
            IngredientsEntry.Text = existing.Ingredients;

            var categories = new[]
            {
                "Meat", "Fish", "Vegetables", "Fruits",
                "Dairy", "Grains", "Snacks", "Drinks", "Other"
            };
            int idx = Array.IndexOf(categories, existing.Category);
            if (idx >= 0) CategoryPicker.SelectedIndex = idx;
        }
        else
        {
            TitleLabel.Text = "Add Food";
            CategoryPicker.SelectedIndex = 0;
        }
    }

    /// <summary>
    /// Validate input fields and save a food item
    /// </summary>
    private async void OnSaveClicked(object sender, EventArgs e)
    {
        string name = NameEntry.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(name))
        {
            await DisplayAlert("Required", "Please enter a food name.", "OK");
            return;
        }

        if (!double.TryParse(CaloriesEntry.Text, out double calories)
            || calories < 0 || calories > 9000)
        {
            await DisplayAlert("Invalid",
                "Please enter valid calories (0-9000).", "OK");
            return;
        }

        double.TryParse(ProteinEntry.Text, out double protein);
        double.TryParse(FatEntry.Text, out double fat);
        double.TryParse(SugarEntry.Text, out double sugar);

        string category = CategoryPicker.SelectedItem?.ToString() ?? "Other";
        string ingredients = IngredientsEntry.Text?.Trim() ?? "";

        Result = new FoodItem
        {
            Id = _existing?.Id ?? 0,
            Name = name,
            Category = category,
            Calories = calories,
            Protein = protein,
            Fat = fat,
            Sugar = sugar,
            Ingredients = ingredients
        };

        await Navigation.PopModalAsync();
    }

    /// <summary>
    /// Cancel editing and close the food item page
    /// </summary>
    private async void OnCancelClicked(object sender, EventArgs e)
    {
        Result = null;
        await Navigation.PopModalAsync();
    }
}