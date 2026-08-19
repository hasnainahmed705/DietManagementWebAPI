using DietManagementWebAPI.Models.DBModels;
using DietManagementWebAPI.Models.EmailModels;
using DietManagementWebAPI.Models.OtherModels;
using FirebaseAdmin.Auth.Multitenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;
using static System.Net.WebRequestMethods;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly MongoDbService _mongoService;
    private readonly IConfiguration _configuration;

    public NotificationsController(MongoDbService mongoService, IConfiguration configuration)
    {
        _mongoService = mongoService;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpPatch]
    [Route("UpdateNotificationsPreferences")]
    public async Task<ActionResult> UpdateNotificationsPreferences(
    [FromBody] NotificationsRequestModel request)
    {
        using var session = await _mongoService.Client.StartSessionAsync();

        try
        {
            session.StartTransaction();

            // Check whether at least one field is provided
            if (!request.mealReminders.HasValue &&
                !request.waterReminders.HasValue &&
                !request.workoutAlerts.HasValue &&
                !request.promotionOffers.HasValue)
            {
                return BadRequest(new
                {
                    error = "Please provide at least one preference to update."
                });
            }

            var filter = Builders<NotificationsResponseModel>.Filter.Eq(
                x => x.userName,
                request.userName);

            var updates = new List<UpdateDefinition<NotificationsResponseModel>>();

            if (request.mealReminders.HasValue)
            {
                updates.Add(
                    Builders<NotificationsResponseModel>.Update.Set(
                        x => x.mealReminders,
                        request.mealReminders.Value));
            }

            if (request.waterReminders.HasValue)
            {
                updates.Add(
                    Builders<NotificationsResponseModel>.Update.Set(
                        x => x.waterReminders,
                        request.waterReminders.Value));
            }

            if (request.workoutAlerts.HasValue)
            {
                updates.Add(
                    Builders<NotificationsResponseModel>.Update.Set(
                        x => x.workoutAlerts,
                        request.workoutAlerts.Value));
            }

            if (request.promotionOffers.HasValue)
            {
                updates.Add(
                    Builders<NotificationsResponseModel>.Update.Set(
                        x => x.promotionOffers,
                        request.promotionOffers.Value));
            }

            var updateDefinition =
                Builders<NotificationsResponseModel>.Update.Combine(updates);

            var options = new FindOneAndUpdateOptions<NotificationsResponseModel>
            {
                ReturnDocument = ReturnDocument.After
            };

            var updatedPreference =
                await _mongoService.NotificationsPreferences.FindOneAndUpdateAsync(
                    session,
                    filter,
                    updateDefinition,
                    options);

            if (updatedPreference == null)
            {
                await session.AbortTransactionAsync();

                return NotFound(new
                {
                    error = $"No notification preferences found for user '{request.userName}'."
                });
            }

            await session.CommitTransactionAsync();

            return Ok(new
            {
                message = "Notification setting has been updated successfully."
            });
        }
        catch (Exception ex)
        {
            await session.AbortTransactionAsync();

            return StatusCode(500, new
            {
                error = ex.Message
            });
        }
    }

    [AllowAnonymous]
    [HttpDelete]
    [Route("DeleteAllUserNotifications")]
    public async Task<string> DeleteAllUserNotifications(List<SentNotificationLog> sentNotificationLogs)
    {
        if (sentNotificationLogs == null || !sentNotificationLogs.Any())
        {
            return "No notifications selected.";
        }

        var userName = sentNotificationLogs.First().userName;

        var notificationKeys = sentNotificationLogs
            .Select(x => x.notificationKey)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        if (!notificationKeys.Any())
        {
            return "No valid notifications selected.";
        }

        var filter = Builders<SentNotificationLog>.Filter.And(
        Builders<SentNotificationLog>.Filter.Eq(
            x => x.userName,
            userName),
        Builders<SentNotificationLog>.Filter.In(
            x => x.notificationKey,
            notificationKeys)
        );

        var result = await _mongoService.SentNotificationLogs
            .DeleteManyAsync(filter);

        return $"{result.DeletedCount} notification(s) deleted successfully.";
    }

    [AllowAnonymous]
    [HttpPatch]
    [Route("UpdateUserNotifications")]
    public async Task<string> UpdateUserNotifications(List<SentNotificationLog> sentNotificationLogs)
    {
        using var session = await _mongoService.Client.StartSessionAsync();

        if (sentNotificationLogs == null || !sentNotificationLogs.Any())
        {
            return "No notifications selected.";
        }

        var userName = sentNotificationLogs.First().userName;

        var notificationKeys = sentNotificationLogs
            .Where(x => !x.isRead)
            .Select(x => x.notificationKey)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        if (!notificationKeys.Any())
        {
            return "No valid notifications selected.";
        }

        var filter = Builders<SentNotificationLog>.Filter.And(
        Builders<SentNotificationLog>.Filter.Eq(
            x => x.userName,
            userName),
        Builders<SentNotificationLog>.Filter.In(
            x => x.notificationKey,
            notificationKeys)
        );
        var update = Builders<SentNotificationLog>.Update.Set(x => x.isRead, true);

        try
        {
            session.StartTransaction();

            var result = await _mongoService.SentNotificationLogs
                .UpdateManyAsync(session, filter, update);
            session.CommitTransaction();

            return $"{result.ModifiedCount} notification(s) marked as read successfully.";
        }
        catch (Exception ex)
        {
            await session.AbortTransactionAsync();
            return $"Error occurred while updating notifications: {ex.Message}";
        }

    }
}
