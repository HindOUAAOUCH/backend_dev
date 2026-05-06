using AddressCorrection.src.AddressCorrection.Application.DTOs;
using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using AddressCorrection.src.AddressCorrection.Application.Pipeline;
using System.Runtime.ExceptionServices;

namespace AddressCorrection.src.AddressCorrection.Application.Services;

public class AddressCorrectionOrchestrator : IAddressCorrector
{
    private readonly AddressCorrectionPipeline _pipeline;
    private readonly ILogger<AddressCorrectionOrchestrator> _logger;

    public AddressCorrectionOrchestrator(
        AddressCorrectionPipeline pipeline,
        ILogger<AddressCorrectionOrchestrator> logger)
    {
        _pipeline = pipeline;
        _logger = logger;
    }

    public async Task<AddressResponse> CorrectAsync(AddressRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RawAddress))
            throw new ArgumentException("RawAddress is required.", nameof(request));

        var context = new AddressCorrectionContext
        {
            Request = request,
            NormalizedAddress = request.RawAddress.Trim().ToLowerInvariant(),
        };

        context = await _pipeline.ExecuteAsync(context);

        if (context.Error != null)
            ExceptionDispatchInfo.Throw(context.Error);

        return context.Result!;
    }
}
