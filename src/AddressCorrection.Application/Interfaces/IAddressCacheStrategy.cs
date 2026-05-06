using AddressCorrection.src.AddressCorrection.Domain.Entities;

namespace AddressCorrection.src.AddressCorrection.Application.Interfaces;

/// <summary>
/// Stratégie de cache pour les corrections d'adresses.
/// Responsabilité unique : lookup et sauvegarde dans le cache.
/// </summary>
public interface IAddressCacheStrategy
{
    Task<AddressCorrectionRecord?> GetIfExistsAsync(string normalizedAddress);
    Task SaveAsync(AddressCorrectionRecord record);
}
