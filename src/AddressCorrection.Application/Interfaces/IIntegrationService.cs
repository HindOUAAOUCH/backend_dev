using AddressCorrection.src.AddressCorrection.Application.DTOs;

namespace AddressCorrection.src.AddressCorrection.Application.Interfaces;

/// <summary>
/// Contrat des cas d'utilisation de gestion des intégrations e-commerce.
/// Les controllers délèguent toute la logique métier à cette interface.
/// </summary>
public interface IIntegrationService
{
    /// <summary>Crée une nouvelle intégration pour le client spécifié.</summary>
    Task<IntegrationDto> CreateAsync(
        string clientId,
        CreateIntegrationRequest request,
        CancellationToken ct = default);

    /// <summary>Retourne toutes les intégrations actives d'un client.</summary>
    Task<IReadOnlyList<IntegrationDto>> GetByClientAsync(
        string clientId,
        CancellationToken ct = default);

    /// <summary>
    /// Retourne le détail d'une intégration.
    /// Lève <see cref="IntegrationNotFoundException"/> si introuvable.
    /// Lève <see cref="UnauthorizedIntegrationAccessException"/> si le client n'est pas propriétaire.
    /// </summary>
    Task<IntegrationDto> GetByIdAsync(
        string integrationId,
        string clientId,
        CancellationToken ct = default);

    /// <summary>Met à jour le nom ou l'URL webhook d'une intégration.</summary>
    Task<IntegrationDto> UpdateAsync(
        string integrationId,
        string clientId,
        UpdateIntegrationRequest request,
        CancellationToken ct = default);

    /// <summary>Suspend temporairement une intégration (statut → Paused).</summary>
    Task PauseAsync(
        string integrationId,
        string clientId,
        CancellationToken ct = default);

    /// <summary>Réactive une intégration suspendue (statut → Active).</summary>
    Task ResumeAsync(
        string integrationId,
        string clientId,
        CancellationToken ct = default);

    /// <summary>
    /// Soft-delete de l'intégration et révocation de toutes ses clés API actives.
    /// </summary>
    Task DeleteAsync(
        string integrationId,
        string clientId,
        CancellationToken ct = default);
}