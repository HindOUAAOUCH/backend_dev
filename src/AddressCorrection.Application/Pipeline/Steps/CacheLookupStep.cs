using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using AddressCorrection.src.AddressCorrection.Application.Mappers;
using AddressCorrection.src.AddressCorrection.Domain.Constants;

namespace AddressCorrection.src.AddressCorrection.Application.Pipeline.Steps;

public class CacheLookupStep : ICorrectionStep
{
    private readonly IAddressCacheStrategy _cache;
    private readonly ILogger<CacheLookupStep> _logger;

    public CacheLookupStep(IAddressCacheStrategy cache, ILogger<CacheLookupStep> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<AddressCorrectionContext> ExecuteAsync(AddressCorrectionContext context)
    {
        var cached = await _cache.GetIfExistsAsync(context.NormalizedAddress!);
        if (cached == null) return context;

        _logger.LogInformation("Cache hit for: {Address}", context.NormalizedAddress);

        context.Result = AddressMapper.ToResponse(cached);
        context.FromCache = true;
        context.ModelUsed = cached.ModelUsed;
        context.Status = CorrectionConstants.Status.Success;

        return context;
    }
}
