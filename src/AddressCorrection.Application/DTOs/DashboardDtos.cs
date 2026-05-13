namespace AddressCorrection.src.AddressCorrection.Application.DTOs;

// ── Dashboard — activité journalière ─────────────────────────────────────────

/// <summary>
/// Volume de corrections pour un jour donné.
/// Utilisé par le graphe "Activité — 7 derniers jours" du dashboard.
/// </summary>
public sealed class DailyStatsDto
{
    /// <summary>Date du jour au format "yyyy-MM-dd".</summary>
    public string Date { get; init; } = string.Empty;

    /// <summary>Label court pour l'axe X du graphe (ex: "Lun", "Mar").</summary>
    public string DayLabel { get; init; } = string.Empty;

    /// <summary>Nombre total de requêtes ce jour.</summary>
    public int Total { get; init; }

    /// <summary>Nombre de corrections effectuées (adresse modifiée par le LLM).</summary>
    public int Corrected { get; init; }

    /// <summary>Nombre de réponses servies depuis le cache MongoDB.</summary>
    public int FromCache { get; init; }

    /// <summary>Nombre d'échecs (LLM ou référentiel indisponible).</summary>
    public int Failed { get; init; }
}

// ── Reports — activité mensuelle ──────────────────────────────────────────────

/// <summary>
/// Volume de corrections pour un mois donné.
/// Utilisé par le graphe "Évolution sur 6 mois" des rapports.
/// </summary>
public sealed class MonthlyStatsDto
{
    /// <summary>Année et mois au format "yyyy-MM".</summary>
    public string Month { get; init; } = string.Empty;

    /// <summary>Label court pour l'axe X (ex: "Jan", "Fév").</summary>
    public string MonthLabel { get; init; } = string.Empty;

    /// <summary>Total de requêtes ce mois.</summary>
    public int Total { get; init; }

    /// <summary>Corrections effectuées.</summary>
    public int Corrected { get; init; }

    /// <summary>Échecs.</summary>
    public int Failed { get; init; }
}

// ── Reports — répartition des types d'erreurs ─────────────────────────────────

/// <summary>
/// Proportion d'un type d'erreur d'adresse dans les corrections.
/// Utilisé par le PieChart "Répartition des erreurs".
/// </summary>
public sealed class ErrorTypeStatsDto
{
    /// <summary>Libellé du type d'erreur (ex: "Fautes ortho.", "CP manquant").</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Pourcentage arrondi (0–100).</summary>
    public int Value { get; init; }
}

// ── Reports — performance par pays ───────────────────────────────────────────
// ── Dashboard — corrections récentes ─────────────────────────────────────────

/// <summary>
/// Représentation d'une correction récente pour le tableau du dashboard.
/// Contient moins de champs que CorrectionRequestDto pour des raisons de performance.
/// </summary>
public sealed class RecentCorrectionDto
{
    /// <summary>Identifiant court affiché (ex: "cmd_9821").</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Adresse brute soumise.</summary>
    public string RawAddress { get; init; } = string.Empty;

    /// <summary>Adresse corrigée. Null si la correction a échoué.</summary>
    public string? CorrectedAddress { get; init; }

    /// <summary>Statut : "corrected" | "normalized" | "failed" | "cached".</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Heure de traitement au format "HH:mm".</summary>
    public string Time { get; init; } = string.Empty;

    /// <summary>Date/heure complète UTC pour le tri et l'affichage détaillé.</summary>
    public DateTime ProcessedAt { get; init; }

    /// <summary>Nom du modèle LLM utilisé. Null si réponse cache.</summary>
    public string? ModelUsed { get; init; }

    /// <summary>Vrai si la réponse a été servie depuis le cache.</summary>
    public bool FromCache { get; init; }
}

// ── Dashboard — résumé complet ────────────────────────────────────────────────

/// <summary>
/// Agrégat complet pour le dashboard client.
/// Un seul appel retourne toutes les données nécessaires à la page d'accueil.
/// Évite les waterfalls de requêtes côté frontend.
/// </summary>
public sealed class DashboardSummaryDto
{
    /// <summary>Compteurs globaux du mois en cours.</summary>
    public DashboardCountersDto Counters { get; init; } = new();

    /// <summary>Activité des 7 derniers jours pour le graphe.</summary>
    public IReadOnlyList<DailyStatsDto> DailyActivity { get; init; } = [];

    /// <summary>5 corrections les plus récentes.</summary>
    public IReadOnlyList<RecentCorrectionDto> RecentCorrections { get; init; } = [];

    /// <summary>Pourcentage du quota mensuel utilisé (0–100). Null si illimité.</summary>
    public double? QuotaUsedPercent { get; init; }

    /// <summary>Nombre de corrections restantes ce mois. Null si illimité.</summary>
    public int? RemainingQuota { get; init; }
}

/// <summary>Compteurs KPI du mois en cours pour les 4 stat-cards du dashboard.</summary>
public sealed class DashboardCountersDto
{
    public int TotalVerified { get; init; }
    public int TotalCorrected { get; init; }
    public double CorrectionRate { get; init; }

    /// <summary>Économies estimées en EUR (basé sur un coût moyen de retour colis).</summary>
    public decimal EstimatedSavings { get; init; }

    /// <summary>Variation % vs mois précédent pour TotalVerified.</summary>
    public double? TrendVerified { get; init; }

    /// <summary>Variation % vs mois précédent pour TotalCorrected.</summary>
    public double? TrendCorrected { get; init; }
}

// ── Reports — résumé complet ──────────────────────────────────────────────────

/// <summary>
/// Agrégat complet pour la page rapports.
/// Même principe que DashboardSummaryDto : un seul appel pour toute la page.
/// </summary>
public sealed class ReportsSummaryDto
{
    /// <summary>Compteurs du mois en cours (réutilise DashboardCountersDto).</summary>
    public DashboardCountersDto Counters { get; init; } = new();

    /// <summary>Évolution sur les N derniers mois (par défaut : 6).</summary>
    public IReadOnlyList<MonthlyStatsDto> MonthlyActivity { get; init; } = [];

    /// <summary>Répartition des types d'erreurs détectées.</summary>
    public IReadOnlyList<ErrorTypeStatsDto> ErrorTypes { get; init; } = [];

}