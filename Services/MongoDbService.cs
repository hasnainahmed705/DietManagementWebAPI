using MongoDB.Driver;

public class MongoDbService
{
    private readonly IMongoDatabase _database;

    public MongoDbService(IConfiguration config)
    {
        var client = new MongoClient(config.GetConnectionString("mongodb+srv://hasnainwork705_Admin:v7YghSbpvoTukeLD@dietmanagementcluster.hrvbcpm.mongodb.net/\r\n"));
        _database = client.GetDatabase(config["DietManagementDB"]);
    }

    public IMongoCollection<Meal> Meals => _database.GetCollection<Meal>("Meals");
}