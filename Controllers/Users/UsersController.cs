using DietManagementWebAPI.Models.Auth;
using DietManagementWebAPI.Models.DBModels;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

public class UsersController : ControllerBase
{
    private readonly MongoDbService _mongoService;

    public UsersController(MongoDbService mongoService)
    {
        _mongoService = mongoService;
    }

    [HttpPost]
    [Route("InsertNewUser")]
    public async Task<IActionResult> InsertNewUser([FromBody] UsersDBModel registerAuth)
    {
        var _registerAuth = new UsersDBModel
        { 
            email = registerAuth.email,
            firstName= registerAuth.firstName, 
            lastName= registerAuth.lastName,
            password= registerAuth.password,
            userName= registerAuth.userName,
        };

        await _mongoService.Users.InsertOneAsync(_registerAuth);

        return Ok(new { message = "Registration Successfull!"});
    }

    [HttpGet]
    [Route("GetUserByUsername/{userName}")]
    public async Task<ActionResult<UsersDBModel>> GetUserByUsername(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return BadRequest(new { message = "Username is required" });

        var user = await _mongoService.Users
                                     .Find(u => u.userName == userName)
                                     .FirstOrDefaultAsync();

        if (user == null)
            return NotFound(new { message = $"User with username '{userName}' not found" });

        return Ok(user);
    }

    [HttpPost]
    [Route("InsertUserProfile")]
    public async Task<IActionResult> InsertUserProfile([FromBody] UserProfileData registerAuth)
    {
        var _registerAuth = new UserProfileData
        {
            Goal = registerAuth.Goal,
            userName = registerAuth.userName,
            Gender = registerAuth.Gender,
            FatTargetG = registerAuth.FatTargetG,
            CarbTargetG = registerAuth.CarbTargetG,
            ProteinTargetG = registerAuth.ProteinTargetG,
            HeightCm = registerAuth.HeightCm,
            WeightKg = registerAuth.WeightKg,
            Age = registerAuth.Age,
            DailyCalorieTarget = registerAuth.DailyCalorieTarget,
        };

        await _mongoService.UserProfile.InsertOneAsync(_registerAuth);

        return Ok(new { message = "User profile updated successfully!" });
    }

    [HttpGet]
    [Route("GetAllUsers")]
    public async Task<ActionResult> GetAllUsers()
    {
        var users = await _mongoService.Users.Find(_ => true).ToListAsync();
        return Ok(users);
    }
}
