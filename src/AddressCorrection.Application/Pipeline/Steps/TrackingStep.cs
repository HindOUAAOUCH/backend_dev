using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using AddressCorrection.src.AddressCorrection.Application.Mappers;

namespace AddressCorrection.src.AddressCorrection.Application.Pipeline.Steps;

public class TrackingStep : ICorrectionStep
{
    private readonly IRequestTracker _tracker;

    public TrackingStep(IRequestTracker tracker)
    {
        _tracker = tracker;
    }

    /// <inheritdoc />
    /// Tracking must always run, including when a previous step (e.g. LLM) fails,
    /// so that failure events are recorded for analytics and diagnostics.
    public bool RunAlways => true;

    public async Task<AddressCorrectionContext> ExecuteAsync(AddressCorrectionContext context)
    {
        context.DurationMs = (long)(DateTimeOffset.UtcNow - context.StartedAt).TotalMilliseconds;

        var correctedAddress = context.Result != null
            ? AddressMapper.ToFormattedLine(context.Result)
            : null;

        await _tracker.TraceAsync(
            rawAddress: context.Request.RawAddress,
            correctedAddress: correctedAddress,
            fromCache: context.FromCache,
            modelUsed: context.ModelUsed ?? string.Empty,
            status: context.Status ?? string.Empty,
            durationMs: context.DurationMs);

        return context;
    }
}
