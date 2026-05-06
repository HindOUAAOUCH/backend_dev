using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using System.Text.Json;

namespace AddressCorrection.src.AddressCorrection.Infrastructure.Referentials;

/// <summary>
/// Client BAN — Base Adresse Nationale française.
/// API officielle gratuite : https://api-adresse.data.gouv.fr
/// 26 millions d'adresses françaises officielles, sans clé API.
/// Placé dans Infrastructure car c'est un détail technique (appel HTTP externe).
/// </summary>
public class BanClient : IAddressReferentialClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BanClient> _logger;

    public string Name => "BAN";

    public BanClient(IHttpClientFactory httpClientFactory, ILogger<BanClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public bool Supports(string country) =>
        country.Equals("France", StringComparison.OrdinalIgnoreCase) ||
        country.Equals("FR", StringComparison.OrdinalIgnoreCase);

    public async Task<ReferentialResult> SearchAsync(
        string houseNumber, string street, string city, string postalCode, string country)
    {
        try
        {
            var query = BuildQuery(houseNumber, street, city);
            var url = $"https://api-adresse.data.gouv.fr/search/?q={Uri.EscapeDataString(query)}&limit=1";

            if (!string.IsNullOrWhiteSpace(postalCode))
                url += $"&postcode={Uri.EscapeDataString(postalCode)}";

            var client = _httpClientFactory.CreateClient("Referential");
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("BAN returned HTTP {Status}", response.StatusCode);
                return NotFound();
            }

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var features = doc.RootElement.GetProperty("features");

            if (features.GetArrayLength() == 0)
            {
                _logger.LogDebug("BAN: no result for '{Query}'", query);
                return NotFound();
            }

            var props = features[0].GetProperty("properties");

            // Score BAN entre 0 et 1 — on accepte uniquement si > 0.5
            var score = props.TryGetProperty("score", out var s) ? s.GetDouble() : 0;
            if (score < 0.5)
            {
                _logger.LogDebug("BAN: low score {Score} for '{Query}'", score, query);
                return NotFound();
            }

            return new ReferentialResult
            {
                Found = true,
                HouseNumber = props.TryGetProperty("housenumber", out var hn) ? hn.GetString() : houseNumber,
                Street = props.TryGetProperty("street", out var st) ? st.GetString() : street,
                PostalCode = props.TryGetProperty("postcode", out var pc) ? pc.GetString() : postalCode,
                City = props.TryGetProperty("city", out var ci) ? ci.GetString() : city,
                Country = "France",
                Source = "BAN"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning("BAN error (silent): {Error}", ex.Message);
            return NotFound();
        }
    }

    private static string BuildQuery(string houseNumber, string street, string city)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(houseNumber)) parts.Add(houseNumber);
        if (!string.IsNullOrWhiteSpace(street)) parts.Add(street);
        if (!string.IsNullOrWhiteSpace(city)) parts.Add(city);
        return string.Join(" ", parts);
    }

    private static ReferentialResult NotFound() => new() { Found = false, Source = "BAN" };
}