using AddressCorrection.src.AddressCorrection.Application.DTOs;
using AddressCorrection.src.AddressCorrection.Domain.Entities;

namespace AddressCorrection.src.AddressCorrection.Application.Mappers;

/// <summary>
/// Mapper entre les DTOs et les entités domain.
/// Centralise toutes les conversions — jamais de mapping dans les services ou controllers.
/// </summary>
public static class AddressMapper
{
    /// <summary>
    /// Convertit un AddressResponse (DTO LLM) en AddressCorrectionRecord (entité cache).
    /// </summary>
    public static AddressCorrectionRecord ToRecord(
        AddressRequest request,
        AddressResponse response,
        string normalizedAddress,
        string modelUsed) =>
        new()
        {
            NormalizedAddress = normalizedAddress,
            HouseNumber = response.HouseNumber,
            Street = response.Street,
            Complement = response.Complement,
            PostalCode = response.PostalCode,
            City = response.City,
            Country = response.Country,
            Status = response.Status,
            CorrectionNote = response.CorrectionNote,
            ModelUsed = modelUsed,
            CreatedAt = DateTime.UtcNow,
        };

    /// <summary>
    /// Convertit un AddressCorrectionRecord (cache) en AddressResponse (DTO API).
    /// </summary>
    public static AddressResponse ToResponse(AddressCorrectionRecord record) =>
        new()
        {
            HouseNumber = record.HouseNumber,
            Street = record.Street,
            Complement = record.Complement,
            PostalCode = record.PostalCode,
            City = record.City,
            Country = record.Country,
            Status = record.Status,
            CorrectionNote = record.CorrectionNote,
        };

    /// <summary>
    /// Formate un AddressResponse en une seule ligne lisible.
    /// Ex : "14 Rue de la Paix, 75002, Paris, France"
    /// Utilisé pour renseigner CorrectionRequest.CorrectedAddress.
    /// </summary>
    public static string? ToFormattedLine(AddressResponse response)
    {
        var parts = new[]
        {
            response.HouseNumber,
            response.Street,
            response.Complement,
            response.PostalCode,
            response.City,
            response.Country,
        }
        .Where(p => !string.IsNullOrWhiteSpace(p))
        .ToArray();

        return parts.Length > 0 ? string.Join(", ", parts) : null;
    }
}