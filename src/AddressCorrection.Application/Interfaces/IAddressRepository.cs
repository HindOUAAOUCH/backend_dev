using AddressCorrection.src.AddressCorrection.Domain.Entities;

namespace AddressCorrection.src.AddressCorrection.Application.Interfaces;

public interface IAddressRepository
{
    Task<AddressCorrectionRecord?> FindByNormalizedAddressAsync(string normalizedAddress);
    Task SaveAsync(AddressCorrectionRecord record);
}