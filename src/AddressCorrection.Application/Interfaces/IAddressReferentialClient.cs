namespace AddressCorrection.src.AddressCorrection.Application.Interfaces;

/// <summary>
/// Résultat retourné par n'importe quel référentiel (BAN, CartoCiudad, Nominatim).
/// Found = false si le référentiel n'a pas trouvé l'adresse.
/// </summary>
public class ReferentialResult
{
    public bool Found { get; set; }
    public string? HouseNumber { get; set; }
    public string? Street { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string Source { get; set; } = string.Empty; // "BAN" | "CartoCiudad" | "Nominatim"
}

/// <summary>
/// Contrat commun pour tous les clients de référentiels d'adresses européens.
/// Implémenté par BanClient, CartoCiudadClient, NominatimClient.
/// </summary>
public interface IAddressReferentialClient
{
    string Name { get; }
    bool Supports(string country);

    /// <summary>
    /// Recherche l'adresse dans le référentiel.
    /// Ne lève jamais d'exception — toujours silencieux.
    /// </summary>
    Task<ReferentialResult> SearchAsync(
        string houseNumber, string street, string city, string postalCode, string country);
}