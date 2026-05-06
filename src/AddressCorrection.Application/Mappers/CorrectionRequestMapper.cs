using AddressCorrection.src.AddressCorrection.Application.DTOs;
using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using AddressCorrection.src.AddressCorrection.Domain.Entities;

namespace AddressCorrection.src.AddressCorrection.Application.Mappers;

/// <summary>
/// Mapper entre l'entité domain <see cref="CorrectionRequest"/> et les DTOs API.
/// Centralise la conversion — jamais de mapping dans les controllers ou services.
/// </summary>
public static class CorrectionRequestMapper
{
    /// <summary>
    /// Convertit une entité <see cref="CorrectionRequest"/> en DTO <see cref="CorrectionRequestDto"/>.
    /// </summary>
    public static CorrectionRequestDto ToDto(CorrectionRequest entity) =>
        new()
        {
            // Id is nullable on the domain entity (assigned by MongoDB on insert).
            // The DTO exposes a non-nullable string; an empty string indicates a missing ID.
            Id = entity.Id ?? string.Empty,
            RawAddress = entity.RawAddress,
            CorrectedAddress = entity.CorrectedAddress,
            FromCache = entity.FromCache,
            ModelUsed = entity.ModelUsed,
            Status = entity.Status,
            DurationMs = entity.DurationMs,
            Source = entity.Source,
            SentAt = entity.SentAt,
        };

    /// <summary>
    /// Convertit un résultat paginé d'entités en DTO paginé.
    /// </summary>
    public static PagedCorrectionRequestDto ToPagedDto(PagedResult<CorrectionRequest> paged) =>
        new()
        {
            Items = paged.Items
                .Select(ToDto)
                .ToList()
                .AsReadOnly(),
            Total = paged.Total,
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalPages = paged.TotalPages,
            HasNext = paged.HasNext,
            HasPrev = paged.HasPrev,
        };
}
