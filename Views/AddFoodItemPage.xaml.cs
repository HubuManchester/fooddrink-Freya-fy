using NutriLens.Models;

namespace NutriLens.Views;

public partial class AddFoodItemPage : ContentPage
{
    public FoodItem? Result { get; private set; }
    private readonly FoodItem? _existing;
    private string? _selectedImagePath;

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

            // Load existing image if present
            if (!string.IsNullOrEmpty(existing.ImagePath) &&
                File.Exists(existing.ImagePath))
            {
                _selectedImagePath = existing.ImagePath;
                ShowImagePreview(existing.ImagePath);
            }
        }
        else
        {
            TitleLabel.Text = "Add Food";
            CategoryPicker.SelectedIndex = 0;
        }
    }

    // ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤
    // Photo: Take with Camera
    // ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤

    /// <summary>
    /// Capture a photo using the device camera
    /// </summary>
    private async void OnTakePhotoClicked(object sender, EventArgs e)
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await DisplayAlert("Not Supported",
                    "Camera capture is not supported on this device.", "OK");
                return;
            }

            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo != null)
                await ProcessPickedPhoto(photo);
        }
        catch (PermissionException)
        {
            await DisplayAlert("Permission Required",
                "Camera permission is required to take photos.", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Camera error: {ex.Message}");
            await DisplayAlert("Error", "Could not open the camera.", "OK");
        }
    }

    // ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤
    // Photo: Pick from Gallery
    // ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤

    /// <summary>
    /// Pick a photo from the device photo library
    /// </summary>
    private async void OnPickPhotoClicked(object sender, EventArgs e)
    {
        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync(
                new MediaPickerOptions
                {
                    Title = "Select a food photo"
                });

            if (photo != null)
                await ProcessPickedPhoto(photo);
        }
        catch (PermissionException)
        {
            await DisplayAlert("Permission Required",
                "Photo library permission is required to pick photos.", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Gallery error: {ex.Message}");
            await DisplayAlert("Error", "Could not open the photo library.", "OK");
        }
    }

    // ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤
    // Photo: Remove
    // ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤

    /// <summary>
    /// Remove the currently selected photo
    /// </summary>
    private void OnRemovePhotoClicked(object sender, EventArgs e)
    {
        _selectedImagePath = null;
        PreviewImage.Source = null;
        ImagePreviewFrame.IsVisible = false;
        ImagePlaceholderFrame.IsVisible = true;
        RemovePhotoButton.IsVisible = false;
    }

    // ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤
    // Helpers
    // ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤

    /// <summary>
    /// Copy picked/captured photo to app's local storage and show preview
    /// </summary>
    private async Task ProcessPickedPhoto(FileResult photo)
    {
        // Copy to app data directory so path stays valid after cache clear
        string localFolder = FileSystem.AppDataDirectory;
        string fileName = $"food_{DateTime.Now:yyyyMMdd_HHmmss}{Path.GetExtension(photo.FileName)}";
        string destPath = Path.Combine(localFolder, fileName);

        using (var srcStream = await photo.OpenReadAsync())
        using (var destStream = File.OpenWrite(destPath))
        {
            await srcStream.CopyToAsync(destStream);
        }

        _selectedImagePath = destPath;
        ShowImagePreview(destPath);
    }

    /// <summary>
    /// Update UI to display the selected image preview
    /// </summary>
    private void ShowImagePreview(string imagePath)
    {
        PreviewImage.Source = ImageSource.FromFile(imagePath);
        ImagePlaceholderFrame.IsVisible = false;
        ImagePreviewFrame.IsVisible = true;
        RemovePhotoButton.IsVisible = true;
    }

    // ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤
    // Save / Cancel
    // ©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤©¤

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
                "Please enter valid calories (0¨C9000).", "OK");
            return;
        }

        if (!double.TryParse(ProteinEntry.Text, out double protein))
        {
            await DisplayAlert("Error", "Invalid protein value", "OK");
            return;
        }

        if (!double.TryParse(FatEntry.Text, out double fat))
        {
            await DisplayAlert("Error", "Invalid fat value", "OK");
            return;
        }

        if (!double.TryParse(SugarEntry.Text, out double sugar))
        {
            await DisplayAlert("Error", "Invalid sugar value", "OK");
            return;
        }

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
            Ingredients = ingredients,
            ImagePath = _selectedImagePath   
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