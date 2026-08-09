using DietManagementWebAPI.Models.DBModels;
using DietManagementWebAPI.Models.EmailModels;
using DietManagementWebAPI.Models.OtherModels;
using MongoDB.Driver;

namespace DietManagementWebAPI.Services
{
    public class UserOTPJob
    {
        private readonly MongoDbService _mongoService;

        public UserOTPJob(MongoDbService mongoService)
        {
            _mongoService = mongoService;
        }

        public async Task ProcessUserOtpAsync()
        {
            var filter = Builders<UsersDBModel>.Filter.Empty;

            var users = await _mongoService.Users
                                .Find(filter)
                                .ToListAsync();
            foreach (var user in users)
            {
                var userOtps = await _mongoService.UserOtps
                                .Find(Builders<UserOtpsModel>.Filter.Eq("userName", user.userName))
                                .ToListAsync();

                foreach (var userOtp in userOtps)
                {
                    var userOtpFilter = Builders<UserOtpsModel>.Filter.Eq(x => x.Id, userOtp.Id);

                    if (userOtp.isVerified == true)
                    {
                        await _mongoService.UserOtps.DeleteOneAsync(userOtpFilter);
                    }
                    else
                    {
                        DateTime expiryTime = DateTime.Parse(userOtp.expiresAt);

                        if (expiryTime < DateTime.UtcNow)
                        {
                            await _mongoService.UserOtps.DeleteOneAsync(userOtpFilter);
                        }
                    }
                }
            }
        }
    }
}
