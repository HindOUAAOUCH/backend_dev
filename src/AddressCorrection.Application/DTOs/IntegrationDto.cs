using System.ComponentModel.DataAnnotations;

namespace AddressCorrection.src.AddressCorrection.Application.DTOs;

// ── Requêtes entrantes ────────────────────────────────────────────────────────

/// <summary>Corps de la requête POST /api/integrations.</summary>
public sealed class CreateIntegrationRequest
{
    /// <summary>Nom lisible de l'intégration (ex: "Boutique principale").</summary>
    [Required]
    [MinLength(2)]
    [MaxLength(100)]
    public string Name { get; init; } = string.Empty;

    /// <summary>Plateforme cible. Valeurs : shopify | woocommerce | magento | custom.</summary>
    [Required]
    public string Platform { get; init; } = string.Empty;

    /// <summary>URL de webhook (optionnel). Doit être HTTPS si renseignée.</summary>
    [Url]
    public string? WebhookUrl { get; init; }
}

/// <summary>Corps de la requête PUT /api/integrations/{id}.</summary>
public sealed class UpdateIntegrationRequest
{
    /// <summary>Nouveau nom lisible. Null = inchangé.</summary>
    [MinLength(2)]
    [MaxLength(100)]
    public string? Name { get; init; }

    /// <summary>Nouvelle URL de webhook. Null = inchangée. Chaîne vide = suppression.</summary>
    [Url]
    public string? WebhookUrl { get; init; }
}

// ── Réponses sortantes ────────────────────────────────────────────────────────

/// <summary>Représentation publique d'une intégration.</summary>
public sealed class IntegrationDto
{
    public string Id { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Platform { get; init; } = string.Empty;
    public string? WebhookUrl { get; init; }

    /// <summary>Statut : "active" | "paused" | "deleted".</summary>
    public string Status { get; init; } = string.Empty;

    public long TotalRequests { get; init; }
    public DateTime? LastUsedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}