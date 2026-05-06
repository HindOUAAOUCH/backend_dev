using AddressCorrection.src.AddressCorrection.Application.Exceptions;
using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using AddressCorrection.src.AddressCorrection.Application.Prompts;
using AddressCorrection.src.AddressCorrection.Domain.Constants;

namespace AddressCorrection.src.AddressCorrection.Application.Pipeline.Steps;

/// <summary>
/// Étape 2 : Appel LLM avec fallback multi-modèles.
/// Ignorée si le résultat est déjà disponible (cache hit).
/// En cas d'échec de tous les modèles, enregistre l'erreur dans le contexte.
/// </summary>
public class LlmProcessingStep : ICorrectionStep
{
    private readonly ILlmOrchestrator _llmOrchestrator;
    private readonly ILogger<LlmProcessingStep> _logger;

    public LlmProcessingStep(ILlmOrchestrator llmOrchestrator, ILogger<LlmProcessingStep> logger)
    {
        _llmOrchestrator = llmOrchestrator;
        _logger = logger;
    }

    public async Task<AddressCorrectionContext> ExecuteAsync(AddressCorrectionContext context)
    {
        if (context.Result != null) return context;

        var prompt = AddressCorrectionPrompt.Build(context.Request.RawAddress);
        var (result, modelUsed) = await _llmOrchestrator.CompleteWithFallbackAsync(
            prompt, context.Request.RawAddress);

        if (result == null)
        {
            // Sanitize user-provided address before logging to prevent log forging
            var sanitizedAddress = context.Request.RawAddress.Replace("\r", "").Replace("\n", "");
            _logger.LogError("All LLM models failed for address: {Address}", sanitizedAddress);
            context.Status = CorrectionConstants.Status.Failed;
            context.Error = new AllModelsFailedException();
            context.Stopwatch.Stop();
            return context;
        }

        _logger.LogInformation("LLM processing succeeded with model {Model} in {Duration}ms",
            modelUsed, context.Stopwatch.ElapsedMilliseconds);
        context.Result = result;
        context.ModelUsed = modelUsed;
        context.Status = CorrectionConstants.Status.Success;
        return context;
    }
}
