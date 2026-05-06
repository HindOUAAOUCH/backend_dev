using AddressCorrection.src.AddressCorrection.Application.Interfaces;

namespace AddressCorrection.src.AddressCorrection.Application.Pipeline.Steps;

/// <summary>
/// Étape 3 : Validation et enrichissement via les référentiels d'adresses (BAN, CartoCiudad, Nominatim).
/// Ignorée si aucun résultat LLM n'est disponible ou si le résultat provient du cache.
/// Seule la TimeoutException est absorbée (appel non-bloquant) ; les autres exceptions se propagent.
/// </summary>
public class ReferentialValidationStep : ICorrectionStep
{
    private readonly IAddressReferentialService _referentialService;
    private readonly ILogger<ReferentialValidationStep> _logger;

    public ReferentialValidationStep(
        IAddressReferentialService referentialService,
        ILogger<ReferentialValidationStep> logger)
    {
        _referentialService = referentialService;
        _logger = logger;
    }

    public async Task<AddressCorrectionContext> ExecuteAsync(AddressCorrectionContext context)
    {
        if (context.Result == null || context.FromCache) return context;

        try
        {
            var referentialResult = await _referentialService.ValidateAsync(
                context.Result.HouseNumber,
                context.Result.Street,
                context.Result.City,
                context.Result.PostalCode,
                context.Result.Country);

            if (referentialResult != null)
            {
                if (!string.IsNullOrWhiteSpace(referentialResult.PostalCode))
                    context.Result.PostalCode = referentialResult.PostalCode;
                if (!string.IsNullOrWhiteSpace(referentialResult.City))
                    context.Result.City = referentialResult.City;
                if (!string.IsNullOrWhiteSpace(referentialResult.Street))
                    context.Result.Street = referentialResult.Street;
            }
        }
        catch (TimeoutException)
        {
            var sanitizedAddress = context.NormalizedAddress?.Replace("\r", "").Replace("\n", "");
            _logger.LogWarning("Referential validation timed out for address: {Address} (non-blocking). Continuing without enrichment.",
                sanitizedAddress);
        }

        return context;
    }
}
