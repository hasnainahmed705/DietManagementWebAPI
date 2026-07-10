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
    [Route("InsertNewUser")]
    public async Task<IActionResult> InsertNewUser([FromBody] UsersDBModel registerAuth)
    {
        var user = await _mongoService.Users
                                    .Find(u => u.email == registerAuth.email)
                                    .FirstOrDefaultAsync();
        if (user != null)
        {
            return Conflict(new { message = "Email Already exists!" });
        }

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
