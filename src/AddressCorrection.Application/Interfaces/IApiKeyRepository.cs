using AddressCorrection.src.AddressCorrection.Domain.Entities;

namespace AddressCorrection.src.AddressCorrection.Application.Interfaces;

/// <summary>
/// Contrat de persistance des clés API.
/// </summary>
public interface IApiKeyRepository
{
    /// <summary>Récupère une clé API par son identifiant. Retourne null si introuvable.</summary>
    Task<ApiKey?> GetByIdAsync(string id, CancellationToken ct = default);

    /// <summary>Retourne toutes les clés d'une intégration (actives et révoquées).</summary>
    Task<IReadOnlyList<ApiKey>> GetByIntegrationIdAsync(string integrationId, CancellationToken ct = default);

    /// <summary>
    /// Récupère une clé avec son hash par son préfixe, pour la validation.
    /// Retourne null si introuvable.
    /// </summary>
    Task<ApiKey?> GetWithHashByPrefixAsync(string prefix, CancellationToken ct = default);

    /// <summary>Compte les clés actives (non révoquées, non expirées) d'une intégration.</summary>
    Task<int> CountActiveAsync(string integrationId, DateTime now, CancellationToken ct = default);

    /// <summary>Persiste une nouvelle clé API.</summary>
    Task CreateAsync(ApiKey apiKey, CancellationToken ct = default);

    /// <summary>Révoque une clé API (RevokedAt = now, IsRevoked = true).</summary>
    Task RevokeAsync(string id, DateTime revokedAt, CancellationToken ct = default);

    /// <summary>Met à jour LastUsedAt et le compteur d'utilisation.</summary>
    Task RecordUsageAsync(string id, DateTime usedAt, CancellationToken ct = default);
}