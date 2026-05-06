using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using AddressCorrection.src.AddressCorrection.Domain.Constants;
using AddressCorrection.src.AddressCorrection.Domain.Entities;

namespace AddressCorrection.src.AddressCorrection.Infrastructure.Services;

public class RequestTracker : IRequestTracker
{
    private readonly ICorrectionRequestRepository _correctionRequestRepository;
    private readonly IUsageTrackingRepository _usageTrackingRepository;

    public RequestTracker(
        ICorrectionRequestRepository correctionRequestRepository,
        IUsageTrackingRepository usageTrackingRepository)
    {
        _correctionRequestRepository = correctionRequestRepository;
        _usageTrackingRepository = usageTrackingRepository;
    }

    public async Task TraceAsync(
        string rawAddress,
        string? correctedAddress,
        bool fromCache,
        string modelUsed,
        string status,
        long durationMs)
    {
        await _correctionRequestRepository.SaveAsync(new CorrectionRequest
        {
            RawAddress = rawAddress,
            CorrectedAddress = correctedAddress,
            FromCache = fromCache,
            ModelUsed = modelUsed,
            Status = status,
            DurationMs = durationMs,
            Source = CorrectionConstants.Source.Api,
            SentAt = DateTime.UtcNow,
        });

        await _usageTrackingRepository.IncrementAsync(fromCache: fromCache);
    }
}
