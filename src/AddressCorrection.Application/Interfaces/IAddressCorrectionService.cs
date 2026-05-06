using AddressCorrection.src.AddressCorrection.Application.DTOs;

namespace AddressCorrection.src.AddressCorrection.Application.Interfaces;

public interface IAddressCorrectionService
{
    Task<AddressResponse?> CorrectAsync(AddressRequest request);
}


