using MongoDB.Driver;
using Microsoft.Extensions.Configuration;

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
        _database = client.GetDatabase(config["DietManagementDB"]);
    }

    public IMongoCollection<Meal> Meals => _database.GetCollection<Meal>("Meals");
}
