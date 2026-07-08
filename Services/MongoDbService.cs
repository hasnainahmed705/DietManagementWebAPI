using MongoDB.Driver;

public class MongoDbService
{
    private readonly IMongoDatabase _database;

    public MongoDbService(IConfiguration config)
    {
        var client = new MongoClient(config.GetConnectionString("MongoDB"));
        _database = client.GetDatabase(config["DatabaseName"]);
    }

    public IMongoCollection<Meal> Meals => _database.GetCollection<Meal>("Meals");
}