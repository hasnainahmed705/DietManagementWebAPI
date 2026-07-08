using MongoDB.Driver;
using Microsoft.Extensions.Configuration;

public class MongoDbService
{
    private readonly IMongoDatabase _database;

    public MongoDbService(IConfiguration config)
    {
        // ✅ Correctly read the connection string by NAME
        var connectionString = config.GetConnectionString("MongoDb");

        // ✅ Fallback in case GetConnectionString doesn't work
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = config["ConnectionStrings:MongoDb"];
        }

        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(config["DietManagementDB"]);
    }

    public IMongoCollection<Meal> Meals => _database.GetCollection<Meal>("Meals");
}
