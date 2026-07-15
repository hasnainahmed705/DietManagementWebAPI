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

        try
        {
            // Get last FoodId for sequence
            var lastMeal = await _mongoService.Meals
                .Find(m => true)
                .SortByDescending(m => m.FoodId)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastMeal != null && !string.IsNullOrEmpty(lastMeal.FoodId))
            {
                string lastId = lastMeal.FoodId.Replace("F", "").Trim();
                if (int.TryParse(lastId, out int lastNum))
                {
                    nextNumber = lastNum + 1;
                }
            }

            // Auto assign FoodId
            foreach (var meal in meals)
            {
                meal.FoodId = $"F{nextNumber:D3}";
                nextNumber++;
            }

            // Insert
            await _mongoService.Meals.InsertManyAsync(meals);

            // Return the full inserted documents
            return Ok(meals);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost]
    [Route("InsertUserMeal")]
    public async Task<IActionResult> InsertUserMeal(UsersMealsData meals)
    {
        if (meals.FoodName == "")
            return BadRequest(new { message = "No meals provided." });

        try
        {
            await _mongoService.UsersMeals.InsertOneAsync(meals);
            return Ok(meals);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete]
    [Route("DeleteUserMeal")]
    public async Task<IActionResult> DeleteUserMeal(string userName, string foodName)
    {
        var filter = Builders<UsersMealsData>.Filter.Where(
            u => u.userName == userName && u.FoodName == foodName
        );

        var meal = await _mongoService.UsersMeals
                                      .Find(filter)
                                      .FirstOrDefaultAsync();

        if (meal == null)
            return NotFound(new { message = $"Meal: {foodName} not found for the user: {userName}!" });

        try
        {
            var result = await _mongoService.UsersMeals.DeleteOneAsync(filter);

            return result.IsAcknowledged && result.DeletedCount > 0
                ? Ok(new { message = "Meal has been deleted successfully", deletedMeal = meal })
                : NotFound(new { message = "Meal has not been deleted!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

}