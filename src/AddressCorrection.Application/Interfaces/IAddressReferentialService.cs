namespace AddressCorrection.src.AddressCorrection.Application.Interfaces;

/// <summary>
/// Abstraction du service de validation référentiel d'adresses.
/// Permet de mocker le service dans les tests unitaires et respecte le DIP.
/// </summary>
public interface IAddressReferentialService
{
    /// <summary>
    /// Valide les composants d'une adresse via les référentiels configurés (BAN, CartoCiudad, Nominatim).
    /// Retourne null si aucun référentiel ne correspond ou si la validation échoue.
    /// </summary>
    Task<ReferentialResult?> ValidateAsync(
        string? houseNumber,
        string? street,
        string? city,
        string? postalCode,
        string? country);
}
