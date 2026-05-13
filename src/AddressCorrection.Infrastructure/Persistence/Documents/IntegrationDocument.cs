using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AddressCorrection.src.AddressCorrection.Infrastructure.Persistence.Documents;

/// <summary>
/// Représentation MongoDB d'une intégration.
/// Séparée de l'entité Domain — aucune annotation Mongo ne doit apparaître dans le Domain.
/// </summary>
public sealed class IntegrationDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("clientId")]
    public string ClientId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("platform")]
    public string Platform { get; set; } = string.Empty;

    [BsonElement("webhookUrl")]
    public string? WebhookUrl { get; set; }

    /// <summary>Stocké en string pour la lisibilité dans MongoDB ("active" | "paused" | "deleted").</summary>
    [BsonElement("status")]
    public string Status { get; set; } = "active";

    [BsonElement("totalRequests")]
    public long TotalRequests { get; set; }

    [BsonElement("lastUsedAt")]
    public DateTime? LastUsedAt { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}