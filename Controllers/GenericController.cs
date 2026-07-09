using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

[ApiController]
[Route("api/[controller]")]
public class GenericController : ControllerBase
{
    private readonly MongoDbService _mongoService;

    public GenericController(MongoDbService mongoService)
    {
        _mongoService = mongoService;
    }

    [HttpGet("CommonGetGlobal")]
    public async Task<IActionResult> CommonGetGlobal(string collectionName)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
            return BadRequest(new { message = "Collection name is required" });

        try
        {
            var collection = _mongoService.GetCollection(collectionName); // Dynamic method

            var data = await collection.Find(FilterDefinition<dynamic>.Empty)
                                       .ToListAsync();

            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error fetching data from {collectionName}", error = ex.Message });
        }
    }
}