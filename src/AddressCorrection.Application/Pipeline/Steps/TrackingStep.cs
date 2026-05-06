using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using AddressCorrection.src.AddressCorrection.Application.Mappers;
using AddressCorrection.src.AddressCorrection.Domain.Constants;

namespace AddressCorrection.src.AddressCorrection.Application.Pipeline.Steps;

/// <summary>
/// Étape 5 (finale) : Logging et analytics de la tentative de correction.
/// S'exécute toujours, quel que soit le résultat des étapes précédentes.
/// </summary>
public class TrackingStep : ICorrectionStep
{
    private readonly IRequestTracker _requestTracker;

    public TrackingStep(IRequestTracker requestTracker)
    {
        _requestTracker = requestTracker;
    }

    public async Task<AddressCorrectionContext> ExecuteAsync(AddressCorrectionContext context)
    {
        var durationMs = context.Stopwatch.ElapsedMilliseconds;

        await _requestTracker.TraceAsync(
            rawAddress: context.Request.RawAddress,
            correctedAddress: context.Result != null ? AddressMapper.ToFormattedLine(context.Result) : null,
            fromCache: context.FromCache,
            modelUsed: context.ModelUsed ?? CorrectionConstants.Model.None,
            status: context.Status ?? CorrectionConstants.Status.Failed,
            durationMs: durationMs);

        return context;
    }
}
