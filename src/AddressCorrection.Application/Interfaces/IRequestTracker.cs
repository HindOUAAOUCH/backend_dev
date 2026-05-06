namespace AddressCorrection.src.AddressCorrection.Application.Interfaces;

/// <summary>
/// Responsabilité unique : logging et analytics des tentatives de correction.
/// </summary>
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
