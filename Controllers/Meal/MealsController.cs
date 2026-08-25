using DietManagementWebAPI.Models.DBModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Org.BouncyCastle.Tls;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class MealsController : ControllerBase
{
    private readonly MongoDbService _mongoService;

    public MealsController(MongoDbService mongoService)
    {
        _mongoService = mongoService;
    }

    [HttpGet]
    [Route("GetAllMeals")]
    public async Task<ActionResult> GetAll()
    {
        var meals = await _mongoService.Meals.Find(_ => true).ToListAsync();
        return Ok(meals);
    }

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
            bool isDbUpdateRequired = false;
            string nextFoodId;

            var user = await _mongoService.Users.Find(u => u.userName == meals.userName).FirstOrDefaultAsync();
            DateTime todayLocal = TimeZoneHelper.GetUserLocalDate(user.timeZone);
            string todayStr = todayLocal.ToString("yyyy-MM-dd");

            var lastMeal = await _mongoService.UsersMeals
            .Find(FilterDefinition<UsersMealsData>.Empty)
            .Sort(Builders<UsersMealsData>.Sort.Descending(x => x.FoodId))
            .Limit(1)
            .FirstOrDefaultAsync();


            if (lastMeal == null || string.IsNullOrEmpty(lastMeal.FoodId))
            {
                nextFoodId = "UM000001";          // First record
            }
            else
            {
                // Extract the number part after "UM"
                string numberPart = lastMeal.FoodId.Substring(2);

                if (int.TryParse(numberPart, out int lastNumber))
                {
                    nextFoodId = $"UM{(lastNumber + 1).ToString("D6")}";  // UM000046 etc.
                }
                else
                {
                    // Safety fallback if the format is wrong
                    nextFoodId = "UM000001";
                }
            }

            meals.FoodId = nextFoodId;

            if (string.IsNullOrEmpty(user.lastMealDateStr))
            {
                user.currentStreak = 1;
                user.lastMealDateStr = todayStr;
                user.longestStreak = 1;
                isDbUpdateRequired = true;
            }
            else
            {
                DateTime lastMealDate = DateTime.Parse(user.lastMealDateStr);
                TimeSpan difference = todayLocal.Date - lastMealDate.Date;

                if (difference.TotalDays == 1)
                {
                    user.currentStreak += 1;
                    user.lastMealDateStr = todayStr;

                    if (user.currentStreak > user.longestStreak)
                        user.longestStreak = user.currentStreak;

                    isDbUpdateRequired = true;
                }
                else if (difference.TotalDays == 2)
                {
                    user.previousStreak = user.currentStreak;
                    user.currentStreak = 1;
                    user.lastMealDateStr = todayStr;
                    isDbUpdateRequired = true;
                }
                else if (difference.TotalDays > 2)
                {
                    user.previousStreak = 0;
                    user.currentStreak = 1;
                    user.lastMealDateStr = todayStr;
                    isDbUpdateRequired = true;
                }
            }

            if (isDbUpdateRequired)
            {
                using var session = await _mongoService.Client.StartSessionAsync();

                session.StartTransaction();
                var update = Builders<UsersDBModel>.Update
                    .Set(u => u.currentStreak, user.currentStreak)
                    .Set(u => u.longestStreak, user.longestStreak)
                    .Set(u => u.lastMealDateStr, user.lastMealDateStr)
                    .Set(u => u.previousStreak, user.previousStreak);
                await _mongoService.Users.UpdateOneAsync(u => u.userName == user.userName, update);

                await session.CommitTransactionAsync();
            }

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
    public async Task<IActionResult> DeleteUserMeal(string userName, string foodId)
    {
        var filter = Builders<UsersMealsData>.Filter.Where(
            u => u.userName == userName && u.FoodId == foodId
        );

        var meal = await _mongoService.UsersMeals
                                      .Find(filter)
                                      .FirstOrDefaultAsync();

        if (meal == null)
            return NotFound(new { message = $"Meal not found for the user: {userName}!" });

        try
        {
            var result = await _mongoService.UsersMeals.DeleteOneAsync(filter);

            return result.IsAcknowledged && result.DeletedCount > 0
                ? Ok("Meal has been deleted successfully")
                : NotFound("Meal has not been deleted!");
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

}