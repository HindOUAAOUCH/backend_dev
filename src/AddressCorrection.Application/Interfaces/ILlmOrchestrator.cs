using AddressCorrection.src.AddressCorrection.Application.DTOs;

namespace AddressCorrection.src.AddressCorrection.Application.Interfaces;

/// <summary>
/// Orchestrateur LLM avec logique de fallback multi-modèles.
/// Responsabilité unique : appeler le LLM et retourner un résultat ou null si tous les modèles échouent.
/// </summary>
public interface ILlmOrchestrator
{
    Task<(AddressResponse? Result, string? ModelUsed)> CompleteWithFallbackAsync(
        string prompt,
        string rawAddress);
}
