using AddressCorrection.src.AddressCorrection.Domain.Entities;
using AddressCorrection.src.AddressCorrection.Infrastructure.Persistence.Documents;

namespace AddressCorrection.src.AddressCorrection.Infrastructure.Mappers;

/// <summary>
/// Mapper entre l'entité domain <see cref="UsageTracking"/> et le document MongoDB
/// <see cref="UsageTrackingDocument"/>. Centralise la conversion Domain ↔ Infrastructure.
/// </summary>
public static class UsageTrackingDocumentMapper
{
    public static UsageTracking ToEntity(UsageTrackingDocument doc) =>
        new()
        {
            Id = doc.Id,
            Date = doc.Date,
            RequestCount = doc.RequestCount,
            CacheHitCount = doc.CacheHitCount,
            LlmCallCount = doc.LlmCallCount,
            LimitReached = doc.LimitReached,
        };

    public static UsageTrackingDocument ToDocument(UsageTracking entity) =>
        new()
        {
            Id = entity.Id,
            Date = entity.Date,
            RequestCount = entity.RequestCount,
            CacheHitCount = entity.CacheHitCount,
            LlmCallCount = entity.LlmCallCount,
            LimitReached = entity.LimitReached,
        };
}
