using AddressCorrection.src.AddressCorrection.Domain.Entities;
using AddressCorrection.src.AddressCorrection.Infrastructure.Persistence.Documents;

namespace AddressCorrection.src.AddressCorrection.Infrastructure.Mappers;

/// <summary>
/// Mapper entre l'entité domain <see cref="AddressCorrectionRecord"/> et le document MongoDB
/// <see cref="AddressCorrectionDocument"/>. Centralise la conversion Domain ↔ Infrastructure.
/// </summary>
public static class AddressCorrectionDocumentMapper
{
    public static AddressCorrectionRecord ToEntity(AddressCorrectionDocument doc) =>
        new()
        {
            Id = doc.Id,
            RawAddress = doc.RawAddress,
            NormalizedAddress = doc.NormalizedAddress,
            HouseNumber = doc.HouseNumber,
            Street = doc.Street,
            Complement = doc.Complement,
            PostalCode = doc.PostalCode,
            City = doc.City,
            Country = doc.Country,
            Status = doc.Status,
            CorrectionNote = doc.CorrectionNote,
            ModelUsed = doc.ModelUsed,
            FromCache = doc.FromCache,
            ProcessedAt = doc.ProcessedAt,
            CreatedAt = doc.CreatedAt,
        };

    public static AddressCorrectionDocument ToDocument(AddressCorrectionRecord entity) =>
        new()
        {
            Id = entity.Id,
            RawAddress = entity.RawAddress,
            NormalizedAddress = entity.NormalizedAddress,
            HouseNumber = entity.HouseNumber,
            Street = entity.Street,
            Complement = entity.Complement,
            PostalCode = entity.PostalCode,
            City = entity.City,
            Country = entity.Country,
            Status = entity.Status,
            CorrectionNote = entity.CorrectionNote,
            ModelUsed = entity.ModelUsed,
            FromCache = entity.FromCache,
            ProcessedAt = entity.ProcessedAt,
            CreatedAt = entity.CreatedAt,
        };
}
