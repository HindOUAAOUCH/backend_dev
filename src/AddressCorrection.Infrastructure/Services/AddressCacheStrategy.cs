using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using AddressCorrection.src.AddressCorrection.Domain.Entities;

namespace AddressCorrection.src.AddressCorrection.Infrastructure.Services;

/// <summary>
/// Stratégie de cache pour les corrections d'adresses.
/// Délègue au repository MongoDB. Responsabilité unique : abstraire le stockage cache.
/// </summary>
public class AddressCacheStrategy : IAddressCacheStrategy
{
    private readonly IAddressRepository _addressRepository;

    public AddressCacheStrategy(IAddressRepository addressRepository)
    {
        _addressRepository = addressRepository;
    }

    public Task<AddressCorrectionRecord?> GetIfExistsAsync(string normalizedAddress)
        => _addressRepository.FindByNormalizedAddressAsync(normalizedAddress);

    public Task SaveAsync(AddressCorrectionRecord record)
        => _addressRepository.SaveAsync(record);
}
