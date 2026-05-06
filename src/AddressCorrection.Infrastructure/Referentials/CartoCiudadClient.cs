using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using System.Text.Json;

namespace AddressCorrection.src.AddressCorrection.Infrastructure.Referentials;

/// <summary>
/// Client CartoCiudad — référentiel officiel espagnol (IGN).
/// API gratuite, sans clé, sans quota : https://www.cartociudad.es/geocoder/api
/// Placé dans Infrastructure car c'est un détail technique (appel HTTP externe).
/// </summary>
public class CartoCiudadClient : IAddressReferentialClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CartoCiudadClient> _logger;

    public string Name => "CartoCiudad";

    public CartoCiudadClient(IHttpClientFactory httpClientFactory, ILogger<CartoCiudadClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public bool Supports(string country) =>
        country.Equals("Spain", StringComparison.OrdinalIgnoreCase) ||
        country.Equals("España", StringComparison.OrdinalIgnoreCase) ||
        country.Equals("ES", StringComparison.OrdinalIgnoreCase);

    public async Task<ReferentialResult> SearchAsync(
        string houseNumber, string street, string city, string postalCode, string country)
    {
        try
        {
            var query = BuildQuery(houseNumber, street, city);
            var url = $"https://www.cartociudad.es/geocoder/api/geocoder/candidates" +
                      $"?q={Uri.EscapeDataString(query)}&limit=1";

            var client = _httpClientFactory.CreateClient("Referential");
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("CartoCiudad returned HTTP {Status}", response.StatusCode);
                return NotFound();
            }

            var json = await response.Content.ReadAsStringAsync();
            var results = JsonDocument.Parse(json).RootElement;

            if (results.ValueKind != JsonValueKind.Array || results.GetArrayLength() == 0)
            {
                _logger.LogDebug("CartoCiudad: no result for '{Query}'", query);
                return NotFound();
            }

            var first = results[0];

            var streetType = first.TryGetProperty("tip_via", out var tv) ? tv.GetString() : null;
            var streetName = first.TryGetProperty("address", out var ad) ? ad.GetString() : street;
            var foundCity = first.TryGetProperty("muni", out var mu) ? mu.GetString() : city;
            var foundPostal = first.TryGetProperty("postalCode", out var pc) ? pc.GetString() : postalCode;
            var foundNumber = first.TryGetProperty("portalNumber", out var pn) ? pn.GetString() : houseNumber;

            // Reconstruction : "Calle" + "Mayor" → "Calle Mayor"
            var fullStreet = !string.IsNullOrWhiteSpace(streetType) && !string.IsNullOrWhiteSpace(streetName)
                ? $"{Capitalize(streetType)} {streetName}"
                : streetName ?? street;

            return new ReferentialResult
            {
                Found = true,
                HouseNumber = foundNumber,
                Street = fullStreet,
                PostalCode = foundPostal,
                City = !string.IsNullOrWhiteSpace(foundCity) ? Capitalize(foundCity) : city,
                Country = "Spain",
                Source = "CartoCiudad"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning("CartoCiudad error (silent): {Error}", ex.Message);
            return NotFound();
        }
    }

    private static string BuildQuery(string houseNumber, string street, string city)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(street)) parts.Add(street);
        if (!string.IsNullOrWhiteSpace(houseNumber)) parts.Add(houseNumber);
        if (!string.IsNullOrWhiteSpace(city)) parts.Add(city);
        return string.Join(" ", parts);
    }

    private static string Capitalize(string s) =>
        string.IsNullOrWhiteSpace(s) ? s : char.ToUpper(s[0]) + s[1..].ToLower();

    private static ReferentialResult NotFound() => new() { Found = false, Source = "CartoCiudad" };
}