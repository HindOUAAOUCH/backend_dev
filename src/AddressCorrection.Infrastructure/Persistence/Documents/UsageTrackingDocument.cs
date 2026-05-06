using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AddressCorrection.src.AddressCorrection.Infrastructure.Persistence.Documents;

/// <summary>
/// Document MongoDB pour la collection "usage_tracking".
/// Contient toutes les annotations MongoDB — la couche Domain n'en a aucune.
/// </summary>
[BsonIgnoreExtraElements]
public sealed class UsageTrackingDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public DateTime Date { get; set; }
    public int RequestCount { get; set; }
    public int CacheHitCount { get; set; }
    public int LlmCallCount { get; set; }
    public bool LimitReached { get; set; }
}
