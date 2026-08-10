using DietManagementWebAPI.Models.DBModels;
using DietManagementWebAPI.Models.OtherModels;
using FirebaseAdmin.Messaging;
using MongoDB.Driver;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Globalization;

namespace DietManagementWebAPI.Services
{
    public class GymNotificationJob
    {
        private readonly MongoDbService _mongoService;
        private readonly FirebaseNotificationService _FirebaseNotificationService;

        public GymNotificationJob(MongoDbService mongoService, FirebaseNotificationService firebaseNotificationService)
        {
            _mongoService = mongoService;
            _FirebaseNotificationService = firebaseNotificationService;
        }

        public async Task ProcessGymNotificationsAsync()
        {
            var filter = Builders<UsersDBModel>.Filter.Empty;

            var users = await _mongoService.Users
                                .Find(filter)
                                .ToListAsync();
           
            foreach (var user in users)
            {
                var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(user.timeZone);
                var currentTimeString = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, userTimeZone);

                var currentTimeString2 = currentTimeString.ToString("hh:mm tt");

                var prefs = await _mongoService.NotificationsPreferences
                       .Find(Builders<NotificationsResponseModel>.Filter.Eq("userName", user.userName))
                       .FirstOrDefaultAsync();

                var eligibleProfile = await _mongoService.UserProfile
                    .Find(Builders<UserProfileData>.Filter.And(
                        Builders<UserProfileData>.Filter.Eq("isPerformGym", true),
                        Builders<UserProfileData>.Filter.Eq("userName", user.userName)
                    )).FirstOrDefaultAsync();

                if (prefs != null && prefs.workoutAlerts == true)
                {
                    if(eligibleProfile!=null)
                    {
                        if (eligibleProfile.isPerformGym == true && eligibleProfile.GymTiming==currentTimeString2)
                        {
                            if (user != null && !string.IsNullOrEmpty(user.fcmToken))
                            {
                                var fcmToken = user.fcmToken;

                                try
                                {
                                    await _FirebaseNotificationService.SendToDeviceAsync(
                                        fcmToken,
                                        "Time to Workout! 🏋️",
                                        $"It's {currentTimeString2}, time for your scheduled gym session."
                                    );
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.ToString());
                                }
                            }
                        }
                    }
                }

                if (prefs != null && prefs.mealReminders == true)
                {
                    if (user != null && !string.IsNullOrEmpty(user.fcmToken))
                    {
                        var fcmToken = user.fcmToken;

                        try
                        {
                            switch (currentTimeString2)
                            {
                                case "08:00 AM":
                                    this.mealTypeSession(fcmToken, currentTimeString2, "Breakfast");
                                    break;
                                case "01:00 PM":
                                    this.mealTypeSession(fcmToken, currentTimeString2, "Lunch");
                                    break;
                                case "08:00 PM":
                                    this.mealTypeSession(fcmToken, currentTimeString2, "Dinner");
                                    break;
                                default:
                                    break;
                            }

                            if (eligibleProfile!=null)
                            {
                                if (eligibleProfile.isPerformGym == true)
                                {

                                    var userGymTiming = DateTime.ParseExact(
                                                        eligibleProfile.GymTiming,
                                                        "hh:mm tt",
                                                        CultureInfo.InvariantCulture);

                                    var preWorkoutTime = userGymTiming.AddMinutes(-45);
                                    var postWorkoutTime = userGymTiming.AddMinutes(60);

                                    if (currentTimeString.Hour == preWorkoutTime.Hour &&
                                        currentTimeString.Minute == preWorkoutTime.Minute)
                                    {
                                        this.mealTypeSession(fcmToken, currentTimeString2, "Pre Workout");
                                    }

                                    if (currentTimeString.Hour == postWorkoutTime.Hour &&
                                        currentTimeString.Minute == postWorkoutTime.Minute)
                                    {
                                        this.mealTypeSession(fcmToken, currentTimeString2, "Post Workout");
                                    }

                                }
                            }

                            
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                }

                if (prefs != null && prefs.waterReminders == true)
                {
                    if (user != null)
                    {
                        var fcmToken = user.fcmToken;

                        var waterReminderTimes = new[]
                        {
                        "08:00 AM",
                        "10:00 AM",
                        "12:00 PM",
                        "02:00 PM",
                        "04:00 PM",
                        "06:00 PM",
                        "08:00 PM",
                        "10:00 PM"
                    };

                        if (fcmToken != "")
                        {
                            if (waterReminderTimes.Contains(currentTimeString2))
                            {
                                await _FirebaseNotificationService.SendToDeviceAsync(
                                    fcmToken,
                                    "Stay Hydrated! 💧",
                                    "It's time to drink some water. Stay hydrated!"
                                );
                            }
                        }
                    }
                   
                }
            }
        }

        private async void mealTypeSession(string fcmToken1, string currentTimeString3, string mealType)
        {
            string title;
            string description;

            switch (mealType)
            {
                case "Breakfast":
                    title = "Time for Breakfast! 🍳";
                    description = $"It's {currentTimeString3}, time for your scheduled breakfast.";
                    break;

                case "Lunch":
                    title = "Time for Lunch! 🍽️";
                    description = $"It's {currentTimeString3}, time for your scheduled lunch.";
                    break;

                case "Dinner":
                    title = "Time for Dinner! 🍽️";
                    description = $"It's {currentTimeString3}, time for your scheduled dinner.";
                    break;

                case "Pre Workout":
                    title = "Pre-Workout Meal Time! 💪";
                    description = $"It's {currentTimeString3}, time for your pre-workout meal.";
                    break;

                case "Post Workout":
                    title = "Post-Workout Meal Time! 💪";
                    description = $"It's {currentTimeString3}, time for your post-workout meal.";
                    break;

                default:
                    title = "Meal Reminder 🍽️";
                    description = $"It's {currentTimeString3}, time for your scheduled meal.";
                    break;
            }

            await _FirebaseNotificationService.SendToDeviceAsync(
                fcmToken1,
                title,
                description
            );
        }
    }
}
