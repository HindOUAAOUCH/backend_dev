using AddressCorrection.src.AddressCorrection.Domain.Enums;

namespace AddressCorrection.src.AddressCorrection.Domain.Entities;

/// <summary>
/// Représente une intégration e-commerce d'un client avec la plateforme.
/// Une intégration est le point d'entrée technique d'un système tiers.
/// Chaque intégration possède ses propres clés API et son propre webhook.
/// </summary>
public sealed class Integration
{
    public string Id { get; init; } = string.Empty;

    /// <summary>Identifiant du client propriétaire de cette intégration.</summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>Nom lisible de l'intégration (ex: "Boutique Shopify principale").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Plateforme cible (ex: Shopify, WooCommerce, Magento, Custom).</summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>URL de callback appelée après chaque correction réussie. Nullable si non configurée.</summary>
    public string? WebhookUrl { get; set; }

    /// <summary>Statut courant de l'intégration.</summary>
    public IntegrationStatus Status { get; set; } = IntegrationStatus.Active;

    /// <summary>Date de création de l'intégration (UTC).</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>Date de dernière modification (UTC).</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Nombre total de requêtes envoyées via cette intégration.</summary>
    public long TotalRequests { get; set; }

    /// <summary>Date du dernier appel reçu via cette intégration. Null si jamais utilisée.</summary>
    public DateTime? LastUsedAt { get; set; }
}