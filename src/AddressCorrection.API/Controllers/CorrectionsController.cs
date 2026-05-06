using AddressCorrection.src.AddressCorrection.Application.DTOs;
using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using AddressCorrection.src.AddressCorrection.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AddressCorrection.src.AddressCorrection.API.Controllers;

/// <summary>
/// Expose l'historique des requêtes de correction et les statistiques globales.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class CorrectionsController : ControllerBase
{
    private readonly ICorrectionRequestRepository _repository;
    private readonly ILogger<CorrectionsController> _logger;

    public CorrectionsController(
        ICorrectionRequestRepository repository,
        ILogger<CorrectionsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    // ── GET /api/corrections ──────────────────────────────────────────────────
    /// <summary>
    /// Retourne l'historique paginé des requêtes de correction.
    /// </summary>
    /// <param name="page">Numéro de page (défaut : 1).</param>
    /// <param name="pageSize">Taille de page (défaut : 20, max : 100).</param>
    /// <param name="status">Filtre optionnel : "success" | "failed".</param>
    /// <param name="search">Recherche dans l'adresse brute (insensible à la casse).</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedCorrectionRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedCorrectionRequestDto>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null)
    {
        try
        {
            var paged = await _repository.GetPagedAsync(page, pageSize, status, search);
            return Ok(MapToDto(paged));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching corrections history");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An unexpected error occurred." });
        }
    }

    // ── GET /api/corrections/stats ────────────────────────────────────────────
    /// <summary>
    /// Retourne les compteurs globaux (total, succès, échecs, cache, aujourd'hui).
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(CorrectionStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CorrectionStatsDto>> GetStats()
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
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An unexpected error occurred." });
        }
    }

    // ── Mapper privé ──────────────────────────────────────────────────────────

    private static PagedCorrectionRequestDto MapToDto(PagedResult<CorrectionRequest> paged) =>
        new()
        {
            Items = paged.Items
                .Select(r => new CorrectionRequestDto
                {
                    Id = r.Id,
                    RawAddress = r.RawAddress,
                    CorrectedAddress = r.CorrectedAddress,
                    FromCache = r.FromCache,
                    ModelUsed = r.ModelUsed,
                    Status = r.Status,
                    DurationMs = r.DurationMs,
                    Source = r.Source,
                    SentAt = r.SentAt,
                })
                .ToList()
                .AsReadOnly(),
            Total = paged.Total,
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalPages = paged.TotalPages,
            HasNext = paged.HasNext,
            HasPrev = paged.HasPrev,
        };
}