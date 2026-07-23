using DietManagementWebAPI.Models.DBModels;
using DietManagementWebAPI.Models.EmailModels;
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

            // Insert User
            var newUser = new UsersDBModel
            {
                firstName = request.firstName,
                lastName = request.lastName,
                email = request.email,
                password = hashedPassword,
                userName = finalUserName,
                twoStepAuth= false
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
                DailyCalorieTarget = request.DailyCalorieTarget
            };

            await _mongoService.UserProfile.InsertOneAsync(newProfile);

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
            twoStepAuth= user.twoStepAuth
        };

        return Ok(response);
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
}
