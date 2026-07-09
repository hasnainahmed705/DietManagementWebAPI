using DietManagementWebAPI.Models.Auth;
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
    public async Task<IActionResult> InsertNewUser([FromBody] RegisterAuth registerAuth)
    {
        var _registerAuth = new RegisterAuth 
        { 
            email = registerAuth.email,
            firstName= registerAuth.firstName, 
            lastName= registerAuth.lastName,
            password= registerAuth.password
        };

        await _mongoService.Users.InsertOneAsync(_registerAuth);
        return Ok(new { message = "Registration Successfull!"});
    }

    [HttpGet]
    [Route("GetAllUsers")]
    public async Task<ActionResult> GetAllUsers()
    {
        var users = await _mongoService.Users.Find(_ => true).ToListAsync();
        return Ok(users);
    }
}
