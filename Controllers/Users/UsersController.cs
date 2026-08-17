using DietManagementWebAPI.Models.DBModels;
using DietManagementWebAPI.Models.EmailModels;
using DietManagementWebAPI.Models.OtherModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
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
public class UsersController : ControllerBase
{
    private readonly MongoDbService _mongoService;
    private readonly IConfiguration _configuration;

    public UsersController(MongoDbService mongoService, IConfiguration configuration)
    {
        _mongoService = mongoService;
        _configuration = configuration;
    }

    [HttpDelete]
    [Route("DeleteAllData")]
    public async Task<IActionResult> DeleteAllData(string userName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return BadRequest(new
                {
                    message = "Username is required."
                });
            }

            var userNotificationFilter = Builders<SentNotificationLog>.Filter.Eq(x => x.userName, userName);
            var userNotificationResult = await _mongoService.SentNotificationLogs.DeleteManyAsync(userNotificationFilter);

            // Delete Weight Logs
            var weightFilter = Builders<UserWeightModel>.Filter.Eq(x => x.userName, userName);
            var weightResult = await _mongoService.UserWeightLogs.DeleteManyAsync(weightFilter);

            var burnCaloriesFilter = Builders<WorkoutBurnCaloriesModel>.Filter.Eq(x => x.userName, userName);
            var burnCaloriesResult = await _mongoService.WorkoutBurnCalories.DeleteManyAsync(burnCaloriesFilter);

            var prefFilter = Builders<UsersPreferencesModel>.Filter.Eq(x => x.userName, userName);
            var prefResult = await _mongoService.UserPreferences.DeleteManyAsync(prefFilter);

            var workoutSessionFilter = Builders<WorkoutSessionModel>.Filter.Eq(x => x.userName, userName);
            var workoutSessionResult = await _mongoService.WorkoutSessions.DeleteManyAsync(workoutSessionFilter);

            var workoutLogsFilter = Builders<WorkoutExerciseModel>.Filter.Eq(x => x.userName, userName);
            var workoutLogsResult = await _mongoService.WorkoutExerciseLogs.DeleteManyAsync(workoutLogsFilter);

            // Delete OTP Records
            var otpFilter = Builders<UserOtpsModel>.Filter.Eq(x => x.userName, userName);
            var otpResult = await _mongoService.UserOtps.DeleteManyAsync(otpFilter);

            // Delete User Meals
            var usersMealsFilter = Builders<UsersMealsData>.Filter.Eq(x => x.userName, userName);
            var usersMealsResult = await _mongoService.UsersMeals.DeleteManyAsync(usersMealsFilter);

            var notificationPrefsFilter = Builders<NotificationsResponseModel>.Filter.Eq(x => x.userName, userName);
            var notificationPrefsResult = await _mongoService.NotificationsPreferences.DeleteOneAsync(notificationPrefsFilter);

            // Delete User Profile
            var profileFilter = Builders<UserProfileData>.Filter.Eq(x => x.userName, userName);
            var profileResult = await _mongoService.UserProfile.DeleteOneAsync(profileFilter);

            // Delete Users
            var usersFilter = Builders<UsersDBModel>.Filter.Eq(x => x.userName, userName);
            var usersResult = await _mongoService.Users.DeleteOneAsync(usersFilter);


            return Ok("Your account and all associated data have been successfully deleted.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "An error occurred while deleting an account.",
                error = ex.Message
            });
        }
    }

    [HttpDelete]
    [Route("ResetAllData")]
    public async Task<IActionResult> ResetAllData(string userName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return BadRequest(new
                {
                    message = "Username is required."
                });
            }

            var userNotificationFilter = Builders<SentNotificationLog>.Filter.Eq(x => x.userName, userName);
            var userNotificationResult = await _mongoService.SentNotificationLogs.DeleteManyAsync(userNotificationFilter);

            // Delete Weight Logs
            var weightFilter = Builders<UserWeightModel>.Filter.Eq(x => x.userName, userName);
            var weightResult = await _mongoService.UserWeightLogs.DeleteManyAsync(weightFilter);

            // Delete User Meals
            var usersMealsFilter = Builders<UsersMealsData>.Filter.Eq(x => x.userName, userName);
            var usersMealsResult = await _mongoService.UsersMeals.DeleteManyAsync(usersMealsFilter);

            var workoutSessionFilter = Builders<WorkoutSessionModel>.Filter.Eq(x => x.userName, userName);
            var workoutSessionResult = await _mongoService.WorkoutSessions.DeleteManyAsync(workoutSessionFilter);

            var workoutLogsFilter = Builders<WorkoutExerciseModel>.Filter.Eq(x => x.userName, userName);
            var workoutLogsResult = await _mongoService.WorkoutExerciseLogs.DeleteManyAsync(workoutLogsFilter);

            var burnCaloriesFilter = Builders<WorkoutBurnCaloriesModel>.Filter.Eq(x => x.userName, userName);
            var burnCaloriesResult = await _mongoService.WorkoutBurnCalories.DeleteManyAsync(burnCaloriesFilter);

            return Ok("Your account data has been reset successfully.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "An error occurred while deleting an account.",
                error = ex.Message
            });
        }
    }

    private string GenerateJwtToken(UsersDBModel user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        var key = Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"]!);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
            new Claim(ClaimTypes.Name, user.userName),
            new Claim(ClaimTypes.Email, user.email)
        }),

            Expires = DateTime.UtcNow.AddDays(7),

            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    [HttpGet]
    [Route("PingCloud")]
    public async Task<IActionResult> PingCloud()
    {
        return Ok("API is running!");
    }

    [HttpPost]
    [Route("RegisterUser")]
    public async Task<IActionResult> RegisterUser([FromBody] RegisterRequest request)
    {
        try
        {
            // Check if email already exists
            var existingEmail = await _mongoService.Users
                .Find(u => u.email == request.email)
                .FirstOrDefaultAsync();

            if (existingEmail != null)
                return Conflict(new { message = "Email already exists!" });

            // Hash Password
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.password, 12);

            // Generate unique username
            string finalUserName = await GenerateUniqueGuestUserNameAsync();

            var userPref = new UsersPreferencesModel
            {
                userName = finalUserName,
                lastUpdated = DateTime.UtcNow.ToString(),
                personalizedAds = true,
                shareAnalytics = false
            };

            var notificationsPref = new NotificationsResponseModel
            {
                userName = finalUserName,
                waterReminders = true,
                workoutAlerts = true,
                mealReminders = true,
                promotionOffers = true
            };

            // Insert User
            var newUser = new UsersDBModel
            {
                firstName = request.firstName,
                lastName = request.lastName,
                email = request.email,
                password = hashedPassword,
                userName = finalUserName,
                twoStepAuth= false,
                timeZone= request.timeZone
            };

            await _mongoService.Users.InsertOneAsync(newUser);

            var newProfile = new UserProfileData
            {
                userName = finalUserName,
                Goal = request.Goal,
                Gender = request.Gender,
                FatTargetG = request.FatTargetG,
                CarbTargetG = request.CarbTargetG,
                ProteinTargetG = request.ProteinTargetG,
                HeightCm = request.HeightCm,
                WeightKg = request.WeightKg,
                Age = request.Age,
                DailyCalorieTarget = request.DailyCalorieTarget,
                GymTiming = request.GymTiming,
                isPerformGym = request.isPerformGym,
            };

            await _mongoService.UserProfile.InsertOneAsync(newProfile);

            await _mongoService.UserPreferences.InsertOneAsync(userPref);

            await _mongoService.NotificationsPreferences.InsertOneAsync(notificationsPref);

            return Ok(new
            {
                message = "Registration successful!",
                userName = finalUserName
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // Helper Method
    private async Task<string> GenerateUniqueGuestUserNameAsync()
    {
        string userName;
        var random = new Random();

        do
        {
            int number = random.Next(100000, 999999);
            userName = $"Guest@{number}";

            // Check if username already exists
            var existing = await _mongoService.Users
                .Find(u => u.userName == userName)
                .FirstOrDefaultAsync();

            if (existing == null)
                return userName;   // Unique found

        } while (true); // Loop until unique username is found
    }

    [AllowAnonymous]
    [HttpPost]
    [Route("ChangeUserPassword")]
    public async Task<IActionResult> ChangeUserPassword([FromBody] ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.userName) ||
        string.IsNullOrWhiteSpace(request.currentPassword) ||
        string.IsNullOrWhiteSpace(request.newPassword))
            return BadRequest(new { message = "All fields are required" });

        if (request.currentPassword == request.newPassword)
            return Ok(new { success = false, message = "New password must be different from current password" });

        var user = await _mongoService.Users
            .Find(u => u.userName == request.userName)
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound(new { success = false, message = "User not found!" });

        if (!BCrypt.Net.BCrypt.Verify(request.currentPassword, user.password))
            return BadRequest(new { success = false, message = "Current password is incorrect" });
       
        string newHashedPassword = BCrypt.Net.BCrypt.HashPassword(request.newPassword);

        var update = Builders<UsersDBModel>.Update.Set(u => u.password, newHashedPassword);
        var result = await _mongoService.Users.UpdateOneAsync(
            u => u.userName == request.userName, update);

        if (result.ModifiedCount > 0)
            return Ok(new { success = true, message = "Password updated successfully" });

        return Ok(new { success = false, message = "Password update failed! Please try again." });
    }

    [HttpPost]
    [Route("ForgotUserPassword")]
    public async Task<IActionResult> ForgotUserPassword([FromBody] ForgotPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.userName) ||
        string.IsNullOrWhiteSpace(request.newPassword))
            return BadRequest(new { message = "All fields are required" });

        var user = await _mongoService.Users
            .Find(u => u.userName == request.userName)
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound(new { success = false, message = "User not found!" });

        string newHashedPassword = BCrypt.Net.BCrypt.HashPassword(request.newPassword);

        var update = Builders<UsersDBModel>.Update.Set(u => u.password, newHashedPassword);
        var result = await _mongoService.Users.UpdateOneAsync(
            u => u.userName == request.userName, update);

        if (result.ModifiedCount > 0)
            return Ok(new { success = true, message = "Password updated successfully" });

        return Ok(new { success = false, message = "Password update failed! Please try again." });
    }

    [HttpPost]
    [Route("ProcessLoginApproval")]
    public async Task<IActionResult> ProcessLoginApproval(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return BadRequest(new { message = "Email and Password are required" });

        var user = await _mongoService.Users
                                     .Find(u => u.email == email)
                                     .FirstOrDefaultAsync();

        if (user == null)
            return NotFound(new { message = $"Email '{email}' not found" });

        // Verify hashed password
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.password);

        if (!isPasswordValid)
            return Unauthorized(new { message = "Incorrect password. Please try again." });

        // Generate JWT Token
        var token = GenerateJwtToken(user);

        var response = new UserLoginResponse
        {
            firstName = user.firstName,
            lastName = user.lastName,
            email = user.email,
            userName = user.userName,
            token = token,
            twoStepAuth= user.twoStepAuth,
            fcmToken= user.fcmToken,
            timeZone= user.timeZone,
        };

        return Ok(response);
    }

    [HttpPost]
    [Route("UpdateFcmToken")]
    public async Task<IActionResult> UpdateFcmToken([FromBody] UpdateFcmTokenRequest request)
    {
        using var session = await _mongoService.Client.StartSessionAsync();

        try
        {
            session.StartTransaction();
            // 2. Find the user by their email (or ID)
            var filter = Builders<UsersDBModel>.Filter.Eq("email", request.email); 

            // 3. Update the fcmToken field
            var update = Builders<UsersDBModel>.Update.Set("fcmToken", request.fcmToken);
            // 4. Execute the update in MongoDB
            var result = await _mongoService.Users.UpdateOneAsync(filter, update);
            if (result.MatchedCount == 0)
            {
                return NotFound(new { error = "User not found." });
            }

            await session.CommitTransactionAsync();

            return Ok(new { success = true, message = "FCM token updated successfully." });
        }
        catch (Exception ex)
        {
            await session.AbortTransactionAsync();
            return BadRequest(new { error = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPatch]
    [Route("UpdateUserName")]
    public async Task<ActionResult<object>> UpdateUserName(string email, string updatedUserName)
    {
        // 1. Validate inputs
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Email is required!" });

        if (string.IsNullOrWhiteSpace(updatedUserName))
            return BadRequest(new { message = "New Username is required!" });

        // 2. Find the user by email
        var existingUser = await _mongoService.Users
                                              .Find(u => u.email == email)
                                              .FirstOrDefaultAsync();

        if (existingUser == null)
            return NotFound(new { message = $"Email '{email}' not found" });

        var oldUserName = existingUser.userName;

        // 3. If new userName is same as old, no update needed
        if (oldUserName == updatedUserName)
            return Ok(new
            {
                message = "Username is already the same.",
                user = existingUser
            });

        // 4. ✅ NEW: Check if the updatedUserName is already taken by ANOTHER user
        var duplicateUser = await _mongoService.Users
                                                .Find(u => u.userName == updatedUserName && u.email != email)
                                                .FirstOrDefaultAsync();

        if (duplicateUser != null)
            return Conflict(new
            {
                message = $"Username '{updatedUserName}' is already taken by another user. Please select another one!",
            });

        // 5. Start transaction and update all 3 collections
        using var session = await _mongoService.Client.StartSessionAsync();

        try
        {
            session.StartTransaction();

            // Update Users
            var userFilter = Builders<UsersDBModel>.Filter.Eq(u => u.email, email);
            var userUpdate = Builders<UsersDBModel>.Update.Set(u => u.userName, updatedUserName);

            var updatedUser = await _mongoService.Users.FindOneAndUpdateAsync(
                session,
                userFilter,
                userUpdate,
                new FindOneAndUpdateOptions<UsersDBModel, UsersDBModel>
                {
                    ReturnDocument = ReturnDocument.After
                });

            if (updatedUser == null)
                throw new Exception("User update failed");

            // Update UserProfile
            var profileFilter = Builders<UserProfileData>.Filter.Eq(p => p.userName, oldUserName);
            var profileUpdate = Builders<UserProfileData>.Update.Set(p => p.userName, updatedUserName);
            var profileResult = await _mongoService.UserProfile.UpdateManyAsync(session, profileFilter, profileUpdate);

            // Update UsersMeals
            var mealsFilter = Builders<UsersMealsData>.Filter.Eq(m => m.userName, oldUserName);
            var mealsUpdate = Builders<UsersMealsData>.Update.Set(m => m.userName, updatedUserName);
            var mealsResult = await _mongoService.UsersMeals.UpdateManyAsync(session, mealsFilter, mealsUpdate);

            // Update UsersWeight
            var weightFilter = Builders<UserWeightModel>.Filter.Eq(m => m.userName, oldUserName);
            var weightUpdate = Builders<UserWeightModel>.Update.Set(m => m.userName, updatedUserName);
            var weightResult = await _mongoService.UserWeightLogs.UpdateManyAsync(session, weightFilter, weightUpdate);

            var userPrefFilter = Builders<UsersPreferencesModel>.Filter.Eq(m => m.userName, oldUserName);
            var userPrefUpdate = Builders<UsersPreferencesModel>.Update.Set(m => m.userName, updatedUserName);
            var userPrefResult = await _mongoService.UserPreferences.UpdateManyAsync(session, userPrefFilter, userPrefUpdate);

            var workoutSessionFilter = Builders<WorkoutSessionModel>.Filter.Eq(m => m.userName, oldUserName);
            var workoutSessionUpdate = Builders<WorkoutSessionModel>.Update.Set(m => m.userName, updatedUserName);
            var workoutSessionResult = await _mongoService.WorkoutSessions.UpdateManyAsync(session, workoutSessionFilter, workoutSessionUpdate);

            var workoutLogsFilter = Builders<WorkoutExerciseModel>.Filter.Eq(m => m.userName, oldUserName);
            var workoutLogsUpdate = Builders<WorkoutExerciseModel>.Update.Set(m => m.userName, updatedUserName);
            var workoutLogsResult = await _mongoService.WorkoutExerciseLogs.UpdateManyAsync(session, workoutLogsFilter, workoutLogsUpdate);

            var burnCaloriesFilter = Builders<WorkoutBurnCaloriesModel>.Filter.Eq(m => m.userName, oldUserName);
            var burnCaloriesUpdate = Builders<WorkoutBurnCaloriesModel>.Update.Set(m => m.userName, updatedUserName);
            var burnCaloriesResult = await _mongoService.WorkoutBurnCalories.UpdateManyAsync(session, burnCaloriesFilter, burnCaloriesUpdate);

            var userOtpsFilter = Builders<UserOtpsModel>.Filter.Eq(m => m.userName, oldUserName);
            var userOtpsUpdate = Builders<UserOtpsModel>.Update.Set(m => m.userName, updatedUserName);
            var userOtpsResult = await _mongoService.UserOtps.UpdateManyAsync(session, userOtpsFilter, userOtpsUpdate);

            var notificationsPrefsFilter = Builders<NotificationsResponseModel>.Filter.Eq(m => m.userName, oldUserName);
            var notificationsPrefsUpdate = Builders<NotificationsResponseModel>.Update.Set(m => m.userName, updatedUserName);
            var notificationsPrefsResult = await _mongoService.NotificationsPreferences.UpdateManyAsync(session, notificationsPrefsFilter, notificationsPrefsUpdate);

            var userNotificationsFilter = Builders<SentNotificationLog>.Filter.Eq(m => m.userName, oldUserName);
            var userNotificationsUpdate = Builders<SentNotificationLog>.Update.Set(m => m.userName, updatedUserName);
            var userNotificationsResult = await _mongoService.SentNotificationLogs.UpdateManyAsync(session, userNotificationsFilter, userNotificationsUpdate);

            await session.CommitTransactionAsync();

            return Ok(new
            {
                message = "Username updated successfully.",
                username = updatedUser.userName,
            });
        }
        catch (Exception ex)
        {
            await session.AbortTransactionAsync();
            return StatusCode(500, new
            {
                message = "Update failed, all changes rolled back",
                error = ex.Message
            });
        }
    }

    [AllowAnonymous]
    [HttpPatch]
    [Route("UpdateTwoStepAuth")]
    public async Task<ActionResult<object>> UpdateTwoStepAuth([FromBody] twoStepAuthRequest twoStepAuth)
    {
        using var session = await _mongoService.Client.StartSessionAsync();

        try
        {
            session.StartTransaction();

            var userFilter = Builders<UsersDBModel>.Filter.Eq(u => u.email, twoStepAuth.email)
                            & Builders<UsersDBModel>.Filter.Eq(u => u.userName, twoStepAuth.userName);

            var userUpdate = Builders<UsersDBModel>.Update.Set(u => u.twoStepAuth, twoStepAuth.twoStepAuth);

            var updatedUser = await _mongoService.Users.FindOneAndUpdateAsync(
                session,
                userFilter,
                userUpdate,
                new FindOneAndUpdateOptions<UsersDBModel, UsersDBModel>
                {
                    ReturnDocument = ReturnDocument.After
                });

            await session.CommitTransactionAsync();

            if(updatedUser.twoStepAuth)
            {
                return Ok(new
                {
                    updatedUser.userName,
                    updatedUser.email,
                    updatedUser.twoStepAuth,
                    message = "Two-factor authentication has been enabled successfully."
                });
            }
            else
            {
                return Ok(new
                {
                    updatedUser.userName,
                    updatedUser.email,
                    updatedUser.twoStepAuth,
                    message = "Two-factor authentication has been disabled successfully."
                });
            }
            
        }
        catch (Exception ex)
        {
            await session.AbortTransactionAsync();
            return StatusCode(500, new
            {
                message = "Update failed, all changes rolled back",
                error = ex.Message
            });
        }
    }


    [AllowAnonymous]
    [HttpPut]
    [Route("UpdateUserProfile")]
    public async Task<ActionResult<UserProfileData>> UpdateUserProfile(
    string userName,
    [FromBody] UserProfileUpdateDto profileData)
    {
        try
        {
            // Check if user exists
            var existingUser = await _mongoService.UserProfile
                                                  .Find(u => u.userName == userName)
                                                  .FirstOrDefaultAsync();

            if (existingUser == null)
            {
                return NotFound(new { message = $"User '{userName}' not found." });
            }

            // Build dynamic update definition
            var updateBuilder = Builders<UserProfileData>.Update;
            var updates = new List<UpdateDefinition<UserProfileData>>();

            // Use reflection or manual checks to only update provided fields
            if (profileData.Gender != null) updates.Add(updateBuilder.Set(u => u.Gender, profileData.Gender));
            if (profileData.Age.HasValue) updates.Add(updateBuilder.Set(u => u.Age, profileData.Age));
            if (profileData.HeightCm != null) updates.Add(updateBuilder.Set(u => u.HeightCm, profileData.HeightCm));
            if (profileData.WeightKg.HasValue) updates.Add(updateBuilder.Set(u => u.WeightKg, profileData.WeightKg));
            if (profileData.Goal != null) updates.Add(updateBuilder.Set(u => u.Goal, profileData.Goal));
            if (profileData.DailyCalorieTarget != null) updates.Add(updateBuilder.Set(u => u.DailyCalorieTarget, profileData.DailyCalorieTarget));
            if (profileData.ProteinTargetG != null) updates.Add(updateBuilder.Set(u => u.ProteinTargetG, profileData.ProteinTargetG));
            if (profileData.CarbTargetG != null) updates.Add(updateBuilder.Set(u => u.CarbTargetG, profileData.CarbTargetG));
            if (profileData.FatTargetG != null) updates.Add(updateBuilder.Set(u => u.FatTargetG, profileData.FatTargetG));
            if (profileData.GymTiming != null) updates.Add(updateBuilder.Set(u => u.GymTiming, profileData.GymTiming));
            if (profileData.isPerformGym != null) updates.Add(updateBuilder.Set(u => u.isPerformGym, profileData.isPerformGym));

            if (updates.Count == 0)
            {
                return BadRequest("No valid fields provided to update.");
            }

            var combinedUpdate = updateBuilder.Combine(updates);

            // Update the document
            var result = await _mongoService.UserProfile.UpdateOneAsync(
                Builders<UserProfileData>.Filter.Eq(u => u.userName, userName),
                combinedUpdate
            );

            if (result.MatchedCount == 0)
            {
                return NotFound("User not found.");
            }

            // Return the updated document
            var updatedUser = await _mongoService.UserProfile
                                                .Find(u => u.userName == userName)
                                                .FirstOrDefaultAsync();

            return Ok(updatedUser);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPatch]
    [Route("UpdateUserPreferences")]
    public async Task<ActionResult> UpdateUserPreferences(
    [FromQuery] string userName,
    [FromBody] UpdateUserPreferencesRequest request)
    {
        using var session = await _mongoService.Client.StartSessionAsync();

        try
        {
            session.StartTransaction();

            // Check whether at least one field is provided
            if (!request.shareAnalytics.HasValue &&
                !request.personalizedAds.HasValue)
            {
                return BadRequest(new
                {
                    error = "Please provide at least one preference to update."
                });
            }

            var filter = Builders<UsersPreferencesModel>.Filter.Eq(
                x => x.userName,
                userName);

            var updates = new List<UpdateDefinition<UsersPreferencesModel>>();

            if (request.shareAnalytics.HasValue)
            {
                updates.Add(
                    Builders<UsersPreferencesModel>.Update.Set(
                        x => x.shareAnalytics,
                        request.shareAnalytics.Value));
            }

            if (request.personalizedAds.HasValue)
            {
                updates.Add(
                    Builders<UsersPreferencesModel>.Update.Set(
                        x => x.personalizedAds,
                        request.personalizedAds.Value));
            }

            // Always update timestamp
            updates.Add(
                Builders<UsersPreferencesModel>.Update.Set(
                    x => x.lastUpdated,
                    DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")));

            var updateDefinition =
                Builders<UsersPreferencesModel>.Update.Combine(updates);

            var options = new FindOneAndUpdateOptions<UsersPreferencesModel>
            {
                ReturnDocument = ReturnDocument.After
            };

            var updatedPreference =
                await _mongoService.UserPreferences.FindOneAndUpdateAsync(
                    session,
                    filter,
                    updateDefinition,
                    options);

            if (updatedPreference == null)
            {
                await session.AbortTransactionAsync();

                return NotFound(new
                {
                    error = $"No preferences found for user '{userName}'."
                });
            }

            await session.CommitTransactionAsync();

            return Ok(new
            {
                message = "Preferences updated successfully.",
                data = new
                {
                    updatedPreference.userName,
                    updatedPreference.shareAnalytics,
                    updatedPreference.personalizedAds,
                    updatedPreference.lastUpdated
                }
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
}
