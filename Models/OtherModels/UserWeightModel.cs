using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class UserWeightModel
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfDefault]
    public string Id { get; set; } = string.Empty;

    public required string userName { get; set; }
    public string weightLogDate { get; set; } = string.Empty;
    public string weightInKg { get; set; } = string.Empty;
    public string weightInLb { get; set; } = string.Empty;
    public string dailyCaloriesTarget { get; set; } = string.Empty;
}