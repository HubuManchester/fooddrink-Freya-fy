using SQLite;

namespace NutriLens.Models;

/// <summary>
/// Represents a food item in the food database
/// </summary>
[Table("FoodItems")]
public class FoodItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public double Calories { get; set; }
    public double Protein { get; set; }
    public double Fat { get; set; }
    public double Sugar { get; set; }

    [Column("Ingredients")]
    public string Ingredients { get; set; } = "";

    /// <summary>
    /// Returns emoji icon based on category
    /// </summary>
    public string CategoryIcon => Category switch
    {
        "Meat" => "🥩",
        "Fish" => "🐟",
        "Vegetables" => "🥦",
        "Fruits" => "🍎",
        "Dairy" => "🥛",
        "Grains" => "🌾",
        "Snacks" => "🍫",
        "Drinks" => "🥤",
        _ => "🍳"
    };

    /// <summary>
    /// Returns background color based on category
    /// </summary>
    public string CategoryColor => Category switch
    {
        "Meat" => "#FFEBEE",
        "Fish" => "#E3F2FD",
        "Vegetables" => "#E8F5E9",
        "Fruits" => "#FFF9C4",
        "Dairy" => "#F3E5F5",
        "Grains" => "#FFF3E0",
        "Snacks" => "#FCE4EC",
        "Drinks" => "#E0F7FA",
        _ => "#F5F5F5"
    };
}