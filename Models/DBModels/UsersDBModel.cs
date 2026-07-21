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

        [Required]
        public string firstName { get; set; }
        [Required]
        public string lastName { get; set; }
        [Required]
        public string email { get; set; }
        [Required]
        public string password { get; set; }

        public string userName { get; set; } = "";
    }

    public class UserLoginResponse
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string email { get; set; }
        public string userName { get; set; }
        public string token { get; set; }
    }

    public class ChangePasswordRequest
    {
        public required string userName { get; set; }
        public required string currentPassword { get; set; }
        public required string newPassword { get; set; }
    }


    public class RegisterRequest
    {
        // User Table Fields
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string email { get; set; }
        public string password { get; set; }

        public string? Gender { get; set; }

        public int? Age { get; set; }

        public string? HeightCm { get; set; }

        public double? WeightKg { get; set; }

        public string? Goal { get; set; }

        public string? DailyCalorieTarget { get; set; }

        public string? ProteinTargetG { get; set; }

        public string? CarbTargetG { get; set; }

        public string? FatTargetG { get; set; }
    }

    public class UserProfileData
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnoreIfDefault]
        public string Id { get; set; }
        public string userName { get; set; }

        public string? Gender { get; set; }

        public int? Age { get; set; }

        public string? HeightCm { get; set; }

        public double? WeightKg { get; set; }

        public string? Goal { get; set; }

        public string? DailyCalorieTarget { get; set; }

        public string? ProteinTargetG { get; set; }

        public string? CarbTargetG { get; set; }

        public string? FatTargetG { get; set; }
    }

    public class UserProfileUpdateDto
    {
        public string? Gender { get; set; }
        public int? Age { get; set; }
        public string? HeightCm { get; set; }
        public double? WeightKg { get; set; }
        public string? Goal { get; set; }
        public string? DailyCalorieTarget { get; set; }
        public string? ProteinTargetG { get; set; }
        public string? CarbTargetG { get; set; }
        public string? FatTargetG { get; set; }
    }
}
