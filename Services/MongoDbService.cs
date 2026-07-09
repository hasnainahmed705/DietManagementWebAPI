using DietManagementWebAPI.Models.Auth;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

public class MongoDbService
{
    private readonly IMongoDatabase _database;

    public MongoDbService(IConfiguration config)
    {
        // Try environment variable first (used in Render)
        var connectionString = config["MONGODB_CONNECTION_STRING"];

        // Fallback to appsettings.json (used locally)
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = config.GetConnectionString("MongoDB");
        }

        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(config["MONGODB_DATABASE"]);
    }

    public IMongoCollection<Meal> Meals => _database.GetCollection<Meal>("Meals");
    public IMongoCollection<RegisterAuth> Users => _database.GetCollection<RegisterAuth>("Users");
}
