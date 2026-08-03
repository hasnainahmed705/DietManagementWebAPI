using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DietManagementWebAPI.Models.OtherModels
{
    public class WorkoutSessionModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnoreIfDefault]
        public string? Id { get; set; }

        public required string userName { get; set; }

        public required string sessionName { get; set; }

        public required string muscleGroupName { get; set; }

        public required string startTime { get; set; }

        public required string endTime { get; set; }

        public int durationMinutes { get; set; }

        public required string status { get; set; }
    }

    public class WorkoutSessionRequest
    {
        public required string userName { get; set; }

        public required string sessionName { get; set; }

        public required string muscleGroupName { get; set; }

        public required string startTime { get; set; }

        public required string endTime { get; set; }

        public int durationMinutes { get; set; }

        public required string status { get; set; }
    }

    public class WorkoutExerciseModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnoreIfDefault]
        public string? Id { get; set; }

        public required string sessionName { get; set; }

        public required string userName { get; set; }

        public required string date { get; set; }

        public required string exerciseName { get; set; }

        public required string muscleGroupName { get; set; }

        public required List<WorkoutSetModel> sets { get; set; }
    }

    public class WorkoutSetModel
    {
        public int setNumber { get; set; }

        public int reps { get; set; }

        public bool isCompleted { get; set; }
    }

    public class WorkoutExerciseRequest
    {
        public required string sessionName { get; set; }

        public required string userName { get; set; }

        public required string date { get; set; }

        public required string exerciseName { get; set; }

        public required string muscleGroupName { get; set; }

        public required List<WorkoutSetRequest> sets { get; set; }
    }

    public class WorkoutSetRequest
    {
        public int setNumber { get; set; }

        public int reps { get; set; }

        public bool isCompleted { get; set; }
    }

    public class WorkoutBurnCaloriesModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnoreIfDefault]
        public string? Id { get; set; }
        public required string userName { get; set; }

        public required string muscleGroupName { get; set; }

        public double caloriesBurned { get; set; }
        public required string date { get; set; }
    }

    public class WorkoutBurnCaloriesRequest
    {
        public required string userName { get; set; }

        public required string muscleGroupName { get; set; }

        public double caloriesBurned { get; set; }
        public required string date { get; set; }
    }
}
