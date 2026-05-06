using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AddressCorrection.src.AddressCorrection.Domain.Entities;

public sealed class CorrectionRequest
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string RawAddress { get; set; } = string.Empty;
    public string Source { get; set; } = "API";
    public bool FromCache { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ModelUsed { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public string? CorrectedAddress { get; set; }
}