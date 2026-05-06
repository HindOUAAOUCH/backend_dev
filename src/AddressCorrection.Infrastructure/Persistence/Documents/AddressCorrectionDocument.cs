using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AddressCorrection.src.AddressCorrection.Infrastructure.Persistence.Documents;

/// <summary>
/// Document MongoDB pour la collection "address_corrections" (cache d'adresses corrigées).
/// Contient toutes les annotations MongoDB — la couche Domain n'en a aucune.
/// </summary>
[BsonIgnoreExtraElements]
public sealed class AddressCorrectionDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string RawAddress { get; set; } = string.Empty;
    public string NormalizedAddress { get; set; } = string.Empty;
    public string? HouseNumber { get; set; }
    public string? Street { get; set; }
    public string? Complement { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Status { get; set; }
    public string? CorrectionNote { get; set; }
    public string ModelUsed { get; set; } = string.Empty;
    public bool FromCache { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; }
}
