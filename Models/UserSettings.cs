using SQLite;

namespace NutriLens.Models;

/// <summary>
/// Stores user preferences and nutrition goals in SQLite database
/// </summary>
[Table("UserSettings")]
public class UserSettings
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int CalorieTarget { get; set; } = 2000;
    public int WaterTarget { get; set; } = 2000;
    public bool DarkMode { get; set; } = false;
    public bool TTSEnabled { get; set; } = false;
    public bool PeanutAlert { get; set; } = false;
    public bool GlutenAlert { get; set; } = false;
    public bool LactoseAlert { get; set; } = false;
}