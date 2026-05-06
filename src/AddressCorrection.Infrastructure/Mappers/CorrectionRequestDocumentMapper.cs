using AddressCorrection.src.AddressCorrection.Domain.Entities;
using AddressCorrection.src.AddressCorrection.Infrastructure.Persistence.Documents;

namespace AddressCorrection.src.AddressCorrection.Infrastructure.Mappers;

/// <summary>
/// Mapper entre l'entité domain <see cref="CorrectionRequest"/> et le document MongoDB
/// <see cref="CorrectionRequestDocument"/>. Centralise la conversion Domain ↔ Infrastructure.
/// </summary>
public static class CorrectionRequestDocumentMapper
{
    public static CorrectionRequest ToEntity(CorrectionRequestDocument doc) =>
        new()
        {
            Id = doc.Id,
            RawAddress = doc.RawAddress,
            Source = doc.Source,
            FromCache = doc.FromCache,
            Status = doc.Status,
            ModelUsed = doc.ModelUsed,
            DurationMs = doc.DurationMs,
            SentAt = doc.SentAt,
            CorrectedAddress = doc.CorrectedAddress,
        };

    public static CorrectionRequestDocument ToDocument(CorrectionRequest entity) =>
        new()
        {
            Id = entity.Id,
            RawAddress = entity.RawAddress,
            Source = entity.Source,
            FromCache = entity.FromCache,
            Status = entity.Status,
            ModelUsed = entity.ModelUsed,
            DurationMs = entity.DurationMs,
            SentAt = entity.SentAt,
            CorrectedAddress = entity.CorrectedAddress,
        };
}
