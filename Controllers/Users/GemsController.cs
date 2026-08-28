using DietManagementWebAPI.Models.DBModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class GemsCOntroller : ControllerBase
{
    private readonly MongoDbService _mongoService;
    private readonly IConfiguration _configuration;

    public GemsCOntroller(MongoDbService mongoService, IConfiguration configuration)
    {
        _mongoService = mongoService;
        _configuration = configuration;
    }

    [HttpGet]
    [Route("GetStreakAndGems")]
    public async Task<IActionResult> GetStreakAndGems(string userName)
    {
        using var session = await _mongoService.Client.StartSessionAsync();

        var user = await _mongoService.Users.Find(u => u.userName == userName).FirstOrDefaultAsync();
        if (user == null) return NotFound();
        DateTime todayLocal = TimeZoneHelper.GetUserLocalDate(user.timeZone);

        session.StartTransaction();
        // Safety check: Did they break the streak by inactivity?
        if (!string.IsNullOrEmpty(user.lastMealDateStr))
        {
            DateTime lastMealDate = DateTime.Parse(user.lastMealDateStr);
            TimeSpan gap = todayLocal.Date - lastMealDate.Date;
            if (gap.TotalDays == 2 && user.currentStreak > 0)
            {
                // Missed 1 day so far. Move current to previous.
                user.previousStreak = user.currentStreak;
                user.currentStreak = 0;

                var update = Builders<UsersDBModel>.Update
                    .Set(u => u.currentStreak, 0)
                    .Set(u => u.previousStreak, user.previousStreak);
                await _mongoService.Users.UpdateOneAsync(u => u.userName == userName, update);
            }
            else if (gap.TotalDays > 2 && (user.currentStreak > 0 || user.previousStreak > 0))
            {
                // Missed 2+ days. Reset entirely.
                user.currentStreak = 0;
                user.previousStreak = 0;

                var update = Builders<UsersDBModel>.Update
                    .Set(u => u.currentStreak, 0)
                    .Set(u => u.previousStreak, 0);
                await _mongoService.Users.UpdateOneAsync(u => u.userName == userName, update);
            }

            await session.CommitTransactionAsync();
        }


        return Ok(new
        {
            currentStreak = user.currentStreak,
            longestStreak = user.longestStreak,
            previousStreak = user.previousStreak, // If > 0, Flutter will show the "Restore" button
            totalGems = user.totalGems,
            gemCardIndex = user.gemCardIndex,
            lastGemCollectionDate= user.lastGemCollectionDate,
        });

    }

    [HttpPost]
    [Route("CollectGemCard")]
    public async Task<IActionResult> CollectGemCard(string userName, int gemCardIndex)
    {
        using var session = await _mongoService.Client.StartSessionAsync();

        var user = await _mongoService.Users.Find(u => u.userName == userName).FirstOrDefaultAsync();
        if (user == null) return NotFound();

        // 1. Check if cards need to be reset (e.g., they collect daily)
        DateTime todayLocal = TimeZoneHelper.GetUserLocalDate(user.timeZone);
        string todayStr = todayLocal.ToString("yyyy-MM-dd");

        if (user.lastGemCollectionDate != todayStr && user.gemCardIndex >= 8)
        {
            // It's a new day and they finished yesterday's cards, reset index
            user.gemCardIndex = 0;
        }

        if (user.gemCardIndex >= 8)
            return BadRequest(new { message = "All 8 cards already collected today." });

        // Optional but recommended: Ensure the index requested matches the user's actual progress
        if (user.gemCardIndex != gemCardIndex)
            return BadRequest(new { message = "Invalid card index. Please refresh." });

        // 2. Determine Gem Reward
        // Flutter sends a 0-based index (0 to 7)
        int[] gemValues = { 1, 5, 3, 2, 5, 2, 4, 3 };

        // Safety check just in case the index is out of bounds
        if (gemCardIndex < 0 || gemCardIndex > 7)
            return BadRequest(new { message = "Invalid card index." });

        int gemsAwarded = gemValues[gemCardIndex];

        session.StartTransaction();

        // 3. Update User
        var update = Builders<UsersDBModel>.Update
            .Inc(u => u.totalGems, gemsAwarded)
            .Inc(u => u.gemCardIndex, 1) // IMPORTANT: Increment by exactly 1
            .Set(u => u.lastGemCollectionDate, todayStr);

        await _mongoService.Users.UpdateOneAsync(u => u.userName == userName, update);

        await session.CommitTransactionAsync();

        return Ok(new
        {
            awardedGems = gemsAwarded,
            totalGems = user.totalGems + gemsAwarded,
            nextCardIndex = user.gemCardIndex + 1
        });
    }

    [HttpPatch]
    [Route("UpdateGemCardIndex")] 
    public async void UpdateGemCardIndex(string userName)
    {
        using var session = await _mongoService.Client.StartSessionAsync();
        session.StartTransaction();
        var user = await _mongoService.Users.UpdateOneAsync(u => u.userName == userName, Builders<UsersDBModel>.Update.Set(u => u.gemCardIndex, 0));
        await session.CommitTransactionAsync();
    }

        [HttpPost]
    [Route("RestoreStreak")]
    public async Task<IActionResult> RestoreStreak(string userName)
    {
        using var session = await _mongoService.Client.StartSessionAsync();

        var user = await _mongoService.Users.Find(u => u.userName == userName).FirstOrDefaultAsync();
        DateTime todayLocal = TimeZoneHelper.GetUserLocalDate(user.timeZone);
        string todayStr = todayLocal.ToString("yyyy-MM-dd");
        if (user == null) return NotFound();

        // 1. Check if the user has enough gems (costs 10 gems)
        if (user.totalGems < 10)
        {
            return BadRequest(new { message = "You need at least 10 gems to restore your streak!" });
        }

        // 2. Check if the user is eligible (must have a previous streak that was broken exactly 1 day ago)
        // If previousStreak is 0, they either didn't break a streak, or they missed 2+ days (which should clear previousStreak).
        if (user.previousStreak <= 0)
        {
            return BadRequest(new { message = "Streak restoration is not available!" });
        }

        // 3. Perform the restoration
        user.totalGems -= 10;

        // Add the lost streak back to their current streak
        // Example: They had 15, missed yesterday, logged today. 
        // current = 1, previous = 15. After restore: current = 16.
        user.currentStreak += user.previousStreak + 1;

        if (user.currentStreak > user.longestStreak)
        {
            user.longestStreak = user.currentStreak;
        }

        // 4. Reset previous streak so it cannot be restored again
        user.previousStreak = 0;

        // 5. Update the database
        var filter = Builders<UsersDBModel>.Filter.Eq(u => u.userName, userName);
        var update = Builders<UsersDBModel>.Update
            .Set(u => u.totalGems, user.totalGems)
            .Set(u => u.currentStreak, user.currentStreak)
            .Set(u => u.longestStreak, user.longestStreak)
            .Set(u => u.previousStreak, user.previousStreak)
            .Set(u => u.lastMealDateStr, todayStr);

        await _mongoService.Users.UpdateOneAsync(filter, update);

        return Ok(new
        {
            message = "Streak restored successfully!",
            totalGems = user.totalGems,
            currentStreak = user.currentStreak,
            longestStreak = user.longestStreak
        });
    }


}