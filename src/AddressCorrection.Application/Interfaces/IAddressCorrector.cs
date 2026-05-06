using AddressCorrection.src.AddressCorrection.Application.DTOs;

namespace AddressCorrection.src.AddressCorrection.Application.Interfaces;

/// <summary>
/// Contrat du service de correction d'adresses.
/// </summary>
public interface IAddressCorrector
{
    Task<AddressResponse> CorrectAsync(AddressRequest request);
}
