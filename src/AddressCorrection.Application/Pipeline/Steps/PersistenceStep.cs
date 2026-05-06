using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using AddressCorrection.src.AddressCorrection.Application.Mappers;

namespace AddressCorrection.src.AddressCorrection.Application.Pipeline.Steps;

/// <summary>
/// Étape 4 : Sauvegarde du résultat dans le cache.
/// Ignorée si le résultat est absent ou s'il provient déjà du cache.
/// Arrête le chronomètre après la sauvegarde pour mesurer la durée totale de traitement.
/// </summary>
public class PersistenceStep : ICorrectionStep
{
    private readonly IAddressCacheStrategy _cacheStrategy;

    public PersistenceStep(IAddressCacheStrategy cacheStrategy)
    {
        _cacheStrategy = cacheStrategy;
    }

    public async Task<AddressCorrectionContext> ExecuteAsync(AddressCorrectionContext context)
    {
        if (context.Result == null || context.FromCache) return context;

        if (string.IsNullOrEmpty(context.NormalizedAddress))
            throw new InvalidOperationException("NormalizedAddress must be set by the orchestrator before PersistenceStep.");
        if (context.ModelUsed == null)
            throw new InvalidOperationException("ModelUsed must be set by LlmProcessingStep before PersistenceStep.");

        var record = AddressMapper.ToRecord(
            context.Request,
            context.Result,
            context.NormalizedAddress!,
            context.ModelUsed!);

        await _cacheStrategy.SaveAsync(record);
        context.Stopwatch.Stop();
        return context;
    }
}
