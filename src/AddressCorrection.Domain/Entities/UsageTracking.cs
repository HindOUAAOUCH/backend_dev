using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AddressCorrection.src.AddressCorrection.Domain.Entities;

public sealed class UsageTracking
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