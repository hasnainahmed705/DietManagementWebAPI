using DietManagementWebAPI.Models.DBModels;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

public class MongoDbService
{
    private readonly IMongoDatabase _database;
    private readonly IMongoClient _client;

    public MongoDbService(IConfiguration config)
    {
        // Try environment variable first (used in Render)
        var connectionString = config["MONGODB_CONNECTION_STRING"];

        // Fallback to appsettings.json (used locally)
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = config.GetConnectionString("MongoDB");
        }

        _client = new MongoClient(connectionString);
        _database = _client.GetDatabase(config["MONGODB_DATABASE"]);
    }

    // 👇 ADD THIS PROPERTY — exposes the IMongoClient for transactions
    public IMongoClient Client => _client;

    public IMongoCollection<dynamic> GetCollection(string collectionName)
    {
        return _database.GetCollection<dynamic>(collectionName);
    }

    public IMongoCollection<Meal> Meals => _database.GetCollection<Meal>("Meals");
    public IMongoCollection<UsersDBModel> Users => _database.GetCollection<UsersDBModel>("Users");
    public IMongoCollection<UserProfileData> UserProfile => _database.GetCollection<UserProfileData>("UserProfile");
    public IMongoCollection<UsersMealsData> UsersMeals => _database.GetCollection<UsersMealsData>("UsersMeals");
    public IMongoCollection<UserWeightModel> UserWeightLogs => _database.GetCollection<UserWeightModel>("UserWeightLogs");
}
