using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using AddressCorrection.src.AddressCorrection.Domain.Entities;
using AddressCorrection.src.AddressCorrection.Infrastructure.Mappers;
using AddressCorrection.src.AddressCorrection.Infrastructure.Persistence.Documents;
using MongoDB.Driver;

namespace AddressCorrection.src.AddressCorrection.Infrastructure.Persistence;

public sealed class CorrectionRequestRepository : ICorrectionRequestRepository
{
    private readonly IMongoCollection<CorrectionRequestDocument> _collection;

    public CorrectionRequestRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<CorrectionRequestDocument>("correction_requests");
    }

    // ── Écriture ──────────────────────────────────────────────────────────────

    public async Task SaveAsync(CorrectionRequest request)
    {
        var doc = CorrectionRequestDocumentMapper.ToDocument(request);
        await _collection.InsertOneAsync(doc);
    }

    // ── Lecture — aujourd'hui ─────────────────────────────────────────────────

    public async Task<List<CorrectionRequest>> GetTodayRequestsAsync()
    {
        var startOfDay = DateTime.UtcNow.Date;
        var docs = await _collection
            .Find(r => r.SentAt >= startOfDay)
            .SortByDescending(r => r.SentAt)
            .ToListAsync();
        return docs.ConvertAll(CorrectionRequestDocumentMapper.ToEntity);
    }

    // ── Lecture — paginée avec filtres ────────────────────────────────────────

    public async Task<PagedResult<CorrectionRequest>> GetPagedAsync(
        int page,
        int pageSize,
        string? status = null,
        string? search = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);

        var builder = Builders<CorrectionRequestDocument>.Filter;
        var filter = builder.Empty;

        if (!string.IsNullOrWhiteSpace(status))
            filter &= builder.Eq(r => r.Status, status);

        if (!string.IsNullOrWhiteSpace(search))
            filter &= builder.Regex(
                r => r.RawAddress,
                new MongoDB.Bson.BsonRegularExpression(search, "i"));

        if (dateFrom.HasValue)
            filter &= builder.Gte(r => r.SentAt, dateFrom.Value);

        if (dateTo.HasValue)
            filter &= builder.Lte(r => r.SentAt, dateTo.Value);

        var total = await _collection.CountDocumentsAsync(filter);

        var docs = await _collection
            .Find(filter)
            .SortByDescending(r => r.SentAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        var items = docs.ConvertAll(CorrectionRequestDocumentMapper.ToEntity);
        return new PagedResult<CorrectionRequest>(items, (int)total, page, pageSize);
    }

    // ── Statistiques globales ─────────────────────────────────────────────────

    public async Task<CorrectionStats> GetStatsAsync()
    {
        var startOfDay = DateTime.UtcNow.Date;

        var (total, success, failed, cacheHits, today) = await (
            _collection.CountDocumentsAsync(Builders<CorrectionRequestDocument>.Filter.Empty),
            _collection.CountDocumentsAsync(Builders<CorrectionRequestDocument>.Filter.Eq(r => r.Status, "success")),
            _collection.CountDocumentsAsync(Builders<CorrectionRequestDocument>.Filter.Eq(r => r.Status, "failed")),
            _collection.CountDocumentsAsync(Builders<CorrectionRequestDocument>.Filter.Eq(r => r.FromCache, true)),
            _collection.CountDocumentsAsync(Builders<CorrectionRequestDocument>.Filter.Gte(r => r.SentAt, startOfDay))
        ).WhenAll();

        return new CorrectionStats(total, success, failed, cacheHits, today);
    }
}

// ── Extension helper pour WhenAll sur tuple ───────────────────────────────────

file static class TaskExtensions
{
    public static async Task<(T1, T2, T3, T4, T5)> WhenAll<T1, T2, T3, T4, T5>(
        this (Task<T1> t1, Task<T2> t2, Task<T3> t3, Task<T4> t4, Task<T5> t5) tasks)
    {
        await Task.WhenAll(tasks.t1, tasks.t2, tasks.t3, tasks.t4, tasks.t5);
        return (tasks.t1.Result, tasks.t2.Result, tasks.t3.Result, tasks.t4.Result, tasks.t5.Result);
    }
}