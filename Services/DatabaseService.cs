using SQLite;
using NutriLens.Models;

namespace NutriLens.Services;

/// <summary>
/// Handles all SQLite database operations for diary entries and user settings
/// </summary>
public class DatabaseService
{
    private SQLiteAsyncConnection? _database;

    /// <summary>
    /// Initialise database connection and create tables if they don't exist
    /// </summary>
    public async Task InitAsync()
    {
        try
        {
            if (_database != null) return;

            string dbPath = Path.Combine(
                FileSystem.AppDataDirectory, "nutrilens.db3");

            _database = new SQLiteAsyncConnection(dbPath);

            // Create tables
            await _database.CreateTableAsync<DiaryEntry>();
            await _database.CreateTableAsync<UserSettings>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Database init error: {ex.Message}");
        }
    }

    // Diary Entry Methods

    /// <summary>
    /// Save a new food entry to the diary
    /// </summary>
    public async Task<bool> SaveEntryAsync(DiaryEntry entry)
    {
        try
        {
            await InitAsync();
            await _database!.InsertAsync(entry);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Save entry error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get all diary entries for today
    /// </summary>
    public async Task<List<DiaryEntry>> GetTodayEntriesAsync()
    {
        try
        {
            await InitAsync();
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            return await _database!.Table<DiaryEntry>()
                .Where(e => e.Date >= today && e.Date < tomorrow)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Get entries error: {ex.Message}");
            return new List<DiaryEntry>();
        }
    }

    /// <summary>
    /// Get all diary entries for a specific date range
    /// </summary>
    public async Task<List<DiaryEntry>> GetEntriesByDateAsync(
        DateTime from, DateTime to)
    {
        try
        {
            await InitAsync();
            return await _database!.Table<DiaryEntry>()
                .Where(e => e.Date >= from && e.Date < to)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Get entries by date error: {ex.Message}");
            return new List<DiaryEntry>();
        }
    }

    /// <summary>
    /// Delete a diary entry by ID
    /// </summary>
    public async Task<bool> DeleteEntryAsync(int id)
    {
        try
        {
            await InitAsync();
            await _database!.DeleteAsync<DiaryEntry>(id);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Delete entry error: {ex.Message}");
            return false;
        }
    }

    // User Settings Methods

    /// <summary>
    /// Get user settings, creates default if none exist
    /// </summary>
    public async Task<UserSettings> GetSettingsAsync()
    {
        try
        {
            await InitAsync();
            var settings = await _database!.Table<UserSettings>()
                .FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new UserSettings();
                await _database.InsertAsync(settings);
            }

            return settings;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Get settings error: {ex.Message}");
            return new UserSettings();
        }
    }

    /// <summary>
    /// Save user settings to database
    /// </summary>
    public async Task<bool> SaveSettingsAsync(UserSettings settings)
    {
        try
        {
            await InitAsync();
            await _database!.InsertOrReplaceAsync(settings);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Save settings error: {ex.Message}");
            return false;
        }
    }

    // Food Item Methods

    /// <summary>
    /// Get all food items from database
    /// </summary>
    public async Task<List<FoodItem>> GetAllFoodsAsync()
    {
        try
        {
            await InitAsync();
            await _database!.CreateTableAsync<FoodItem>();
            return await _database.Table<FoodItem>()
                .OrderBy(f => f.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Get foods error: {ex.Message}");
            return new List<FoodItem>();
        }
    }

    /// <summary>
    /// Save new food item to database
    /// </summary>
    public async Task<bool> SaveFoodAsync(FoodItem food)
    {
        try
        {
            await InitAsync();
            await _database!.CreateTableAsync<FoodItem>();
            await _database.InsertAsync(food);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Save food error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Update existing food item in database
    /// </summary>
    public async Task<bool> UpdateFoodAsync(FoodItem food)
    {
        try
        {
            await InitAsync();
            await _database!.UpdateAsync(food);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Update food error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Delete food item from database
    /// </summary>
    public async Task<bool> DeleteFoodAsync(int id)
    {
        try
        {
            await InitAsync();
            await _database!.DeleteAsync<FoodItem>(id);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Delete food error: {ex.Message}");
            return false;
        }
    }


    public async Task ResetFoodTableAsync()
    {
        await InitAsync();

        await _database!.ExecuteAsync(
            "DROP TABLE IF EXISTS FoodItems");

        await _database.CreateTableAsync<FoodItem>();
    }
}