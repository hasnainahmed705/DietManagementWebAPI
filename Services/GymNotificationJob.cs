using DietManagementWebAPI.Models.DBModels;
using DietManagementWebAPI.Models.OtherModels;
using FirebaseAdmin.Messaging;
using MongoDB.Driver;
using Org.BouncyCastle.Asn1.Ocsp;

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
            // Get current time formatted exactly like your DB: "05:00PM", "08:30AM"
            var currentTimeString = DateTime.Now.ToString("hh:mmtt");

            // 1. Fetch eligible profiles using your typed UserProfile collection
            var eligibleProfiles = await _mongoService.UserProfile
                .Find(Builders<UserProfileData>.Filter.And(
                    Builders<UserProfileData>.Filter.Eq("isPerformGym", true),
                    Builders<UserProfileData>.Filter.Eq("GymTiming", currentTimeString)
                )).ToListAsync();

            foreach (var profile in eligibleProfiles)
            {
                var userName = profile.userName; // Assuming property is UserName, adjust if necessary

                // 2. Verify preferences using your typed collection
                // (Assuming you have a similar property like: public IMongoCollection<NotificationsPreferencesData> NotificationsPreferences => ...)
                var prefs = await _mongoService.NotificationsPreferences
                    .Find(Builders<NotificationsResponseModel>.Filter.Eq("userName", userName))
                    .FirstOrDefaultAsync();

                if (prefs != null && prefs.workoutAlerts == true) // Adjust property name to match your model
                {
                    // 3. Get FCM Token
                    // (Assuming you have a similar property like: public IMongoCollection<UserData> Users => ...)
                    var user = await _mongoService.Users
                        .Find(Builders<UsersDBModel>.Filter.Eq("userName", userName))
                        .FirstOrDefaultAsync();

                    if (user != null && !string.IsNullOrEmpty(user.fcmToken)) // Adjust property name to match your model
                    {
                        var fcmToken = user.fcmToken;

                        //// 4. Send FCM Push
                        //var message = new Message()
                        //{
                        //    Token = fcmToken,
                        //    Notification = new Notification()
                        //    {
                        //        Title = "Time to Workout! 🏋️",
                        //        Body = $"It's {currentTimeString}, time for your scheduled gym session. Let's go!"
                        //    }
                        //};

                        await _FirebaseNotificationService.SendToDeviceAsync(
                           fcmToken,
                            "Time to Workout! 🏋️",
                            $"It's {currentTimeString}, time for your scheduled gym session. Let's go!"
                            );
                    }
                }
            }
        }
    }
}
