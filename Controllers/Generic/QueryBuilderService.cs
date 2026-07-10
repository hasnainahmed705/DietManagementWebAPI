using DietManagementWebAPI.Controllers.Generic;
using MongoDB.Bson;
using MongoDB.Driver;

public class QueryBuilderService
{
    // Build Filter
    public FilterDefinition<dynamic> BuildFilter(List<FilterItem> filters)
    {
        var filterBuilder = Builders<dynamic>.Filter;
        var filter = filterBuilder.Empty;

        if (filters == null || filters.Count == 0)
            return filter;

        foreach (var f in filters)
        {
            if (string.IsNullOrWhiteSpace(f.filterName) || string.IsNullOrWhiteSpace(f.filterValue))
                continue;

            filter = f.filterType?.ToLower() switch
            {
                "contains" => filter & filterBuilder.Regex(f.filterName, new BsonRegularExpression(f.filterValue, "i")),
                "gt" => filter & filterBuilder.Gt(f.filterName, f.filterValue),
                "lt" => filter & filterBuilder.Lt(f.filterName, f.filterValue),
                _ => filter & filterBuilder.Eq(f.filterName, f.filterValue)
            };
        }

        return filter;
    }

    // Build Sort
    public SortDefinition<dynamic> BuildSort(string sortField, string sortOrder)
    {
        if (string.IsNullOrWhiteSpace(sortField))
            return Builders<dynamic>.Sort.Ascending("_id");

        return sortOrder?.ToLower() == "desc"
            ? Builders<dynamic>.Sort.Descending(sortField)
            : Builders<dynamic>.Sort.Ascending(sortField);
    }

    // Execute Query with Pagination
    public async Task<List<dynamic>> ExecuteQueryAsync(
        IMongoCollection<dynamic> collection,
        FilterDefinition<dynamic> filter,
        SortDefinition<dynamic> sort,
        int page,
        int ttlRecords)
    {
        int limit = ttlRecords > 0 ? ttlRecords : 5;
        int skip = page * limit;

        return await collection.Find(filter)
                               .Sort(sort)
                               .Skip(skip)
                               .Limit(limit)
                               .ToListAsync();
    }
}