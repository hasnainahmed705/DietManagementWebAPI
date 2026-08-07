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
            var filter = Builders<UsersDBModel>.Filter.Empty;

            var users = await _mongoService.Users
                                .Find(filter)
                                .ToListAsync();

            foreach (var user in users)
            {
                var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(user.timeZone);
                var utcTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, userTimeZone);

                var currentTimeString = utcTime.ToString("hh:mm tt");

                var eligibleProfiles = await _mongoService.UserProfile
                    .Find(Builders<UserProfileData>.Filter.And(
                        Builders<UserProfileData>.Filter.Eq("isPerformGym", true),
                        Builders<UserProfileData>.Filter.Eq("userName", user.userName),
                        Builders<UserProfileData>.Filter.Eq("GymTiming", currentTimeString)
                    )).ToListAsync();

                foreach (var profile in eligibleProfiles)
                {
                    var userName = profile.userName;

                    var prefs = await _mongoService.NotificationsPreferences
                        .Find(Builders<NotificationsResponseModel>.Filter.Eq("userName", userName))
                        .FirstOrDefaultAsync();

                    if (prefs != null && prefs.workoutAlerts == true)
                    {
                        if (user != null && !string.IsNullOrEmpty(user.fcmToken))
                        {
                            var fcmToken = user.fcmToken;

                            try
                            {
                                await _FirebaseNotificationService.SendToDeviceAsync(
                                    fcmToken,
                                    "Time to Workout! 🏋️",
                                    $"It's {currentTimeString}, time for your scheduled gym session."
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
        }
    }
}
