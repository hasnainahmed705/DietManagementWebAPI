using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DietManagementWebAPI.Models.DBModels
{

    public class UsersDBModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnoreIfDefault]
        public string Id { get; set; }

        [BsonElement("firstName")]
        public string FirstName { get; set; }

        [BsonElement("lastName")]
        public string LastName { get; set; }

        [BsonElement("email")]
        public string Email { get; set; }

        [BsonElement("profile")]
        public UserProfileData? Profile { get; set; }

    }

    public class UserProfileData
    {
        [BsonElement("gender")]
        public string? Gender { get; set; }

        [BsonElement("age")]
        public int? Age { get; set; }

        [BsonElement("heightCm")]
        public double? HeightCm { get; set; }

        [BsonElement("heightInch")]
        public double? HeightInch { get; set; }

        [BsonElement("weightKg")]
        public double? WeightKg { get; set; }

        [BsonElement("activityLevel")]
        public string? ActivityLevel { get; set; }

        [BsonElement("goal")]
        public string? Goal { get; set; }

        [BsonElement("dailyCalorieTarget")]
        public int? DailyCalorieTarget { get; set; }

        [BsonElement("proteinTargetG")]
        public int? ProteinTargetG { get; set; }

        [BsonElement("carbTargetG")]
        public int? CarbTargetG { get; set; }

        [BsonElement("fatTargetG")]
        public int? FatTargetG { get; set; }
    }
}
