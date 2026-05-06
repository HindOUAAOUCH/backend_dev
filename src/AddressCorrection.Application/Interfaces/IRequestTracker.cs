namespace AddressCorrection.src.AddressCorrection.Application.Interfaces;

public interface IRequestTracker
{
    Task TraceAsync(
        string rawAddress,
        string? correctedAddress,
        bool fromCache,
        string modelUsed,
        string status,
        long durationMs);
}
