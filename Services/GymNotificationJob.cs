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

                                var workoutNotificationKey = $"{user.userName}_workout_{currentTimeString2}_{currentTimeString:yyyyMMdd}";
                                var alreadySent = await _mongoService.SentNotificationLogs
                                                .Find(Builders<SentNotificationLog>.Filter.And(
                                                    Builders<SentNotificationLog>.Filter.Eq("notificationKey", workoutNotificationKey),
                                                    Builders<SentNotificationLog>.Filter.Eq("userName", user.userName),
                                                    Builders<SentNotificationLog>.Filter.Eq("notificationType", "Workout")
                                                )).AnyAsync();

                                if(!alreadySent)
                                {
                                    try
                                    {
                                        await _FirebaseNotificationService.SendToDeviceAsync(
                                            fcmToken,
                                            "Time to Workout! 🏋️",
                                            $"It's {currentTimeString2}, time for your scheduled gym session."
                                        );

                                        await _mongoService.SentNotificationLogs.InsertOneAsync(new SentNotificationLog
                                        {
                                            userName = user.userName,
                                            notificationKey = workoutNotificationKey,
                                            sentAt = DateTime.UtcNow,
                                            notificationType = "Workout",
                                            message = $"It's {currentTimeString2}, time for your scheduled gym session.",
                                            title = "Time to Workout! 🏋️"
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.ToString());
                                    }
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
                                    this.mealTypeSession(fcmToken, currentTimeString2, user.userName, currentTimeString, "Breakfast");
                                    break;
                                case "01:00 PM":
                                    this.mealTypeSession(fcmToken, currentTimeString2, user.userName, currentTimeString, "Lunch");
                                    break;
                                case "08:00 PM":
                                    this.mealTypeSession(fcmToken, currentTimeString2, user.userName, currentTimeString, "Dinner");
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
                                        this.mealTypeSession(fcmToken, currentTimeString2, user.userName, currentTimeString, "Pre Workout");
                                    }

                                    if (currentTimeString.Hour == postWorkoutTime.Hour &&
                                        currentTimeString.Minute == postWorkoutTime.Minute)
                                    {
                                        this.mealTypeSession(fcmToken, currentTimeString2, user.userName, currentTimeString, "Post Workout");
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
                                var waterNotificationKey = $"{user.userName}_water_{currentTimeString2}_{currentTimeString:yyyyMMdd}";
                                var alreadySent = await _mongoService.SentNotificationLogs
                                                .Find(Builders<SentNotificationLog>.Filter.And(
                                                    Builders<SentNotificationLog>.Filter.Eq("notificationKey", waterNotificationKey),
                                                    Builders<SentNotificationLog>.Filter.Eq("userName", user.userName),
                                                    Builders<SentNotificationLog>.Filter.Eq("notificationType", "Water")
                                                )).AnyAsync();

                                if (!alreadySent)
                                {
                                    await _FirebaseNotificationService.SendToDeviceAsync(
                                    fcmToken,
                                    "Stay Hydrated! 💧",
                                    "It's time to drink some water. Stay hydrated!");

                                    await _mongoService.SentNotificationLogs.InsertOneAsync(new SentNotificationLog
                                    {
                                        userName = user.userName,
                                        notificationKey = waterNotificationKey,
                                        sentAt = DateTime.UtcNow,
                                        notificationType = "Water",
                                        message = "It's time to drink some water. Stay hydrated!",
                                        title = "Stay Hydrated! 💧"
                                    });
                                }
                            }
                        }
                    }
                   
                }
            }
        }

        private async void mealTypeSession(string fcmToken1, string currentTimeString3, string userName, DateTime currentTimeString, string mealType)
        {
            string title;
            string description;

            switch (mealType)
            {
                case "Breakfast":
                    var breakfastNotificationKey = $"{userName}_breakfast_{currentTimeString3}_{currentTimeString:yyyyMMdd}";
                    var alreadySent = await _mongoService.SentNotificationLogs
                                    .Find(Builders<SentNotificationLog>.Filter.And(
                                        Builders<SentNotificationLog>.Filter.Eq("notificationKey", breakfastNotificationKey),
                                        Builders<SentNotificationLog>.Filter.Eq("userName", userName),
                                        Builders<SentNotificationLog>.Filter.Eq("notificationType", "Breakfast")
                                    )).AnyAsync();

                    if(!alreadySent)
                    {
                        title = "Time for Breakfast! 🍳";
                        description = $"It's {currentTimeString3}, time for your scheduled breakfast.";

                        await _FirebaseNotificationService.SendToDeviceAsync(
                                    fcmToken1,
                                    title,
                                    description);

                        await _mongoService.SentNotificationLogs.InsertOneAsync(new SentNotificationLog
                        {
                            userName = userName,
                            notificationKey = breakfastNotificationKey,
                            sentAt = DateTime.UtcNow,
                            notificationType = "Breakfast",
                            message = description,
                            title = title
                        });
                    }
                    break;

                case "Lunch":
                    var lunchNotificationKey = $"{userName}_lunch_{currentTimeString3}_{currentTimeString:yyyyMMdd}";
                    var lunchAlreadySent = await _mongoService.SentNotificationLogs
                                    .Find(Builders<SentNotificationLog>.Filter.And(
                                        Builders<SentNotificationLog>.Filter.Eq("notificationKey", lunchNotificationKey),
                                        Builders<SentNotificationLog>.Filter.Eq("userName", userName),
                                        Builders<SentNotificationLog>.Filter.Eq("notificationType", "Lunch")
                                    )).AnyAsync();

                    if(!lunchAlreadySent)
                    {
                        title = "Time for Lunch! 🍽️";
                        description = $"It's {currentTimeString3}, time for your scheduled lunch.";

                        await _FirebaseNotificationService.SendToDeviceAsync(
                                    fcmToken1,
                                    title,
                                    description);

                        await _mongoService.SentNotificationLogs.InsertOneAsync(new SentNotificationLog
                        {
                            userName = userName,
                            notificationKey = lunchNotificationKey,
                            sentAt = DateTime.UtcNow,
                            notificationType = "Lunch",
                            message = description,
                            title = title
                        });
                    }
                    break;

                case "Dinner":
                    var dinnerNotificationKey = $"{userName}_dinner_{currentTimeString3}_{currentTimeString:yyyyMMdd}";
                    var dinnerAlreadySent = await _mongoService.SentNotificationLogs
                                    .Find(Builders<SentNotificationLog>.Filter.And(
                                        Builders<SentNotificationLog>.Filter.Eq("notificationKey", dinnerNotificationKey),
                                        Builders<SentNotificationLog>.Filter.Eq("userName", userName),
                                        Builders<SentNotificationLog>.Filter.Eq("notificationType", "Dinner")
                                    )).AnyAsync();

                    if(!dinnerAlreadySent)
                    {
                        title = "Time for Dinner! 🍽️";
                        description = $"It's {currentTimeString3}, time for your scheduled dinner.";
                        await _FirebaseNotificationService.SendToDeviceAsync(
                                    fcmToken1,
                                    title,
                                    description);
                        await _mongoService.SentNotificationLogs.InsertOneAsync(new SentNotificationLog
                        {
                            userName = userName,
                            notificationKey = dinnerNotificationKey,
                            sentAt = DateTime.UtcNow,
                            notificationType = "Dinner",
                            message = description,
                            title = title
                        });
                    }
                    break;

                case "Pre Workout":
                    var preWorkoutNotificationKey = $"{userName}_pre_workout_{currentTimeString3}_{currentTimeString:yyyyMMdd}";
                    var preWorkoutAlreadySent = await _mongoService.SentNotificationLogs
                                    .Find(Builders<SentNotificationLog>.Filter.And(
                                        Builders<SentNotificationLog>.Filter.Eq("notificationKey", preWorkoutNotificationKey),
                                        Builders<SentNotificationLog>.Filter.Eq("userName", userName),
                                        Builders<SentNotificationLog>.Filter.Eq("notificationType", "Pre Workout")
                                    )).AnyAsync();

                    if(!preWorkoutAlreadySent)
                    {
                        title = "Pre-Workout Meal Time! 💪";
                        description = $"It's {currentTimeString3}, time for your pre-workout meal.";
                        await _FirebaseNotificationService.SendToDeviceAsync(
                                    fcmToken1,
                                    title,
                                    description);
                        await _mongoService.SentNotificationLogs.InsertOneAsync(new SentNotificationLog
                        {
                            userName = userName,
                            notificationKey = preWorkoutNotificationKey,
                            sentAt = DateTime.UtcNow,
                            notificationType = "Pre Workout",
                            message = description,
                            title = title
                        });
                    }
                    break;

                case "Post Workout":
                    var postWorkoutNotificationKey = $"{userName}_post_workout_{currentTimeString3}_{currentTimeString:yyyyMMdd}";
                    var postWorkoutAlreadySent = await _mongoService.SentNotificationLogs
                                    .Find(Builders<SentNotificationLog>.Filter.And(
                                        Builders<SentNotificationLog>.Filter.Eq("notificationKey", postWorkoutNotificationKey),
                                        Builders<SentNotificationLog>.Filter.Eq("userName", userName),
                                        Builders<SentNotificationLog>.Filter.Eq("notificationType", "Post Workout")
                                    )).AnyAsync();

                    if(!postWorkoutAlreadySent)
                    {
                        title = "Post-Workout Meal Time! 💪";
                        description = $"It's {currentTimeString3}, time for your post-workout meal.";
                        await _FirebaseNotificationService.SendToDeviceAsync(
                                    fcmToken1,
                                    title,
                                    description);
                        await _mongoService.SentNotificationLogs.InsertOneAsync(new SentNotificationLog
                        {
                            userName = userName,
                            notificationKey = postWorkoutNotificationKey,
                            sentAt = DateTime.UtcNow,
                            notificationType = "Post Workout",
                            message = description,
                            title = title
                        });
                    }
                    break;

                default:
                    break;
            }
            
        }
    }
}
