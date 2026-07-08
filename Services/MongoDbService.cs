using MongoDB.Driver;
using Microsoft.Extensions.Configuration;

public class MongoDbService
{
    private readonly IMongoDatabase _database;

    public MongoDbService(IConfiguration config)
    {
        // 1️⃣ Try environment variable first (used in production/Render)
        var connectionString = config["MONGODB_CONNECTION_STRING"];

        // 2️⃣ Fallback to appsettings.json (used in local development)
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = config.GetConnectionString("MongoDb");
        }

        // 3️⃣ Last resort fallback (for local dev with hardcoded value)
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = config["ConnectionStrings:MongoDb"];
        }

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "MongoDB connection string is not configured. " +
                "Set MONGODB_CONNECTION_STRING environment variable " +
                "or ConnectionStrings:MongoDb in appsettings.json."
            );
        }

        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(config["DietManagementDB"]);
    }

    public IMongoCollection<Meal> Meals => _database.GetCollection<Meal>("Meals");
}
