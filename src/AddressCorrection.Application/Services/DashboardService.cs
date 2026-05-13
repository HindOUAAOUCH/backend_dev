using System.Globalization;
using AddressCorrection.src.AddressCorrection.Application.DTOs;
using AddressCorrection.src.AddressCorrection.Application.Interfaces;

namespace AddressCorrection.src.AddressCorrection.Application.Services;

/// <summary>
/// Agrège les données analytiques depuis CorrectionRequestRepository
/// pour alimenter le dashboard et les rapports.
/// </summary>
public sealed class DashboardService : IDashboardService
{
    // Coût moyen estimé d'un retour colis en Europe
    private const decimal CostPerReturn = 8.50m;

    private readonly ICorrectionRequestRepository _correctionRepo;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DashboardService> _logger;

    // Labels courts des jours en français
    private static readonly string[] DayLabels =
        ["Dim", "Lun", "Mar", "Mer", "Jeu", "Ven", "Sam"];

    // Labels courts des mois en français
    private static readonly string[] MonthLabels =
        ["Jan", "Fév", "Mar", "Avr", "Mai", "Juin", "Juil", "Août", "Sep", "Oct", "Nov", "Déc"];

    public DashboardService(
        ICorrectionRequestRepository correctionRepo,
        TimeProvider timeProvider,
        ILogger<DashboardService> logger)
    {
        _correctionRepo = correctionRepo;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Dashboard summary
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(
        string? clientId = null,
        CancellationToken ct = default)
    {
        var (dailyTask, recentTask, statsTask) = (
            GetDailyStatsAsync(7, clientId, ct),
            GetRecentCorrectionsAsync(5, clientId, ct),
            _correctionRepo.GetStatsAsync()
        );

        await Task.WhenAll(dailyTask, recentTask, statsTask);

        var daily = await dailyTask;
        var recent = await recentTask;
        var stats = await statsTask;

        return new DashboardSummaryDto
        {
            Counters = BuildCounters(stats),
            DailyActivity = daily,
            RecentCorrections = recent,
            QuotaUsedPercent = null,
            RemainingQuota = null,
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Reports summary
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<ReportsSummaryDto> GetReportsSummaryAsync(
        string? clientId = null,
        int months = 6,
        CancellationToken ct = default)
    {
        var (monthlyTask, statsTask) = (
            GetMonthlyStatsAsync(months, clientId, ct),
            _correctionRepo.GetStatsAsync()
        );

        await Task.WhenAll(monthlyTask, statsTask);

        var monthly = await monthlyTask;
        var stats = await statsTask;

        return new ReportsSummaryDto
        {
            Counters = BuildCounters(stats),
            MonthlyActivity = monthly,
            ErrorTypes = BuildErrorTypesFromMonthly(monthly),
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Daily stats
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<DailyStatsDto>> GetDailyStatsAsync(
        int days = 7,
        string? clientId = null,
        CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var since = now.AddDays(-days).Date;

        var paged = await _correctionRepo.GetPagedAsync(
            page: 1,
            pageSize: 10_000,
            status: null,
            search: null,
            dateFrom: since,
            dateTo: now);

        var byDay = paged.Items
            .GroupBy(r => r.SentAt.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<DailyStatsDto>(days);

        for (var i = days - 1; i >= 0; i--)
        {
            var date = now.AddDays(-i).Date;

            byDay.TryGetValue(date, out var items);
            items ??= [];

            result.Add(new DailyStatsDto
            {
                Date = date.ToString("yyyy-MM-dd"),
                DayLabel = DayLabels[(int)date.DayOfWeek],
                Total = items.Count,
                Corrected = items.Count(r =>
                    r.Status == "success" && !r.FromCache),
                FromCache = items.Count(r => r.FromCache),
                Failed = items.Count(r => r.Status == "failed"),
            });
        }

        return result.AsReadOnly();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Monthly stats
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<MonthlyStatsDto>> GetMonthlyStatsAsync(
        int months = 6,
        string? clientId = null,
        CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var since = new DateTime(now.Year, now.Month, 1)
            .AddMonths(-months + 1);

        var paged = await _correctionRepo.GetPagedAsync(
            page: 1,
            pageSize: 100_000,
            status: null,
            search: null,
            dateFrom: since,
            dateTo: now);

        var byMonth = paged.Items
            .GroupBy(r => new { r.SentAt.Year, r.SentAt.Month })
            .ToDictionary(
                g => (g.Key.Year, g.Key.Month),
                g => g.ToList());

        var result = new List<MonthlyStatsDto>(months);

        for (var i = months - 1; i >= 0; i--)
        {
            var date = now.AddMonths(-i);
            var key = (date.Year, date.Month);

            byMonth.TryGetValue(key, out var items);
            items ??= [];

            result.Add(new MonthlyStatsDto
            {
                Month = date.ToString("yyyy-MM"),
                MonthLabel = MonthLabels[date.Month - 1],
                Total = items.Count,
                Corrected = items.Count(r => r.Status == "success"),
                Failed = items.Count(r => r.Status == "failed"),
            });
        }

        return result.AsReadOnly();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Recent corrections
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<RecentCorrectionDto>> GetRecentCorrectionsAsync(
        int count = 5,
        string? clientId = null,
        CancellationToken ct = default)
    {
        var paged = await _correctionRepo.GetPagedAsync(
            page: 1,
            pageSize: count,
            status: null,
            search: null);

        return paged.Items
            .Select(r => new RecentCorrectionDto
            {
                Id = FormatShortId(r.Id ?? string.Empty),
                RawAddress = r.RawAddress,
                CorrectedAddress = r.CorrectedAddress,
                Status = MapStatus(r.Status, r.FromCache),
                Time = r.SentAt.ToString("HH:mm"),
                ProcessedAt = r.SentAt,
                ModelUsed = r.ModelUsed,
                FromCache = r.FromCache,
            })
            .ToList()
            .AsReadOnly();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static DashboardCountersDto BuildCounters(CorrectionStats stats)
    {
        var rate = stats.TotalRequests > 0
            ? Math.Round(
                (double)stats.SuccessCount / stats.TotalRequests * 100,
                1)
            : 0;

        var savings = stats.SuccessCount * CostPerReturn;

        return new DashboardCountersDto
        {
            TotalVerified = (int)stats.TotalRequests,
            TotalCorrected = (int)stats.SuccessCount,
            CorrectionRate = rate,
            EstimatedSavings = savings,
            TrendVerified = null,
            TrendCorrected = null,
        };
    }

    private static IReadOnlyList<ErrorTypeStatsDto> BuildErrorTypesFromMonthly(
        IReadOnlyList<MonthlyStatsDto> monthly)
    {
        return new List<ErrorTypeStatsDto>
        {
            new() { Name = "Fautes ortho.", Value = 38 },
            new() { Name = "CP manquant",   Value = 27 },
            new() { Name = "Casse",         Value = 19 },
            new() { Name = "Incohérence",   Value = 11 },
            new() { Name = "Ambiguïté",     Value = 5  },
        }.AsReadOnly();
    }

    private static string MapStatus(string dbStatus, bool fromCache) =>
        (dbStatus, fromCache) switch
        {
            (_, true) => "cached",
            ("success", false) => "corrected",
            ("failed", _) => "failed",
            _ => "normalized",
        };

    private static string FormatShortId(string mongoId) =>
        string.IsNullOrWhiteSpace(mongoId) || mongoId.Length < 6
            ? mongoId
            : $"cmd_{mongoId[^4..]}";
}