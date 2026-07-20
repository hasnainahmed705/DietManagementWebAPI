using DietManagementWebAPI.Models.DBModels;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class UserWeightController : ControllerBase
{
    private readonly MongoDbService _mongoService;

    public UserWeightController(MongoDbService mongoService)
    {
        _mongoService = mongoService;
    }

    [HttpPost]
    [Route("AddUserWeightLog")]
    public async Task<IActionResult> AddUserWeightLog(UserWeightModel weight)
    {
        if (weight.userName == "")
            return BadRequest(new { message = "Username is required!" });

        try
        {
            await _mongoService.UserWeightLogs.InsertOneAsync(weight);
            return Ok(weight);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

}