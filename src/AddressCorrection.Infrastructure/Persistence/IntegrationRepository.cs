using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using AddressCorrection.src.AddressCorrection.Domain.Entities;
using AddressCorrection.src.AddressCorrection.Domain.Enums;
using AddressCorrection.src.AddressCorrection.Infrastructure.Persistence.Documents;
using MongoDB.Driver;

namespace AddressCorrection.src.AddressCorrection.Infrastructure.Persistence;

public sealed class IntegrationRepository : IIntegrationRepository
{
    private readonly IMongoCollection<IntegrationDocument> _collection;

    public IntegrationRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<IntegrationDocument>("integrations");
    }

    public async Task<Integration?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var doc = await _collection
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync(ct);

        return doc is null ? null : ToEntity(doc);
    }

    public async Task<IReadOnlyList<Integration>> GetByClientIdAsync(
        string clientId,
        bool includeDeleted = false,
        CancellationToken ct = default)
    {
        var filter = includeDeleted
            ? Builders<IntegrationDocument>.Filter.Eq(x => x.ClientId, clientId)
            : Builders<IntegrationDocument>.Filter.And(
                Builders<IntegrationDocument>.Filter.Eq(x => x.ClientId, clientId),
                Builders<IntegrationDocument>.Filter.Ne(x => x.Status, "deleted"));

        var docs = await _collection.Find(filter).ToListAsync(ct);
        return docs.Select(ToEntity).ToList();
    }

    public async Task CreateAsync(Integration integration, CancellationToken ct = default)
    {
        var doc = ToDocument(integration);
        await _collection.InsertOneAsync(doc, cancellationToken: ct);
    }

    public async Task UpdateAsync(Integration integration, CancellationToken ct = default)
    {
        var doc = ToDocument(integration);
        await _collection.ReplaceOneAsync(x => x.Id == integration.Id, doc, cancellationToken: ct);
    }

    public async Task SoftDeleteAsync(string id, DateTime deletedAt, CancellationToken ct = default)
    {
        var update = Builders<IntegrationDocument>.Update
            .Set(x => x.Status, "deleted")
            .Set(x => x.UpdatedAt, deletedAt);

        await _collection.UpdateOneAsync(x => x.Id == id, update, cancellationToken: ct);
    }

    public async Task<bool> BelongsToClientAsync(string integrationId, string clientId, CancellationToken ct = default)
    {
        var count = await _collection
            .CountDocumentsAsync(
                x => x.Id == integrationId && x.ClientId == clientId,
                cancellationToken: ct);

        return count > 0;
    }

    public async Task IncrementUsageAsync(string integrationId, DateTime usedAt, CancellationToken ct = default)
    {
        var update = Builders<IntegrationDocument>.Update
            .Inc(x => x.TotalRequests, 1)
            .Set(x => x.LastUsedAt, usedAt);

        await _collection.UpdateOneAsync(x => x.Id == integrationId, update, cancellationToken: ct);
    }

    // ── Mappers privés ─────────────────────────────────────────────────────────

    private static Integration ToEntity(IntegrationDocument doc) => new()
    {
        Id            = doc.Id,
        ClientId      = doc.ClientId,
        Name          = doc.Name,
        Platform      = doc.Platform,
        WebhookUrl    = doc.WebhookUrl,
        Status        = Enum.TryParse<IntegrationStatus>(doc.Status, ignoreCase: true, out var s) ? s : IntegrationStatus.Active,
        TotalRequests = doc.TotalRequests,
        LastUsedAt    = doc.LastUsedAt,
        CreatedAt     = doc.CreatedAt,
        UpdatedAt     = doc.UpdatedAt,
    };

    private static IntegrationDocument ToDocument(Integration e) => new()
    {
        Id            = e.Id,
        ClientId      = e.ClientId,
        Name          = e.Name,
        Platform      = e.Platform,
        WebhookUrl    = e.WebhookUrl,
        Status        = e.Status.ToString().ToLowerInvariant(),
        TotalRequests = e.TotalRequests,
        LastUsedAt    = e.LastUsedAt,
        CreatedAt     = e.CreatedAt,
        UpdatedAt     = e.UpdatedAt,
    };
}
