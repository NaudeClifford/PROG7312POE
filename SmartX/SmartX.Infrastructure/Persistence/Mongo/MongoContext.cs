using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace SmartX.Infrastructure.Persistence.Mongo;

public class MongoContext
{
    private readonly IMongoDatabase _database;

    public MongoContext(IConfiguration configuration)
    {
        var connectionString =
            configuration["Mongo:ConnectionString"];

        var databaseName =
            configuration["Mongo:DatabaseName"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Mongo:ConnectionString is not configured.");
        }

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException(
                "Mongo:DatabaseName is not configured.");
        }

        var client = new MongoClient(connectionString);

        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string name)
    {
        return _database.GetCollection<T>(name);
    }
}