using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using System.Text.Json;

namespace AddressCorrection.src.AddressCorrection.Infrastructure.Referentials;

/// <summary>
/// Client Nominatim / OpenStreetMap.
/// Fallback pour tous les pays européens sans API officielle gratuite dédiée
/// (Allemagne, Italie, Portugal, Belgique, Pays-Bas, etc.)
/// Gratuit, sans clé. Timeout 3s max — ne bloque jamais le système.
/// Placé dans Infrastructure car c'est un détail technique (appel HTTP externe).
/// </summary>
public class NominatimClient : IAddressReferentialClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NominatimClient> _logger;

    // France et Espagne ont leurs propres clients dédiés → Nominatim ne les gère pas
    private static readonly HashSet<string> ExcludedCountries =
        new(StringComparer.OrdinalIgnoreCase) { "France", "FR", "Spain", "España", "ES" };

    // Mapping pays → code ISO 3166-1 alpha-2 pour filtrer les résultats Nominatim
    private static readonly Dictionary<string, string> CountryToIso =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Germany"] = "de",
            ["Allemagne"] = "de",
            ["Deutschland"] = "de",
            ["Italy"] = "it",
            ["Italie"] = "it",
            ["Italia"] = "it",
            ["Portugal"] = "pt",
            ["Belgium"] = "be",
            ["Belgique"] = "be",
            ["België"] = "be",
            ["Netherlands"] = "nl",
            ["Pays-Bas"] = "nl",
            ["Nederland"] = "nl",
            ["Austria"] = "at",
            ["Autriche"] = "at",
            ["Österreich"] = "at",
            ["Switzerland"] = "ch",
            ["Suisse"] = "ch",
            ["Schweiz"] = "ch",
            ["Poland"] = "pl",
            ["Pologne"] = "pl",
            ["Polska"] = "pl",
            ["Sweden"] = "se",
            ["Suède"] = "se",
            ["Sverige"] = "se",
            ["Denmark"] = "dk",
            ["Danemark"] = "dk",
            ["Danmark"] = "dk",
            ["Norway"] = "no",
            ["Norvège"] = "no",
            ["Norge"] = "no",
            ["Finland"] = "fi",
            ["Finlande"] = "fi",
            ["Suomi"] = "fi",
        };

    public string Name => "Nominatim";

    public NominatimClient(IHttpClientFactory httpClientFactory, ILogger<NominatimClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public bool Supports(string country) => !ExcludedCountries.Contains(country);

    public async Task<ReferentialResult> SearchAsync(
        string houseNumber, string street, string city, string postalCode, string country)
    {
        try
        {
            var query = BuildQuery(houseNumber, street, postalCode, city);
            var url = $"https://nominatim.openstreetmap.org/search" +
                      $"?q={Uri.EscapeDataString(query)}" +
                      $"&format=json&addressdetails=1&limit=1";

            if (CountryToIso.TryGetValue(country, out var isoCode))
                url += $"&countrycodes={isoCode}";

            var client = _httpClientFactory.CreateClient("Nominatim");
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Nominatim returned HTTP {Status}", response.StatusCode);
                return NotFound();
            }

            var json = await response.Content.ReadAsStringAsync();
            var results = JsonDocument.Parse(json).RootElement;

            if (results.ValueKind != JsonValueKind.Array || results.GetArrayLength() == 0)
            {
                _logger.LogDebug("Nominatim: no result for '{Query}'", query);
                return NotFound();
            }

            var address = results[0].GetProperty("address");

            return new ReferentialResult
            {
                Found = true,
                HouseNumber = address.TryGetProperty("house_number", out var hn) ? hn.GetString() : houseNumber,
                Street = ExtractStreet(address) ?? street,
                PostalCode = address.TryGetProperty("postcode", out var pc) ? pc.GetString() : postalCode,
                City = ExtractCity(address) ?? city,
                Country = address.TryGetProperty("country", out var co) ? co.GetString() : country,
                Source = "Nominatim"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Nominatim error (silent): {Error}", ex.Message);
            return NotFound();
        }
    }

    // Nominatim retourne la rue sous différentes clés selon les pays
    private static string? ExtractStreet(JsonElement address)
    {
        foreach (var key in new[] { "road", "street", "pedestrian", "footway" })
            if (address.TryGetProperty(key, out var val))
                return val.GetString();
        return null;
    }

    // Nominatim retourne la ville sous différentes clés selon les pays
    private static string? ExtractCity(JsonElement address)
    {
        foreach (var key in new[] { "city", "town", "village", "municipality" })
            if (address.TryGetProperty(key, out var val))
                return val.GetString();
        return null;
    }

    private static string BuildQuery(string houseNumber, string street, string postalCode, string city)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(houseNumber)) parts.Add(houseNumber);
        if (!string.IsNullOrWhiteSpace(street)) parts.Add(street);
        if (!string.IsNullOrWhiteSpace(postalCode)) parts.Add(postalCode);
        if (!string.IsNullOrWhiteSpace(city)) parts.Add(city);
        return string.Join(", ", parts);
    }

    private static ReferentialResult NotFound() => new() { Found = false, Source = "Nominatim" };
}