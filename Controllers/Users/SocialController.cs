using DietManagementWebAPI.Models.DBModels;
using DietManagementWebAPI.Models.OtherModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class SocialController : ControllerBase
{
    private readonly MongoDbService _mongoService;

    public SocialController(MongoDbService mongoService)
    {
        _mongoService = mongoService;
    }

    [HttpPost]
    [Route("SendFriendRequest")]
    public async Task<string> SendFriendRequest(string senderFriendCode, string receiverFriendCode)
    {
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

                var senderUserFriends = new UserFriendsModel
                {
                    userName = senderUser.userName,
                    friends = new List<string> { receiverUser.userName }
                };
                var receiverUserFriends = new UserFriendsModel
                {
                    userName = receiverUser.userName,
                    friends = new List<string> { senderUser.userName }
                };
                await _mongoService.UserFriends.InsertOneAsync(senderUserFriends);
                await _mongoService.UserFriends.InsertOneAsync(receiverUserFriends);
                await session.CommitTransactionAsync();
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
        UserFriendsModel? existingFriend = null;
        var receiverUser = await _mongoService.Users.Find(u => u.userName == receiverUserName).FirstOrDefaultAsync();

        var senderUser = await _mongoService.Users.Find(u => u.userName == senderUserName).FirstOrDefaultAsync();
        if (senderUser == null)
            return $"Unable to find a friend: {senderUserName}";

        try
        {
            existingFriend = await _mongoService.UserFriends.Find(r => r.userName == receiverUser.userName && r.friends.Contains(senderUser.userName)).FirstOrDefaultAsync();
            if (existingFriend == null)
                return "Friend not found!";
            existingFriend.friends.Remove(senderUser.userName);
            await _mongoService.UserFriends.ReplaceOneAsync(f => f.userName == receiverUser.userName, existingFriend);
            return $"Friend: {receiverUser.userName} deleted successfully!";
        }
        catch (Exception ex)
        {
            return "Failed to delete a friend.";
        }
    }
}