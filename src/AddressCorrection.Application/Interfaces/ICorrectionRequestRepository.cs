using AddressCorrection.src.AddressCorrection.Domain.Entities;

namespace AddressCorrection.src.AddressCorrection.Application.Interfaces;

/// <summary>
/// Contrat du repository pour les traces de requêtes de correction.
/// </summary>
public interface ICorrectionRequestRepository
{
    /// <summary>Persiste une nouvelle trace de requête.</summary>
    Task SaveAsync(CorrectionRequest request);

    /// <summary>Retourne les requêtes du jour courant (UTC), triées par date décroissante.</summary>
    Task<List<CorrectionRequest>> GetTodayRequestsAsync();

    /// <summary>
    /// Retourne une page de requêtes, avec filtres optionnels.
    /// </summary>
    /// <param name="page">Numéro de page (base 1).</param>
    /// <param name="pageSize">Nombre d'éléments par page (max 100).</param>
    /// <param name="status">Filtre sur le statut : "success" | "failed" | null (tous).</param>
    /// <param name="search">Recherche sur l'adresse brute (insensible à la casse).</param>
    Task<PagedResult<CorrectionRequest>> GetPagedAsync(
        int page,
        int pageSize,
        string? status = null,
        string? search = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null);

    /// <summary>Retourne les compteurs globaux pour le tableau de bord.</summary>
    Task<CorrectionStats> GetStatsAsync();
}

/// <summary>Résultat paginé générique.</summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Total,
    int Page,
    int PageSize
)
{
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrev => Page > 1;
}

/// <summary>Statistiques globales des corrections.</summary>
public sealed record CorrectionStats(
    long TotalRequests,
    long SuccessCount,
    long FailedCount,
    long CacheHits,
    long TodayRequests
);