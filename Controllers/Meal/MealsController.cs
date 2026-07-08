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
    [Route("ReturnAllMeals")]
    public async Task<ActionResult<List<Meal>>> GetAll()
    {
        var meals = await _mongoService.Meals.Find(_ => true).ToListAsync();
        return Ok(meals);
    }

    // GET: api/meals/ReturnMealById/{id}
    [HttpGet]
    [Route("ReturnMealById/{id}")]
    public async Task<ActionResult<Meal>> GetById(string id)
    {
        var meal = await _mongoService.Meals.Find(m => m.Id == id).FirstOrDefaultAsync();

        if (meal == null)
            return NotFound();

        return Ok(meal);
    }

    // POST: api/meals/CreateMeal
    [HttpPost]
    [Route("CreateMeal")]
    public async Task<ActionResult<Meal>> Create(Meal meal)
    {
        await _mongoService.Meals.InsertOneAsync(meal);
        return CreatedAtAction(nameof(GetById), new { id = meal.Id }, meal);
    }

    // POST: api/meals/BulkInsertMeals
    [HttpPost]
    [Route("BulkInsertMeals")]
    public async Task<IActionResult> BulkInsert(List<Meal> meals)
    {
        if (meals == null || meals.Count == 0)
            return BadRequest(new { message = "No meals provided." });

        await _mongoService.Meals.InsertManyAsync(meals);
        return Ok(new { message = "Bulk insert successful", count = meals.Count });
    }

    // PUT: api/meals/UpdateMeal/{id}
    [HttpPut]
    [Route("UpdateMeal/{id}")]
    public async Task<IActionResult> Update(string id, Meal meal)
    {
        var result = await _mongoService.Meals.ReplaceOneAsync(m => m.Id == id, meal);

        if (result.MatchedCount == 0)
            return NotFound();

        return NoContent();
    }

    // DELETE: api/meals/DeleteMeal/{id}
    [HttpDelete]
    [Route("DeleteMeal/{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _mongoService.Meals.DeleteOneAsync(m => m.Id == id);
        return result.DeletedCount > 0 ? NoContent() : NotFound();
    }
}