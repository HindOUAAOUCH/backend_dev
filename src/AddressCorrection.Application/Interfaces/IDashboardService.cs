using AddressCorrection.src.AddressCorrection.Application.DTOs;

namespace AddressCorrection.src.AddressCorrection.Application.Interfaces;

/// <summary>
/// Agrège les données analytiques depuis les repositories MongoDB
/// pour alimenter le dashboard client et la page rapports.
///
/// Toutes les méthodes acceptent un clientId optionnel :
///   - null  → données globales (vue admin)
///   - fourni → données filtrées pour ce client (vue client SaaS)
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Retourne l'agrégat complet du dashboard en un seul appel.
    /// Inclut compteurs, graphe 7 jours et 5 dernières corrections.
    /// </summary>
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(
        string? clientId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Retourne l'agrégat complet de la page rapports en un seul appel.
    /// Inclut graphe 6 mois, répartition erreurs et tableau par pays.
    /// </summary>
    Task<ReportsSummaryDto> GetReportsSummaryAsync(
        string? clientId = null,
        int months = 6,
        CancellationToken ct = default);

    /// <summary>
    /// Retourne l'activité jour par jour sur les N derniers jours.
    /// </summary>
    Task<IReadOnlyList<DailyStatsDto>> GetDailyStatsAsync(
        int days = 7,
        string? clientId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Retourne l'activité mois par mois sur les N derniers mois.
    /// </summary>
    Task<IReadOnlyList<MonthlyStatsDto>> GetMonthlyStatsAsync(
        int months = 6,
        string? clientId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Retourne les N corrections les plus récentes avec leurs détails d'affichage.
    /// </summary>
    Task<IReadOnlyList<RecentCorrectionDto>> GetRecentCorrectionsAsync(
        int count = 5,
        string? clientId = null,
        CancellationToken ct = default);
   
}