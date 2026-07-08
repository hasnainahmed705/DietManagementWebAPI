using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class Meal
{
    public string FoodId { get; set; } = string.Empty;
    public string FoodName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;

    public int Portion { get; set; }
    public double Calories { get; set; }

    public string Benefits { get; set; } = string.Empty;

    public double Protein { get; set; }
    public double Carbs { get; set; }
    public double Fat { get; set; }
    public double Fiber { get; set; }
    public double Sugar { get; set; }           // ← Added
    public string BestFor { get; set; } = string.Empty;
    public string MealType { get; set; } = string.Empty;
    public string GlycemicIndex { get; set; } = string.Empty;  // ← Added
}