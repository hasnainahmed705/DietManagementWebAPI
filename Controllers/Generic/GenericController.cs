using DietManagementWebAPI.Controllers.Generic;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
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

    [HttpPost("CommonGetGlobal")]
    public async Task<IActionResult> CommonGetGlobal([FromBody] GenericRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.apiPathValue))
            return BadRequest(new { message = "Collection name (apiPathValue) is required" });

        try
        {
            var collection = _mongoService.GetCollection(request.apiPathValue);

            // Build Filter
            var filterBuilder = Builders<dynamic>.Filter;
            var filter = filterBuilder.Empty;

            foreach (var f in request.filters)
            {
                if (string.IsNullOrEmpty(f.filterValue)) continue;

                filter = f.filterType.ToLower() switch
                {
                    "contains" => filter & filterBuilder.Regex(f.filterName, new BsonRegularExpression(f.filterValue, "i")),
                    "gt" => filter & filterBuilder.Gt(f.filterName, f.filterValue),
                    "lt" => filter & filterBuilder.Lt(f.filterName, f.filterValue),
                    _ => filter & filterBuilder.Eq(f.filterName, f.filterValue)   // default = eq
                };
            }

            // Sorting
            var sort = request.sortOrder.ToLower() == "desc"
                ? Builders<dynamic>.Sort.Descending(request.sortField)
                : Builders<dynamic>.Sort.Ascending(request.sortField ?? "_id");

            // Pagination
            int limit = request.ttlRecords > 0 ? request.ttlRecords : 5;
            int skip = request.page * limit;

            var data = await collection.Find(filter)
                                       .Sort(sort)
                                       .Skip(skip)
                                       .Limit(limit)
                                       .ToListAsync();

            var totalRecords = await collection.CountDocumentsAsync(filter);

            return Ok(new
            {
                records = data,
                totalRecords,
                page = request.page,
                pageSize = limit,
                totalPages = (int)Math.Ceiling((double)totalRecords / limit)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}