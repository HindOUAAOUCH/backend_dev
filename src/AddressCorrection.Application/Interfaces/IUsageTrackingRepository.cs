using AddressCorrection.src.AddressCorrection.Domain.Entities;

namespace AddressCorrection.src.AddressCorrection.Application.Interfaces;

public interface IUsageTrackingRepository
{
    Task IncrementAsync(bool fromCache);
    Task<UsageTracking?> GetTodayAsync();
}