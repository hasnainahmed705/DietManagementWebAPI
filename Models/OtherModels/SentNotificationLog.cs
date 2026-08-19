using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DietManagementWebAPI.Models.OtherModels
{
    public class SentNotificationLog
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        public string userName { get; set; } = string.Empty;
        public string notificationKey { get; set; } = string.Empty;
        public DateTime sentAt { get; set; } = DateTime.UtcNow;
        public string notificationType { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;
        public string title { get; set; } = string.Empty;
        public bool isRead { get; set; } = false;
    }
}
    