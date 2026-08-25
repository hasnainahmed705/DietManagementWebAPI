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
            gemCardIndex = user.gemCardIndex
        });

    }

    [HttpPost]
    [Route("CollectGemCard")]
    public async Task<IActionResult> CollectGemCard(string userName,int gemCardIndex)
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
        // 2. Determine Gem Reward 

        int gemsAwarded = 0;

        switch(gemCardIndex)
        {
            case 1:
                gemsAwarded = 1;
                break;
            case 2:
                gemsAwarded = 5;
                break;
            case 3:
                gemsAwarded = 3;
                break;
            case 4:
                gemsAwarded = 2;
                break;
            case 5:
                gemsAwarded = 5;
                break;
            case 6:
                gemsAwarded = 2;
                break;
            case 7:
                gemsAwarded = 4;
                break;
            case 8:
                gemsAwarded = 3;
                break;
        }

        session.StartTransaction();
        // 3. Update User
        var update = Builders<UsersDBModel>.Update
            .Inc(u => u.totalGems, gemsAwarded)
            .Inc(u => u.gemCardIndex, gemCardIndex)
            .Set(u => u.lastGemCollectionDate, todayStr);
        await _mongoService.Users.UpdateOneAsync(u => u.userName == userName, update);

        await session.CommitTransactionAsync();

        return Ok(new
        {
            awardedGems = gemsAwarded,
            totalGems = user.totalGems + gemsAwarded,
            nextCardIndex = gemCardIndex + 1
        });
    }
}