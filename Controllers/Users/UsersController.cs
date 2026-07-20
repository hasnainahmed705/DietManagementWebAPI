using DietManagementWebAPI.Models.DBModels;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly MongoDbService _mongoService;

    public UsersController(MongoDbService mongoService)
    {
        _mongoService = mongoService;
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

            // Generate unique username
            string finalUserName = await GenerateUniqueGuestUserNameAsync();

            // Insert into Users Collection
            var newUser = new UsersDBModel
            {
                firstName = request.firstName,
                lastName = request.lastName,
                email = request.email,
                password = request.password,
                userName = finalUserName
            };

            await _mongoService.Users.InsertOneAsync(newUser);

            // Insert into UserProfile Collection
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

    [HttpPost]
    [Route("ChangeUserPassword")]
    public async Task<ActionResult> ChangeUserPassword([FromBody] ChangePasswordRequest request)
    {
        // 1. Validate input
        if (string.IsNullOrWhiteSpace(request.userName) ||
            string.IsNullOrWhiteSpace(request.currentPassword) ||
            string.IsNullOrWhiteSpace(request.newPassword))
            return BadRequest(new { message = "All fields are required" });

        // 2. Find user by userName
        var user = await _mongoService.Users
                                     .Find(u => u.userName == request.userName)
                                     .FirstOrDefaultAsync();

        if (user == null)
            return NotFound(new { success = false, message = "User not found!" });

        // 3. Match current password
        if (user.password != request.currentPassword)
            return Ok(new { success = false, message = "Current password is incorrect" });

        if (request.currentPassword == request.newPassword)
            return Ok(new { success = false, message = "New password must be different from current password" });

        // 4. Update with new password
        var update = Builders<UsersDBModel>.Update.Set(u => u.password, request.newPassword);
        var result = await _mongoService.Users.UpdateOneAsync(
            u => u.userName == request.userName,
            update
        );

        if (result.ModifiedCount > 0)
            return Ok(new { success = true, message = "Password updated successfully" });

        return Ok(new { success = false, message = "Password update failed! Please try again." });
    }


    [HttpPost]
    [Route("ProcessLoginApproval")]
    public async Task<ActionResult<UsersDBModel>> ProcessLoginApproval(string email,string password)
    {
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password))
            return BadRequest(new { message = "Email and Password are required" });
        
        var user = await _mongoService.Users
                                     .Find(u => (u.email == email))
                                     .FirstOrDefaultAsync();

        if (user == null)
            return NotFound(new { message = $"Email '{email}' not found" });

        if (user.password != password)
            return Unauthorized(new { message = "Incorrect password. Please try again." });

        var response = new UserLoginResponse
        {
            firstName = user.firstName,
            lastName = user.lastName,
            email = user.email,
            userName = user.userName
        };

        return Ok(response);
    }

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


    //[HttpPost]
    //[Route("InsertUserProfile")]
    //public async Task<IActionResult> InsertUserProfile([FromBody] UserProfileData registerAuth)
    //{
    //    var _registerAuth = new UserProfileData
    //    {
    //        Goal = registerAuth.Goal,
    //        userName = registerAuth.userName,
    //        Gender = registerAuth.Gender,
    //        FatTargetG = registerAuth.FatTargetG,
    //        CarbTargetG = registerAuth.CarbTargetG,
    //        ProteinTargetG = registerAuth.ProteinTargetG,
    //        HeightCm = registerAuth.HeightCm,
    //        WeightKg = registerAuth.WeightKg,
    //        Age = registerAuth.Age,
    //        DailyCalorieTarget = registerAuth.DailyCalorieTarget,
    //    };

    //    await _mongoService.UserProfile.InsertOneAsync(_registerAuth);

    //    return Ok(new { message = "User profile updated successfully!" });
    //}

    //[HttpGet]
    //[Route("GetAllUsers")]
    //public async Task<ActionResult> GetAllUsers()
    //{
    //    var users = await _mongoService.Users.Find(_ => true).ToListAsync();
    //    return Ok(users);
    //}
}
