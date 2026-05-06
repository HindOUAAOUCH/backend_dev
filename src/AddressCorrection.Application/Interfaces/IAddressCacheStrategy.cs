using AddressCorrection.src.AddressCorrection.Domain.Entities;

namespace AddressCorrection.src.AddressCorrection.Application.Interfaces;

public interface IAddressCacheStrategy
{
    Task<AddressCorrectionRecord?> GetIfExistsAsync(string normalizedAddress);
    Task SaveAsync(AddressCorrectionRecord record);
}
