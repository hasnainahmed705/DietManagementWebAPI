using DietManagementWebAPI.Controllers.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class GenericController : ControllerBase
{
    private readonly MongoDbService _mongoService;
    private readonly QueryBuilderService _queryBuilder;

    public GenericController(MongoDbService mongoService, QueryBuilderService queryBuilder)
    {
        _mongoService = mongoService;
        _queryBuilder = queryBuilder;
    }

    [Authorize]
    [HttpPost("CommonGetGlobal")]
    public async Task<IActionResult> CommonGetGlobal([FromBody] GenericRequest request)
    {

        Console.WriteLine("JWT DEBUG =====================");
        Console.WriteLine($"IsAuthenticated: {User.Identity?.IsAuthenticated}");
        Console.WriteLine($"Identity Name: {User.Identity?.Name}");
        Console.WriteLine($"Claims Count: {User.Claims.Count()}");
        Console.WriteLine($"Authorization Header: {Request.Headers["Authorization"].FirstOrDefault()}");
        Console.WriteLine("=================================");

        if (!User.Identity.IsAuthenticated)
            return Unauthorized(new { message = "Token validation failed in backend" });

        if (string.IsNullOrWhiteSpace(request.apiPathValue))
            return BadRequest(new { message = "Collection name (apiPathValue) is required" });

        try
        {
            var collection = _mongoService.GetCollection(request.apiPathValue);

            var filter = _queryBuilder.BuildFilter(request.filters);
            var sort = _queryBuilder.BuildSort(request.sortField, request.sortOrder);

            var data = await _queryBuilder.ExecuteQueryAsync(collection,filter,sort,request.page,request.ttlRecords);

            var totalRecords = await collection.CountDocumentsAsync(filter);

            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}