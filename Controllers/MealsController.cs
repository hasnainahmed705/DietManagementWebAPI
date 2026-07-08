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

    [HttpGet]
    public async Task<List<Meal>> GetAll() =>
        await _mongoService.Meals.Find(_ => true).ToListAsync();

    [HttpGet("{id}")]
    public async Task<ActionResult<Meal>> GetById(string id)
    {
        var meal = await _mongoService.Meals.Find(m => m.Id == id).FirstOrDefaultAsync();
        return meal;
    }

    [HttpPost]
    public async Task<ActionResult<Meal>> Create(Meal meal)
    {
        await _mongoService.Meals.InsertOneAsync(meal);
        return CreatedAtAction(nameof(GetById), new { id = meal.Id }, meal);
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> BulkInsert(List<Meal> meals)
    {
        await _mongoService.Meals.InsertManyAsync(meals);
        return Ok(new { message = "Bulk insert successful", count = meals.Count });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, Meal meal)
    {
        var result = await _mongoService.Meals.ReplaceOneAsync(m => m.Id == id, meal);
        return result.ModifiedCount > 0 ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _mongoService.Meals.DeleteOneAsync(m => m.Id == id);
        return result.DeletedCount > 0 ? NoContent() : NotFound();
    }
}