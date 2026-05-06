using AddressCorrection.src.AddressCorrection.Application.DTOs;
using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using AddressCorrection.src.AddressCorrection.Application.Pipeline;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;

namespace AddressCorrection.src.AddressCorrection.Application.Services;

public class AddressCorrectionOrchestrator : IAddressCorrector
{
    private static readonly Regex NewLinePattern = new(@"[\r\n]+", RegexOptions.Compiled);

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
            // Normalize and strip CR/LF to prevent log-forging in downstream steps.
            NormalizedAddress = NewLinePattern.Replace(
                request.RawAddress.Trim().ToLowerInvariant(), string.Empty),
        };

        context = await _pipeline.ExecuteAsync(context);

        if (context.Error != null)
            ExceptionDispatchInfo.Throw(context.Error);

        return context.Result!;
    }
}
