namespace AddressCorrection.src.AddressCorrection.Application.DTOs;

/// <summary>
/// DTO retourné par GET /api/corrections.
/// Miroir sérialisable de l'entité CorrectionRequest — ne jamais exposer l'entité domain directement.
/// </summary>
public sealed class CorrectionRequestDto
{
    /// <summary>Identifiant MongoDB (ObjectId en string).</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Adresse brute soumise.</summary>
    public string RawAddress { get; init; } = string.Empty;

    /// <summary>Adresse corrigée sur une ligne. Null si échec.</summary>
    public string? CorrectedAddress { get; init; }

    /// <summary>true si le résultat provient du cache MongoDB.</summary>
    public bool FromCache { get; init; }

    /// <summary>Modèle LLM utilisé : "gpt-4o-mini", "Ministral-3B", etc.</summary>
    public string ModelUsed { get; init; } = string.Empty;

    /// <summary>"success" ou "failed".</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Durée totale du pipeline en millisecondes.</summary>
    public long DurationMs { get; init; }

    /// <summary>Source de l'appel : "API" ou "Playground".</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>Date/heure UTC de réception, format ISO 8601.</summary>
    public DateTime SentAt { get; init; }
}

/// <summary>
/// DTO de réponse paginée pour GET /api/corrections.
/// </summary>
public sealed class PagedCorrectionRequestDto
{
    public IReadOnlyList<CorrectionRequestDto> Items { get; init; } = [];
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public bool HasNext { get; init; }
    public bool HasPrev { get; init; }
}

/// <summary>
/// DTO de statistiques globales pour GET /api/corrections/stats.
/// </summary>
public sealed class CorrectionStatsDto
{
    public long TotalRequests { get; init; }
    public long SuccessCount { get; init; }
    public long FailedCount { get; init; }
    public long CacheHits { get; init; }
    public long TodayRequests { get; init; }
}