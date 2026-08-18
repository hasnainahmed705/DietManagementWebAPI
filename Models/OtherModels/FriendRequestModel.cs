using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DietManagementWebAPI.Models.OtherModels
{
    public class FriendRequestModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnoreIfDefault]
        public string Id { get; set; }
        public string? senderUserName { get; set; }
        public string? receiverUserName { get; set; }
        public string? status { get; set; } = string.Empty;
        public DateTime createdAt { get; set; } = DateTime.UtcNow;
    }

    public class UserFriendsModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnoreIfDefault]
        public string Id { get; set; }
        public string? userName { get; set; }
        public List<string> friends { get; set; } = [];
    }
}
