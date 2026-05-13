using System.ComponentModel.DataAnnotations;

namespace AddressCorrection.src.AddressCorrection.Application.DTOs;

// ── Requêtes entrantes ────────────────────────────────────────────────────────

/// <summary>Corps de la requête POST /api/integrations/{id}/keys.</summary>
public sealed class GenerateApiKeyRequest
{
    /// <summary>Nom lisible pour identifier la clé (ex: "Clé production", "Clé CI").</summary>
    [Required]
    [MinLength(2)]
    [MaxLength(80)]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Scopes autorisés pour cette clé.
    /// Valeurs valides : "correct" | "read" | "full_access".
    /// Si vide, "full_access" est appliqué par défaut.
    /// </summary>
    public IReadOnlyList<string> Scopes { get; init; } = [];

    /// <summary>
    /// Date d'expiration UTC. Null = pas d'expiration.
    /// Doit être dans le futur si renseignée.
    /// </summary>
    public DateTime? ExpiresAt { get; init; }
}

// ── Réponses sortantes ────────────────────────────────────────────────────────

/// <summary>
/// Retourné UNIQUEMENT lors de la création d'une clé API.
/// Contient la clé en clair — elle ne sera plus jamais accessible ensuite.
/// Le client doit la copier immédiatement.
/// </summary>
public sealed class ApiKeyCreatedDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Clé API en clair, à copier immédiatement.
    /// Format : api_key_xxxxxxxxx
    /// </summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>Préfixe conservé pour les affichages futurs (non-secret).</summary>
    public string Prefix { get; init; } = string.Empty;

    public IReadOnlyList<string> Scopes { get; init; } = [];
    public DateTime? ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Représentation d'une clé API dans les listes.
/// Ne contient jamais la clé en clair ni son hash.
/// </summary>
public sealed class ApiKeyDto
{
    public string Id { get; init; } = string.Empty;
    public string IntegrationId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;

    /// <summary>Préfixe non-secret (ex: "sk_live_aB") pour identification visuelle.</summary>
    public string Prefix { get; init; } = string.Empty;

    public IReadOnlyList<string> Scopes { get; init; } = [];
    public bool IsRevoked { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? LastUsedAt { get; init; }
    public long UsageCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Contexte d'authentification retourné après validation d'une clé API.
/// Injecté dans HttpContext.Items par le middleware.
/// </summary>
public sealed class ApiKeyValidationResult
{
    public string KeyId { get; init; } = string.Empty;
    public string IntegrationId { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public IReadOnlyList<string> Scopes { get; init; } = [];
}

/// <summary>
/// Payload envoyé dans un webhook après une correction d'adresse réussie.
/// </summary>
public sealed class WebhookPayload
{
    /// <summary>Type d'événement (ex: "address.corrected").</summary>
    public string EventType { get; init; } = "address.corrected";

    /// <summary>Horodatage UTC de l'événement.</summary>
    public DateTime OccurredAt { get; init; }

    /// <summary>Identifiant de la correction dans notre système.</summary>
    public string CorrectionId { get; init; } = string.Empty;

    /// <summary>Adresse brute soumise.</summary>
    public string RawAddress { get; init; } = string.Empty;

    /// <summary>Résultat de la correction (les mêmes champs qu'AddressResponse).</summary>
    public object? CorrectedAddress { get; init; }

    /// <summary>Indique si la réponse provient du cache.</summary>
    public bool FromCache { get; init; }
}