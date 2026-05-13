using AddressCorrection.src.AddressCorrection.Application.DTOs;

namespace AddressCorrection.src.AddressCorrection.Application.Interfaces;

/// <summary>
/// Cas d'utilisation liés à la gestion et à la validation des clés API.
/// </summary>
public interface IApiKeyService
{
    /// <summary>
    /// Génère une nouvelle clé API pour l'intégration spécifiée.
    /// La clé en clair est retournée UNE SEULE FOIS dans ApiKeyCreatedDto.
    /// Elle n'est jamais stockée ni récupérable par la suite.
    /// </summary>
    /// <exception cref="Exceptions.IntegrationNotFoundException">Si l'intégration est introuvable.</exception>
    /// <exception cref="Exceptions.UnauthorizedIntegrationAccessException">Si l'intégration n'appartient pas au client.</exception>
    /// <exception cref="Exceptions.ApiKeyLimitExceededException">Si la limite de clés actives est atteinte.</exception>
    Task<ApiKeyCreatedDto> GenerateAsync(
        string integrationId,
        string clientId,
        GenerateApiKeyRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Retourne la liste des clés d'une intégration (sans les hash ni les clés en clair).
    /// Seuls le préfixe, le nom et les métadonnées sont exposés.
    /// </summary>
    Task<IReadOnlyList<ApiKeyDto>> ListAsync(
        string integrationId,
        string clientId,
        CancellationToken ct = default);

    /// <summary>
    /// Révoque définitivement une clé API.
    /// Une clé révoquée ne peut pas être réactivée.
    /// </summary>
    /// <exception cref="Exceptions.ApiKeyNotFoundException">Si la clé est introuvable.</exception>
    /// <exception cref="Exceptions.UnauthorizedIntegrationAccessException">Si la clé n'appartient pas à l'intégration du client.</exception>
    Task RevokeAsync(
        string keyId,
        string integrationId,
        string clientId,
        CancellationToken ct = default);

    /// <summary>
    /// Valide une clé API brute reçue dans un header HTTP.
    /// Retourne le contexte d'authentification si la clé est valide et active.
    /// Retourne null si la clé est invalide, révoquée ou expirée.
    /// Cette méthode est utilisée par le middleware d'authentification uniquement.
    /// </summary>
    Task<ApiKeyValidationResult?> ValidateAsync(string rawApiKey, CancellationToken ct = default);
}