using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using RaidNotificator.DTOs;

namespace RaidNotificator.Infrastructure;

public class MongoDbClient : IMongoClientBot
{
    private readonly ILogger<MongoDbClient> _logger;
    private readonly IMongoCollection<BsonDocument> _collection;
    
    public MongoDbClient(ILogger<MongoDbClient> logger, IConfiguration configuration)
    {
        IMongoClient client;
        
        var cs = Environment.GetEnvironmentVariable("ENV") == "PROD"
            ? configuration.GetConnectionString("MongoDBConnectionStringProd")
            : configuration.GetConnectionString("MongoDBConnectionStringDev");

        client = new MongoClient(cs ?? throw new InvalidOperationException("MongoDB connection string missing."));
        
        _collection = client.GetDatabase("hausapotheke").GetCollection<BsonDocument>("events");
        
        _logger = logger;
        
        _logger.LogInformation("MongoDB initialized");
    }
    
    public async Task InsertEventAsync(RaidEvent raidEvent)
    {
        var document = raidEvent.ToBsonDocument();
        _logger.LogInformation("Inserted event with id {RaidEventId} into MongoDB", raidEvent.Id);
        await _collection.InsertOneAsync(document);
    }

    public async Task<RaidEvent?> GetEventAsync(string id)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
        var document = _collection.Find(filter).FirstOrDefault();

        if (document == null)
        {
            _logger.LogWarning($"No event found with id {id}");
            return await Task.FromResult<RaidEvent?>(null);
        }

        var raidEvent = new RaidEvent
        {
            Id = document["_id"].AsString,
            Title = document["Title"].AsString,
            LastUpdated = document["LastUpdated"].AsInt64,
            SignUps = document["SignUps"].AsBsonArray.Select(s => new SignUp
            {
                UserId = s["UserId"].AsString,
                Name = s["Name"].AsString,
                ClassName = s["ClassName"].AsString,
                RoleName = s["RoleName"].AsString,
                Position = s["Position"].AsInt32
            }).ToList()
        };
        
        return await Task.FromResult(raidEvent)!;
    }

    public async Task UpdateEventAsync(RaidEvent raidEvent)
    {
        await _collection.ReplaceOneAsync(Builders<BsonDocument>.Filter.Eq("_id", raidEvent.Id), raidEvent.ToBsonDocument());
    }
}