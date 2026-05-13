namespace AddressCorrection.src.AddressCorrection.Domain.Entities;

/// <summary>
/// Représente une clé API associée à une intégration.
/// La clé brute n'est JAMAIS stockée — seul son hash PBKDF2 est persisté.
/// Seul le préfixe (8 premiers caractères) est conservé pour l'affichage.
/// </summary>
public sealed class ApiKey
{
    public string Id { get; init; } = string.Empty;

    /// <summary>Identifiant de l'intégration propriétaire.</summary>
    public string IntegrationId { get; init; } = string.Empty;

    /// <summary>Identifiant du client (dénormalisé pour les lookups d'auth).</summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// Nom lisible donné par le client (ex: "Clé production", "Clé staging").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Préfixe non-secret de la clé, conservé pour l'affichage (ex: "sk_live_aB").
    /// Correspond aux 10 premiers caractères de la clé en clair.
    /// Ne permet pas de reconstruire la clé.
    /// </summary>
    public string Prefix { get; init; } = string.Empty;

    /// <summary>
    /// Hash PBKDF2 de la clé complète. Seule valeur persistée pour la validation.
    /// Jamais exposé dans les DTOs ou les logs.
    /// </summary>
    public string HashedKey { get; init; } = string.Empty;

    /// <summary>Sel utilisé lors du hachage (stocké séparément du hash).</summary>
    public string Salt { get; init; } = string.Empty;

    /// <summary>Date d'expiration. Null = pas d'expiration.</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Date de création (UTC).</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>Date du dernier usage authentifié. Null si jamais utilisée.</summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>Nombre total d'authentifications réussies avec cette clé.</summary>
    public long UsageCount { get; set; }

    /// <summary>Indique si la clé a été révoquée manuellement.</summary>
    public bool IsRevoked { get; set; }

    /// <summary>Scopes autorisés pour cette clé (ex: ["correct", "read"]).</summary>
    public IReadOnlyList<string> Scopes { get; init; } = [];

    /// <summary>
    /// Indique si la clé est actuellement utilisable (non révoquée et non expirée).
    /// </summary>
    public bool IsActive(DateTime utcNow) =>
        !IsRevoked && (ExpiresAt is null || ExpiresAt > utcNow);
}