using DietManagementWebAPI.Models.Auth;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

[ApiController]
[Route("api/[controller]")]
public class MealsController : ControllerBase
{
    private readonly MongoDbService _mongoService;

    public MealsController(MongoDbService mongoService)
    {
        _mongoService = mongoService;
    }

    // GET: api/meals/ReturnAllMeals
    [HttpGet]
    [Route("GetAllMeals")]
    public async Task<ActionResult> GetAll()
    {
        var meals = await _mongoService.Meals.Find(_ => true).ToListAsync();
        return Ok(meals);
    }

    // POST: api/meals/BulkInsertMeals
    [HttpPost]
    [Route("InsertMealsMulti")]
    public async Task<IActionResult> BulkInsert(List<Meal> meals)
    {
        if (meals == null || meals.Count == 0)
            return BadRequest(new { message = "No meals provided." });

        await _mongoService.Meals.InsertManyAsync(meals);
        return Ok(new { message = "Bulk insert successful", count = meals.Count });
    }
  }