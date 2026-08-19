using DietManagementWebAPI.Models.DBModels;
using DietManagementWebAPI.Models.OtherModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using static System.Collections.Specialized.BitVector32;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class SocialController : ControllerBase
{
    private readonly MongoDbService _mongoService;
    private readonly FirebaseNotificationService _FirebaseNotificationService;

    public SocialController(MongoDbService mongoService, FirebaseNotificationService firebaseNotificationService)
    {
        _mongoService = mongoService;
        _FirebaseNotificationService = firebaseNotificationService;
    }

    [HttpPost]
    [Route("SendFriendRequest")]
    public async Task<string> SendFriendRequest(string senderFriendCode, string receiverFriendCode)
    {
        if(senderFriendCode == receiverFriendCode)
            return "Friend request to the same account is not allowed.";
        var senderUser = await _mongoService.Users.Find(u => u.yourFriendCode == senderFriendCode).FirstOrDefaultAsync();
        if (senderUser == null)
            return "Sender not found!";

        var receiverUser = await _mongoService.Users.Find(u => u.yourFriendCode == receiverFriendCode).FirstOrDefaultAsync();
        if (receiverUser == null)
            return "The friend code you entered is invalid!";

        var existingFriend = await _mongoService.UserFriends.Find(r => r.userName == senderUser.userName && r.friends.Contains(receiverUser.userName)).FirstOrDefaultAsync();
        if (existingFriend != null)
            return "You are already friend!";

        var existingRequest = await _mongoService.FriendRequests.Find(r => r.senderUserName == senderUser.userName && r.receiverUserName == receiverUser.userName && r.status == "Pending").FirstOrDefaultAsync();
        if (existingRequest != null)
            return "Friend request already sent!";

        try
        {
            var friendRequest = new FriendRequestModel
            {
                senderUserName = senderUser.userName,
                receiverUserName = receiverUser.userName,
                status = "Pending"
            };

            await _mongoService.FriendRequests.InsertOneAsync(friendRequest);

            await _FirebaseNotificationService.SendToDeviceAsync(
                                            receiverUser.fcmToken,
                                            "Someone Wants to Connect! 🎉",
                                            $"{senderUser.userName} sent you a friend request."
                                        );
            var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(receiverUser.timeZone);
            var currentTimeString = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, userTimeZone);

            var currentTimeString2 = currentTimeString.ToString("hh:mm tt");

            var workoutNotificationKey = $"{receiverUser.userName}_social_{currentTimeString2}_{currentTimeString:yyyyMMdd}";
            await _mongoService.SentNotificationLogs.InsertOneAsync(new SentNotificationLog
            {
                userName = receiverUser.userName,
                notificationKey = workoutNotificationKey,
                sentAt = DateTime.UtcNow,
                notificationType = "Social",
                message = $"{senderUser.userName} sent you a friend request.",
                title = "Someone Wants to Connect! 🎉"
            });

            return "Friend request sent successfully!";
        }
        catch (Exception ex)
        {
            return "Failed to send friend request.";
        }
    }

    [HttpPatch]
    [Route("UpdateFriendRequest")]
    public async Task<string> UpdateFriendRequest(string senderUserName, string receiverUserName,string actionName)
    {
        FriendRequestModel? existingRequest = null;
        var receiverUser = await _mongoService.Users.Find(u => u.userName == receiverUserName).FirstOrDefaultAsync();

        var senderUser = await _mongoService.Users.Find(u => u.userName == senderUserName).FirstOrDefaultAsync();
        if (senderUser == null)
            return $"Unable to find the username: {senderUserName}";

        using var session = await _mongoService.Client.StartSessionAsync();
        try
        {
            session.StartTransaction();
            if (actionName == "Accept")
            {
                existingRequest = await _mongoService.FriendRequests.Find(r => r.senderUserName == senderUser.userName && r.receiverUserName == receiverUser.userName && r.status == "Pending").FirstOrDefaultAsync();
                if (existingRequest == null)
                    return "Friend request not found!";

                var frRequestFilter = Builders<FriendRequestModel>.Filter.Eq(p => p.senderUserName, senderUser.userName) & Builders<FriendRequestModel>.Filter.Eq(p => p.receiverUserName, receiverUser.userName) & Builders<FriendRequestModel>.Filter.Eq(p => p.status, "Pending");
                var frRequestUpdate = Builders<FriendRequestModel>.Update.Set(p => p.status, "Accepted");
                var frRequestResult = await _mongoService.FriendRequests.UpdateOneAsync(session, frRequestFilter, frRequestUpdate);

                var existingUserFriends = await _mongoService.UserFriends.Find(x => x.userName == senderUser.userName).FirstOrDefaultAsync();
                var existingUserFriends2 = await _mongoService.UserFriends.Find(x => x.userName == receiverUser.userName).FirstOrDefaultAsync();

                if (existingUserFriends != null)
                {
                    var update = Builders<UserFriendsModel>.Update
                        .AddToSet(x => x.friends, receiverUser.userName);

                    await _mongoService.UserFriends.UpdateOneAsync(
                        x => x.userName == senderUser.userName,
                        update
                    );
                }
                else
                {
                    var senderUserFriends = new UserFriendsModel
                    {
                        userName = senderUser.userName,
                        friends = new List<string>
                        {
                            receiverUser.userName
                        }
                    };

                    await _mongoService.UserFriends.InsertOneAsync(senderUserFriends);
                }
                if (existingUserFriends2 != null)
                {
                    var update = Builders<UserFriendsModel>.Update
                        .AddToSet(x => x.friends, senderUser.userName);

                    await _mongoService.UserFriends.UpdateOneAsync(
                        x => x.userName == receiverUser.userName,
                        update
                    );
                }
                else
                {
                    var receiverUserFriends = new UserFriendsModel
                    {
                        userName = receiverUser.userName,
                        friends = new List<string>
                        {
                            senderUser.userName
                        }
                    };

                    await _mongoService.UserFriends.InsertOneAsync(receiverUserFriends);
                }

                await session.CommitTransactionAsync();
                await _FirebaseNotificationService.SendToDeviceAsync(
                                            senderUser.fcmToken,
                                            "✅ You're Now Friends!",
                                            $"{receiverUser.userName} accepted your friend request."
                                        );
                var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(senderUser.timeZone);
                var currentTimeString = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, userTimeZone);

                var currentTimeString2 = currentTimeString.ToString("hh:mm tt");

                var workoutNotificationKey = $"{senderUser.userName}_social_{currentTimeString2}_{currentTimeString:yyyyMMdd}";
                await _mongoService.SentNotificationLogs.InsertOneAsync(new SentNotificationLog
                {
                    userName = senderUser.userName,
                    notificationKey = workoutNotificationKey,
                    sentAt = DateTime.UtcNow,
                    notificationType = "Social",
                    message = $"{receiverUser.userName} accepted your friend request.",
                    title = "✅ You're Now Friends!"
                });
                return "Friend request accepted successfully!";
            }
            else if (actionName == "Reject")
            {
                existingRequest = await _mongoService.FriendRequests.Find(r => r.senderUserName == senderUser.userName && r.receiverUserName == receiverUser.userName && r.status == "Pending").FirstOrDefaultAsync();
                if (existingRequest == null)
                    return "Friend request not found!";
                var frRequestFilter = Builders<FriendRequestModel>.Filter.Eq(p => p.senderUserName, senderUser.userName) & Builders<FriendRequestModel>.Filter.Eq(p => p.receiverUserName, receiverUser.userName) & Builders<FriendRequestModel>.Filter.Eq(p => p.status, "Pending");
                var frRequestUpdate = Builders<FriendRequestModel>.Update.Set(p => p.status, "Rejected");
                var frRequestResult = await _mongoService.FriendRequests.UpdateOneAsync(session, frRequestFilter, frRequestUpdate);

                await session.CommitTransactionAsync();
                await _FirebaseNotificationService.SendToDeviceAsync(
                                            senderUser.fcmToken,
                                            "❌ Friend Request Rejected",
                                            $"{receiverUser.userName} rejected your friend request."
                                        );
                var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(senderUser.timeZone);
                var currentTimeString = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, userTimeZone);

                var currentTimeString2 = currentTimeString.ToString("hh:mm tt");

                var workoutNotificationKey = $"{senderUser.userName}_social_{currentTimeString2}_{currentTimeString:yyyyMMdd}";
                await _mongoService.SentNotificationLogs.InsertOneAsync(new SentNotificationLog
                {
                    userName = senderUser.userName,
                    notificationKey = workoutNotificationKey,
                    sentAt = DateTime.UtcNow,
                    notificationType = "Social",
                    message = $"{receiverUser.userName} rejected your friend request.",
                    title = "❌ Friend Request Rejected"
                });
                return "Friend request rejected successfully!";
            }

            await session.AbortTransactionAsync();
            return "Invalid action name!";
        }
        catch (Exception ex)
        {
            await session.AbortTransactionAsync();
            return "Failed to update friend request.";
        }
    }

    [HttpDelete]
    [Route("DeleteFriend")]
    public async Task<string> DeleteFriend(string senderUserName, string receiverUserName)
    {
        var receiverUser = await _mongoService.Users.Find(u => u.userName == receiverUserName).FirstOrDefaultAsync();

        var senderUser = await _mongoService.Users.Find(u => u.userName == senderUserName).FirstOrDefaultAsync();
        if (senderUser == null)
            return $"Unable to find a friend: {senderUserName}";

        using var session = await _mongoService.Client.StartSessionAsync();

        try
        {
            session.StartTransaction();
            var existingreceiverFriendFilter = Builders<UserFriendsModel>.Filter.And(Builders<UserFriendsModel>.Filter.Eq(r => r.userName,receiverUser.userName),Builders<UserFriendsModel>.Filter.AnyEq(r => r.friends,senderUser.userName));

            var existingreceiverFriendUpdate = Builders<UserFriendsModel>.Update
                .Pull(r => r.friends, senderUser.userName);

            var existingFriendResult =
                await _mongoService.UserFriends.UpdateOneAsync(
                    existingreceiverFriendFilter,
                    existingreceiverFriendUpdate
                );

            var existingsenderFriendFilter = Builders<UserFriendsModel>.Filter.And(Builders<UserFriendsModel>.Filter.Eq(r => r.userName, senderUser.userName), Builders<UserFriendsModel>.Filter.AnyEq(r => r.friends, receiverUser.userName));

            var existingsenderFriendUpdate = Builders<UserFriendsModel>.Update
                .Pull(r => r.friends, receiverUser.userName);

            var existingsenderFriendResult =
                await _mongoService.UserFriends.UpdateOneAsync(
                    existingsenderFriendFilter,
                    existingsenderFriendUpdate
                ); 
            await session.CommitTransactionAsync();
            await _FirebaseNotificationService.SendToDeviceAsync(
                                            senderUser.fcmToken,
                                            "👋 You've Been Removed",
                                            $"{receiverUser.userName} removed you from their friends list."
                                        );
            var userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(senderUser.timeZone);
            var currentTimeString = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, userTimeZone);

            var currentTimeString2 = currentTimeString.ToString("hh:mm tt");

            var workoutNotificationKey = $"{senderUser.userName}_social_{currentTimeString2}_{currentTimeString:yyyyMMdd}";
            await _mongoService.SentNotificationLogs.InsertOneAsync(new SentNotificationLog
            {
                userName = senderUser.userName,
                notificationKey = workoutNotificationKey,
                sentAt = DateTime.UtcNow,
                notificationType = "Social",
                message = $"{receiverUser.userName} removed you from their friends list.",
                title = "👋 You've Been Removed"
            });
            return $"Friend: {senderUser.userName} deleted successfully!";
        }
        catch (Exception ex)
        {
            await session.AbortTransactionAsync();
            return "Failed to delete a friend.";
        }
    }
}