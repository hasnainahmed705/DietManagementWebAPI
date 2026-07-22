using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace DietManagementWebAPI.Models.EmailModels
{
    public class UserOtpsModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnoreIfDefault]
        public string Id { get; set; }

        public required string email { get; set; }

        public required string userName { get; set; } = "";
        public string otp { get; set; } = "";
        public string createdAt { get; set; } = "";
        public string expiresAt { get; set; } = "";
        public bool isVerified { get; set; } = false;
    }
}
