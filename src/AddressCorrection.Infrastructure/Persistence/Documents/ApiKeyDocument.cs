using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AddressCorrection.src.AddressCorrection.Infrastructure.Persistence.Documents;

/// <summary>
/// Représentation MongoDB d'une clé API.
/// Contient HashedKey et Salt — ces champs ne sortent JAMAIS de l'Infrastructure.
/// </summary>
public sealed class ApiKeyDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("integrationId")]
    public string IntegrationId { get; set; } = string.Empty;

    [BsonElement("clientId")]
    public string ClientId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("prefix")]
    public string Prefix { get; set; } = string.Empty;

    /// <summary>Hash PBKDF2 de la clé. Jamais exposé hors de l'Infrastructure.</summary>
    [BsonElement("hashedKey")]
    public string HashedKey { get; set; } = string.Empty;

    /// <summary>Sel PBKDF2. Jamais exposé hors de l'Infrastructure.</summary>
    [BsonElement("salt")]
    public string Salt { get; set; } = string.Empty;

    [BsonElement("scopes")]
    public List<string> Scopes { get; set; } = [];

    [BsonElement("isRevoked")]
    public bool IsRevoked { get; set; }

    [BsonElement("expiresAt")]
    public DateTime? ExpiresAt { get; set; }

    [BsonElement("lastUsedAt")]
    public DateTime? LastUsedAt { get; set; }

    [BsonElement("usageCount")]
    public long UsageCount { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }
}