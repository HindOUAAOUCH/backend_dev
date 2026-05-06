using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using AddressCorrection.src.AddressCorrection.Domain.Entities;
using MongoDB.Driver;

namespace AddressCorrection.src.AddressCorrection.Infrastructure.Persistence;

public sealed class UsageTrackingRepository : IUsageTrackingRepository
{
    private readonly IMongoCollection<UsageTracking> _collection;

    public UsageTrackingRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<UsageTracking>("usage_tracking");
    }

    public async Task<UsageTracking?> GetTodayAsync()
    {
        var today = DateTime.UtcNow.Date;
        return await _collection
            .Find(u => u.Date == today)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Incrémente les compteurs du jour de manière atomique via un upsert MongoDB.
    /// Élimine la race condition du pattern read-then-insert.
    /// </summary>
    public async Task IncrementAsync(bool fromCache)
    {
        var today = DateTime.UtcNow.Date;

        var filter = Builders<UsageTracking>.Filter.Eq(u => u.Date, today);

        var update = Builders<UsageTracking>.Update
            .SetOnInsert(u => u.Date, today)
            .SetOnInsert(u => u.LimitReached, false)
            .Inc(u => u.RequestCount, 1)
            .Inc(u => u.CacheHitCount, fromCache ? 1 : 0)
            .Inc(u => u.LlmCallCount, fromCache ? 0 : 1);

        await _collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
    }
}
