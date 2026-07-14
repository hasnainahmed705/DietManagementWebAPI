using DietManagementWebAPI.Models.DBModels;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

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
    [Route("ProcessLoginApproval")]
    public async Task<ActionResult<UsersDBModel>> ProcessLoginApproval(string email,string password)
    {
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password))
            return BadRequest(new { message = "Email and Password are required" });
        
        var user = await _mongoService.Users
                                     .Find(u => (u.email == email && u.password == password))
                                     .FirstOrDefaultAsync();

        if (user == null)
            return NotFound(new { message = $"Email '{email}' not found" });

        var response = new UserLoginResponse
        {
            firstName = user.firstName,
            lastName = user.lastName,
            email = user.email,
            userName = user.userName
        };

        return Ok(response);
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
