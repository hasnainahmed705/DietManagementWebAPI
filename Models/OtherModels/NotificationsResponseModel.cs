using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DietManagementWebAPI.Models.OtherModels
{
    public class NotificationsResponseModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnoreIfDefault]
        public string? Id { get; set; }

        public required string userName { get; set; }

        public bool? mealReminders { get; set; }

        public bool? promotionOffers { get; set; }

        public bool? waterReminders { get; set; }
        public bool? workoutAlerts { get; set; }
    }

    public class NotificationsRequestModel
    {
        public required string userName { get; set; }

        public bool? mealReminders { get; set; }

        public bool? promotionOffers { get; set; }

        public bool? waterReminders { get; set; }
        public bool? workoutAlerts { get; set; }
    }
}
