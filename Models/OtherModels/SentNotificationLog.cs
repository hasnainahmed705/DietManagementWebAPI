using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class SentNotificationLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public string userName { get; set; } = string.Empty;
    public string notificationKey { get; set; } = string.Empty;
    public DateTime sentAt { get; set; } = DateTime.UtcNow;
}