using AddressCorrection.src.AddressCorrection.Application.Interfaces;

namespace AddressCorrection.src.AddressCorrection.Application.Services;

public class AddressReferentialService
{
    private readonly IEnumerable<IAddressReferentialClient> _clients;
    private readonly ILogger<AddressReferentialService> _logger;

    public AddressReferentialService(
        IEnumerable<IAddressReferentialClient> clients,
        ILogger<AddressReferentialService> logger)
    {
        _clients = clients;
        _logger = logger;
    }
    public async Task<ReferentialResult?> ValidateAsync(
        string? houseNumber, string? street, string? city, string? postalCode, string? country)
    {
        if (string.IsNullOrWhiteSpace(country) && string.IsNullOrWhiteSpace(city))
        {
            _logger.LogDebug("Referential skipped: no country and no city available");
            return null;
        }

        var client = _clients.FirstOrDefault(c => c.Supports(country ?? string.Empty));

        if (client == null)
        {
            _logger.LogDebug("No referential found for country '{Country}' — keeping LLM result", country);
            return null;
        }

        _logger.LogInformation("Referential selected: {Client} for country '{Country}'",
            client.Name, country);

        var result = await client.SearchAsync(
            houseNumber ?? string.Empty,
            street ?? string.Empty,
            city ?? string.Empty,
            postalCode ?? string.Empty,
            country ?? string.Empty);

        if (result.Found)
            _logger.LogInformation(
                "Referential {Client} found → Street: {Street} | City: {City} | PostalCode: {PostalCode}",
                client.Name, result.Street, result.City, result.PostalCode);
        else
            _logger.LogDebug("Referential {Client} found nothing — keeping LLM result as-is", client.Name);

        return result.Found ? result : null;
    }
}