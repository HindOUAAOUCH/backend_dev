using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using AddressCorrection.src.AddressCorrection.Application.Mappers;

namespace AddressCorrection.src.AddressCorrection.Application.Pipeline.Steps;

public class PersistenceStep : ICorrectionStep
{
    private readonly IAddressCacheStrategy _cache;

    public PersistenceStep(IAddressCacheStrategy cache)
    {
        _cache = cache;
    }

    public async Task<AddressCorrectionContext> ExecuteAsync(AddressCorrectionContext context)
    {
        // Skip if no result, already from cache (already persisted), or a previous step failed
        if (context.Result == null || context.FromCache || context.IsFailed) return context;

        var record = AddressMapper.ToRecord(
            context.Request,
            context.Result,
            context.NormalizedAddress!,
            context.ModelUsed!);

        await _cache.SaveAsync(record);

        return context;
    }
}
