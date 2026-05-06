using AddressCorrection.src.AddressCorrection.Application.Interfaces;

namespace AddressCorrection.src.AddressCorrection.Application.Pipeline.Steps;

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
        // Skip if no LLM result, already from cache, or a previous step failed
        if (context.Result == null || context.FromCache || context.IsFailed) return context;

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
            // Referential is non-blocking — a timeout should not fail the correction
            _logger.LogWarning("Referential validation timed out (non-blocking)");
        }

        return context;
    }
}
