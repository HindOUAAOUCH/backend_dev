using AddressCorrection.src.AddressCorrection.Domain.Entities;
using MongoDB.Driver;

namespace AddressCorrection.src.AddressCorrection.Infrastructure.Persistence;

public sealed class MongoDbContext
{
    public IMongoDatabase Database { get; }

    public MongoDbContext(MongoDbSettings settings)
    {
        var client = new MongoClient(settings.ConnectionString);
        Database = client.GetDatabase(settings.DatabaseName);
        EnsureIndexes();
    }

    public IMongoCollection<T> GetCollection<T>(string name) =>
        Database.GetCollection<T>(name);

    /// <summary>
    /// Crée les index nécessaires à la performance et à l'intégrité.
    /// MongoDB ignore silencieusement les créations d'index déjà existants.
    /// </summary>
    private void EnsureIndexes()
    {
        // Index unique sur normalizedAddress → cache lookup O(log n) + garantie unicité
        var addressCollection = Database.GetCollection<AddressCorrectionRecord>("address_corrections");
        addressCollection.Indexes.CreateOne(new CreateIndexModel<AddressCorrectionRecord>(
            Builders<AddressCorrectionRecord>.IndexKeys.Ascending(r => r.NormalizedAddress),
            new CreateIndexOptions { Unique = true, Name = "idx_normalizedAddress_unique" }));

        // Index sur SentAt → GetTodayRequestsAsync performant
        var requestCollection = Database.GetCollection<CorrectionRequest>("correction_requests");
        requestCollection.Indexes.CreateOne(new CreateIndexModel<CorrectionRequest>(
            Builders<CorrectionRequest>.IndexKeys.Descending(r => r.SentAt),
            new CreateIndexOptions { Name = "idx_sentAt_desc" }));

        // Index unique sur Date → un seul document par jour dans usage_tracking
        var usageCollection = Database.GetCollection<UsageTracking>("usage_tracking");
        usageCollection.Indexes.CreateOne(new CreateIndexModel<UsageTracking>(
            Builders<UsageTracking>.IndexKeys.Ascending(u => u.Date),
            new CreateIndexOptions { Unique = true, Name = "idx_date_unique" }));
    }
}
