using SQLite;

namespace NutriLens.Models;

/// <summary>
/// Represents a single food diary entry stored in SQLite database
/// </summary>
[Table("DiaryEntries")]
public class DiaryEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string FoodName { get; set; } = "";
    public string MealType { get; set; } = ""; // Breakfast/Lunch/Dinner
    public double Calories { get; set; }
    public double Protein { get; set; }
    public double Fat { get; set; }
    public double Sugar { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
}