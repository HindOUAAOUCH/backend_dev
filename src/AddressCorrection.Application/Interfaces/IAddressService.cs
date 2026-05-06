using AddressCorrection.src.AddressCorrection.Application.DTOs;

namespace AddressCorrection.src.AddressCorrection.Application.Interfaces;

public interface IAddressService
{
    Task<AddressResponse?> CorrectAsync(AddressRequest request);
}


