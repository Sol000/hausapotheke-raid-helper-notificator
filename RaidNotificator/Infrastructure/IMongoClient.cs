using RaidNotificator.DTOs;

namespace RaidNotificator.Infrastructure;

public interface IMongoClientBot
{
    Task InsertEventAsync(RaidEvent raidEvent);
    
    Task<RaidEvent?> GetEventAsync(string id);
    
    Task UpdateEventAsync(RaidEvent raidEvent);
}