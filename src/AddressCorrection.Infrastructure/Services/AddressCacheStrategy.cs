using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using AddressCorrection.src.AddressCorrection.Domain.Entities;

namespace AddressCorrection.src.AddressCorrection.Infrastructure.Services;

public class AddressCacheStrategy : IAddressCacheStrategy
{
    private readonly IAddressRepository _repository;

    public AddressCacheStrategy(IAddressRepository repository)
    {
        _repository = repository;
    }

    public Task<AddressCorrectionRecord?> GetIfExistsAsync(string normalizedAddress)
        => _repository.FindByNormalizedAddressAsync(normalizedAddress);

    public Task SaveAsync(AddressCorrectionRecord record)
        => _repository.SaveAsync(record);
}
