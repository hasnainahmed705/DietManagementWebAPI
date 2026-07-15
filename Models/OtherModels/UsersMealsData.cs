using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class UsersMealsData
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfDefault]
    public string Id { get; set; } = string.Empty;
    public required string userName { get; set; }
    public required string email { get; set; }
    public required string FoodName { get; set; }
    public int FoodCalories { get; set; } = 0;

    public string Unit { get; set; } = string.Empty;
    public double Portion { get; set; } = 0.0;

    public double Protein { get; set; } = 0.0;

    public double Carbs { get; set; } = 0.0;
    public double Fat { get; set; } = 0.0;
    public double Sugar { get; set; } = 0.0;
    public double Fiber { get; set; } = 0.0;
    public string Benefit { get; set; } = string.Empty;
    public string BestFor { get; set; } = string.Empty;
    public string MealType { get; set; } = string.Empty;
    public string GlycemicIndex { get; set; } = string.Empty;
}