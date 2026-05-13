using AddressCorrection.src.AddressCorrection.Application.DTOs;
using AddressCorrection.src.AddressCorrection.Domain.Entities;

namespace AddressCorrection.src.AddressCorrection.Application.Mappers;

/// <summary>
/// Transformations Integration (Domain) ↔ IntegrationDto (Application/DTO).
/// Classe statique pure — aucun effet de bord.
/// </summary>
public static class IntegrationMapper
{
    public static IntegrationDto ToDto(Integration integration) => new()
    {
        Id = integration.Id,
        ClientId = integration.ClientId,
        Name = integration.Name,
        Platform = integration.Platform,
        WebhookUrl = integration.WebhookUrl,
        Status = integration.Status.ToString().ToLowerInvariant(),
        TotalRequests = integration.TotalRequests,
        LastUsedAt = integration.LastUsedAt,
        CreatedAt = integration.CreatedAt,
        UpdatedAt = integration.UpdatedAt,
    };
}

/// <summary>
/// Transformations ApiKey (Domain) ↔ ApiKeyDto (Application/DTO).
/// Ne mappe JAMAIS HashedKey ni Salt — ces champs restent dans l'Infrastructure.
/// </summary>
public static class ApiKeyMapper
{
    public static ApiKeyDto ToDto(ApiKey key) => new()
    {
        Id = key.Id,
        IntegrationId = key.IntegrationId,
        Name = key.Name,
        Prefix = key.Prefix,
        Scopes = key.Scopes,
        IsRevoked = key.IsRevoked,
        ExpiresAt = key.ExpiresAt,
        LastUsedAt = key.LastUsedAt,
        UsageCount = key.UsageCount,
        CreatedAt = key.CreatedAt,
    };
}