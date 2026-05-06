namespace AddressCorrection.src.AddressCorrection.Domain.Entities;

/// <summary>
/// Entité domain représentant le suivi d'utilisation journalier.
/// Aucune dépendance sur l'infrastructure (pas d'annotations MongoDB).
/// </summary>
public sealed class UsageTracking
{
    public string? Id { get; set; }

    public DateTime Date { get; set; }
    public int RequestCount { get; set; }
    public int CacheHitCount { get; set; }
    public int LlmCallCount { get; set; }
    public bool LimitReached { get; set; }
}