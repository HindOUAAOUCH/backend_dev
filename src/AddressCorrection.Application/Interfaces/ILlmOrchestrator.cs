using AddressCorrection.src.AddressCorrection.Application.DTOs;

namespace AddressCorrection.src.AddressCorrection.Application.Interfaces;

public interface ILlmOrchestrator
{
    Task<(AddressResponse? Result, string? ModelUsed)> CompleteWithFallbackAsync(
        string prompt,
        string rawAddress);
}
