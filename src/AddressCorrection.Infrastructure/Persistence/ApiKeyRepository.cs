using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using AddressCorrection.src.AddressCorrection.Domain.Entities;
using AddressCorrection.src.AddressCorrection.Infrastructure.Persistence.Documents;
using MongoDB.Driver;

namespace AddressCorrection.src.AddressCorrection.Infrastructure.Persistence;

public sealed class ApiKeyRepository : IApiKeyRepository
{
    private readonly IMongoCollection<ApiKeyDocument> _collection;

    public ApiKeyRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<ApiKeyDocument>("api_keys");
    }

    public async Task<ApiKey?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var doc = await _collection
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync(ct);

        return doc is null ? null : ToEntity(doc);
    }

    public async Task<IReadOnlyList<ApiKey>> GetByIntegrationIdAsync(string integrationId, CancellationToken ct = default)
    {
        var docs = await _collection
            .Find(x => x.IntegrationId == integrationId)
            .ToListAsync(ct);

        return docs.Select(ToEntity).ToList();
    }

    public async Task<ApiKey?> GetWithHashByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        var doc = await _collection
            .Find(x => x.Prefix == prefix)
            .FirstOrDefaultAsync(ct);

        return doc is null ? null : ToEntity(doc);
    }

    public async Task<int> CountActiveAsync(string integrationId, DateTime now, CancellationToken ct = default)
    {
        var filter = Builders<ApiKeyDocument>.Filter.And(
            Builders<ApiKeyDocument>.Filter.Eq(x => x.IntegrationId, integrationId),
            Builders<ApiKeyDocument>.Filter.Eq(x => x.IsRevoked, false),
            Builders<ApiKeyDocument>.Filter.Or(
                Builders<ApiKeyDocument>.Filter.Eq(x => x.ExpiresAt, null),
                Builders<ApiKeyDocument>.Filter.Gt(x => x.ExpiresAt, now)));

        var count = await _collection.CountDocumentsAsync(filter, cancellationToken: ct);
        return (int)count;
    }

    public async Task CreateAsync(ApiKey apiKey, CancellationToken ct = default)
    {
        var doc = ToDocument(apiKey);
        await _collection.InsertOneAsync(doc, cancellationToken: ct);
    }

    public async Task RevokeAsync(string id, DateTime revokedAt, CancellationToken ct = default)
    {
        var update = Builders<ApiKeyDocument>.Update
            .Set(x => x.IsRevoked, true);

        await _collection.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
    }

    public async Task RecordUsageAsync(string id, DateTime usedAt, CancellationToken ct = default)
    {
        var update = Builders<ApiKeyDocument>.Update
            .Set(x => x.LastUsedAt, usedAt)
            .Inc(x => x.UsageCount, 1);

        await _collection.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
    }

    // ── Mappers privés ─────────────────────────────────────────────────────────

    private static ApiKey ToEntity(ApiKeyDocument doc) => new()
    {
        Id            = doc.Id,
        IntegrationId = doc.IntegrationId,
        ClientId      = doc.ClientId,
        Name          = doc.Name,
        Prefix        = doc.Prefix,
        HashedKey     = doc.HashedKey,
        Salt          = doc.Salt,
        Scopes        = doc.Scopes.AsReadOnly(),
        IsRevoked     = doc.IsRevoked,
        ExpiresAt     = doc.ExpiresAt,
        LastUsedAt    = doc.LastUsedAt,
        UsageCount    = doc.UsageCount,
        CreatedAt     = doc.CreatedAt,
    };

    private static ApiKeyDocument ToDocument(ApiKey e) => new()
    {
        Id            = e.Id,
        IntegrationId = e.IntegrationId,
        ClientId      = e.ClientId,
        Name          = e.Name,
        Prefix        = e.Prefix,
        HashedKey     = e.HashedKey,
        Salt          = e.Salt,
        Scopes        = e.Scopes.ToList(),
        IsRevoked     = e.IsRevoked,
        ExpiresAt     = e.ExpiresAt,
        LastUsedAt    = e.LastUsedAt,
        UsageCount    = e.UsageCount,
        CreatedAt     = e.CreatedAt,
    };
}
