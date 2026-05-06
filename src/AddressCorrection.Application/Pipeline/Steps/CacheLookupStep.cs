using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using AddressCorrection.src.AddressCorrection.Application.Mappers;
using AddressCorrection.src.AddressCorrection.Domain.Constants;

namespace AddressCorrection.src.AddressCorrection.Application.Pipeline.Steps;

/// <summary>
/// Étape 1 : Recherche dans le cache. Si l'adresse est déjà corrigée,
/// peuple le résultat et arrête le chronomètre. Les étapes suivantes détecteront
/// que Result est déjà défini et s'exécuteront sans action.
/// </summary>
public class CacheLookupStep : ICorrectionStep
{
    private readonly IAddressCacheStrategy _cacheStrategy;
    private readonly ILogger<CacheLookupStep> _logger;

    public CacheLookupStep(IAddressCacheStrategy cacheStrategy, ILogger<CacheLookupStep> logger)
    {
        _cacheStrategy = cacheStrategy;
        _logger = logger;
    }

    public async Task<AddressCorrectionContext> ExecuteAsync(AddressCorrectionContext context)
    {
        if (context.Result != null) return context;

        if (string.IsNullOrEmpty(context.NormalizedAddress))
            throw new InvalidOperationException("NormalizedAddress must be set before CacheLookupStep.");

        var cached = await _cacheStrategy.GetIfExistsAsync(context.NormalizedAddress);
        if (cached == null) return context;

        // Sanitize user-provided address before logging to prevent log forging
        var sanitizedAddress = context.NormalizedAddress.Replace("\r", "").Replace("\n", "");
        _logger.LogInformation("Cache hit for: {Address}", sanitizedAddress);
        context.Result = AddressMapper.ToResponse(cached);
        context.ModelUsed = cached.ModelUsed;
        context.FromCache = true;
        context.Status = CorrectionConstants.Status.Success;
        context.Stopwatch.Stop();
        return context;
    }
}
