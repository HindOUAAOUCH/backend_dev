// ════════════════════════════════════════════════════════════════════════════
// PATCH : ICorrectionRequestRepository.cs
// Ajouter les paramètres dateFrom/dateTo à GetPagedAsync et le champ Country.
// ════════════════════════════════════════════════════════════════════════════
//
// Dans ICorrectionRequestRepository.cs, remplacer la signature de GetPagedAsync :
//
//   Task<PagedResult<CorrectionRequest>> GetPagedAsync(
//       int page,
//       int pageSize,
//       string? status = null,
//       string? search = null,
//       DateTime? dateFrom = null,   // ← AJOUTER
//       DateTime? dateTo   = null);  // ← AJOUTER
//
// Dans CorrectionRequest.cs (Domain/Entities), ajouter :
//
//   public string? Country { get; set; }          // ← pays détecté (code ISO)
//   public string? CorrectedAddress { get; set; } // ← adresse corrigée complète
//   public bool    FromCache { get; set; }         // ← existe déjà dans CorrectionRequest ?
//   public string? ModelUsed { get; set; }         // ← modèle LLM utilisé
//
// Ces champs doivent aussi être mappés dans CorrectionRequestDocumentMapper.cs
// et CorrectionRequestDocument.cs (ajouter les BsonElement correspondants).
//
// ════════════════════════════════════════════════════════════════════════════


using AddressCorrection.src.AddressCorrection.Application.DTOs;
using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using AddressCorrection.src.AddressCorrection.Application.Mappers;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;


namespace AddressCorrection.src.AddressCorrection.API.Controllers;

/// <summary>
/// Expose l'historique des corrections, les statistiques globales
/// et les données analytiques pour le dashboard et les rapports.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class CorrectionsController : ControllerBase
{
    private readonly ICorrectionRequestRepository _repository;
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<CorrectionsController> _logger;

    public CorrectionsController(
        ICorrectionRequestRepository repository,
        IDashboardService dashboardService,
        ILogger<CorrectionsController> logger)
    {
        _repository = repository;
        _dashboardService = dashboardService;
        _logger = logger;
    }

    // ── GET /api/corrections ──────────────────────────────────────────────────
    /// <summary>
    /// Retourne l'historique paginé des corrections avec filtres.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedCorrectionRequestDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedCorrectionRequestDto>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery][Range(1, 100)] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        CancellationToken ct = default)
    {
        try
        {
            var paged = await _repository.GetPagedAsync(page, pageSize, status, search, dateFrom, dateTo);
            return Ok(CorrectionRequestMapper.ToPagedDto(paged));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching corrections history");
            return StatusCode(500, Problem(detail: "An unexpected error occurred.", statusCode: 500));
        }
    }

    // ── GET /api/corrections/stats ────────────────────────────────────────────
    /// <summary>
    /// Compteurs globaux : total, succès, échecs, cache, aujourd'hui.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(CorrectionStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CorrectionStatsDto>> GetStats(CancellationToken ct)
    {
        try
        {
            var stats = await _repository.GetStatsAsync();
            return Ok(new CorrectionStatsDto
            {
                TotalRequests = stats.TotalRequests,
                SuccessCount = stats.SuccessCount,
                FailedCount = stats.FailedCount,
                CacheHits = stats.CacheHits,
                TodayRequests = stats.TodayRequests,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching corrections stats");
            return StatusCode(500, Problem(detail: "An unexpected error occurred.", statusCode: 500));
        }
    }

    // ── GET /api/corrections/dashboard ───────────────────────────────────────
    /// <summary>
    /// Agrégat complet pour le dashboard client en un seul appel.
    /// Retourne : compteurs KPI + graphe 7 jours + 5 corrections récentes.
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryDto>> GetDashboard(CancellationToken ct)
    {
        // En Phase 2 : clientId depuis HttpContext.Items (ApiKey middleware)
        // En Phase 3 : clientId depuis le claim JWT
        // En Phase 1 : null = données globales
        var clientId = HttpContext.Items["ClientId"] as string;

        try
        {
            var summary = await _dashboardService.GetDashboardSummaryAsync(clientId, ct);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching dashboard summary");
            return StatusCode(500, Problem(detail: "An unexpected error occurred.", statusCode: 500));
        }
    }

    // ── GET /api/corrections/reports ──────────────────────────────────────────
    /// <summary>
    /// Agrégat complet pour la page rapports en un seul appel.
    /// Retourne : compteurs + graphe 6 mois + répartition erreurs + tableau pays.
    /// </summary>
    [HttpGet("reports")]
    [ProducesResponseType(typeof(ReportsSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReportsSummaryDto>> GetReports(
        [FromQuery][Range(1, 24)] int months = 6,
        CancellationToken ct = default)
    {
        var clientId = HttpContext.Items["ClientId"] as string;

        try
        {
            var summary = await _dashboardService.GetReportsSummaryAsync(clientId, months, ct);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching reports summary");
            return StatusCode(500, Problem(detail: "An unexpected error occurred.", statusCode: 500));
        }
    }

    // ── GET /api/corrections/stats/daily ─────────────────────────────────────
    /// <summary>
    /// Activité journalière sur les N derniers jours (défaut : 7).
    /// Données pour le graphe AreaChart du dashboard.
    /// </summary>
    [HttpGet("stats/daily")]
    [ProducesResponseType(typeof(IReadOnlyList<DailyStatsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DailyStatsDto>>> GetDailyStats(
        [FromQuery][Range(1, 90)] int days = 7,
        CancellationToken ct = default)
    {
        var clientId = HttpContext.Items["ClientId"] as string;
        try
        {
            var stats = await _dashboardService.GetDailyStatsAsync(days, clientId, ct);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching daily stats");
            return StatusCode(500, Problem(detail: "An unexpected error occurred.", statusCode: 500));
        }
    }

    // ── GET /api/corrections/stats/monthly ───────────────────────────────────
    /// <summary>
    /// Activité mensuelle sur les N derniers mois (défaut : 6).
    /// Données pour le graphe AreaChart des rapports.
    /// </summary>
    [HttpGet("stats/monthly")]
    [ProducesResponseType(typeof(IReadOnlyList<MonthlyStatsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MonthlyStatsDto>>> GetMonthlyStats(
        [FromQuery][Range(1, 24)] int months = 6,
        CancellationToken ct = default)
    {
        var clientId = HttpContext.Items["ClientId"] as string;
        try
        {
            var stats = await _dashboardService.GetMonthlyStatsAsync(months, clientId, ct);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching monthly stats");
            return StatusCode(500, Problem(detail: "An unexpected error occurred.", statusCode: 500));
        }
    }


    // ── GET /api/corrections/recent ───────────────────────────────────────────
    /// <summary>
    /// Retourne les N corrections les plus récentes avec leurs détails d'affichage.
    /// Données pour le tableau "Corrections récentes" du dashboard.
    /// </summary>
    [HttpGet("recent")]
    [ProducesResponseType(typeof(IReadOnlyList<RecentCorrectionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RecentCorrectionDto>>> GetRecent(
        [FromQuery][Range(1, 50)] int count = 5,
        CancellationToken ct = default)
    {
        var clientId = HttpContext.Items["ClientId"] as string;
        try
        {
            var recent = await _dashboardService.GetRecentCorrectionsAsync(count, clientId, ct);
            return Ok(recent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching recent corrections");
            return StatusCode(500, Problem(detail: "An unexpected error occurred.", statusCode: 500));
        }
    }
}