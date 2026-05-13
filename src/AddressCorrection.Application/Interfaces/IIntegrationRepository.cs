using AddressCorrection.src.AddressCorrection.Domain.Entities;
using AddressCorrection.src.AddressCorrection.Domain.Enums;

namespace AddressCorrection.src.AddressCorrection.Application.Interfaces;

/// <summary>
/// Contrat de persistance des intégrations.
/// Toutes les opérations sont asynchrones et acceptent un CancellationToken.
/// </summary>
public interface IIntegrationRepository
{
    /// <summary>Récupère une intégration par son identifiant. Retourne null si introuvable.</summary>
    Task<Integration?> GetByIdAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Retourne toutes les intégrations d'un client (hors statut Deleted par défaut).
    /// </summary>
    Task<IReadOnlyList<Integration>> GetByClientIdAsync(
        string clientId,
        bool includeDeleted = false,
        CancellationToken ct = default);

    /// <summary>Persiste une nouvelle intégration.</summary>
    Task CreateAsync(Integration integration, CancellationToken ct = default);

    /// <summary>Met à jour une intégration existante.</summary>
    Task UpdateAsync(Integration integration, CancellationToken ct = default);

    /// <summary>
    /// Soft-delete : passe le statut à Deleted sans supprimer le document MongoDB.
    /// </summary>
    Task SoftDeleteAsync(string id, DateTime deletedAt, CancellationToken ct = default);

    /// <summary>
    /// Vérifie qu'une intégration appartient bien au client spécifié.
    /// Utilisé pour les contrôles d'ownership avant modification.
    /// </summary>
    Task<bool> BelongsToClientAsync(string integrationId, string clientId, CancellationToken ct = default);

    /// <summary>Incrémente le compteur de requêtes et met à jour LastUsedAt.</summary>
    Task IncrementUsageAsync(string integrationId, DateTime usedAt, CancellationToken ct = default);
}