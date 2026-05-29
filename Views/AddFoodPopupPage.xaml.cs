using NutriLens.Models;

namespace NutriLens.Views;

public partial class AddFoodPopupPage : ContentPage
{
    public DiaryEntry? Result { get; private set; }

    public AddFoodPopupPage()
    {
        InitializeComponent();
        MealTypePicker.SelectedIndex = 0;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        string foodName = FoodNameEntry.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(foodName))
        {
            await DisplayAlert("Required", "Please enter a food name.", "OK");
            return;
        }

        if (!double.TryParse(CaloriesEntry.Text, out double calories)
            || calories < 0 || calories > 5000)
        {
            await DisplayAlert("Invalid", "Please enter valid calories (0-5000).", "OK");
            return;
        }

        double.TryParse(ProteinEntry.Text, out double protein);
        double.TryParse(FatEntry.Text, out double fat);
        double.TryParse(SugarEntry.Text, out double sugar);

        string mealType = MealTypePicker.SelectedItem?.ToString() ?? "Snack";

        Result = new DiaryEntry
        {
            FoodName = foodName,
            MealType = mealType,
            Calories = calories,
            Protein = protein,
            Fat = fat,
            Sugar = sugar,
            Date = DateTime.Now
        };

        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        Result = null;
        await Navigation.PopModalAsync();
    }
}