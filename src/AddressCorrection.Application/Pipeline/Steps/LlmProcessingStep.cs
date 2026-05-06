using AddressCorrection.src.AddressCorrection.Application.Exceptions;
using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using AddressCorrection.src.AddressCorrection.Application.Prompts;
using AddressCorrection.src.AddressCorrection.Domain.Constants;

namespace AddressCorrection.src.AddressCorrection.Application.Pipeline.Steps;

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
        if (context.Result != null) return context; // Cache hit — skip LLM processing

        var prompt = AddressCorrectionPrompt.Build(context.Request.RawAddress);
        var (result, modelUsed) = await _llmOrchestrator.CompleteWithFallbackAsync(
            prompt, context.Request.RawAddress);

        if (result == null)
        {
            context.Error = new AllModelsFailedException();
            context.Status = CorrectionConstants.Status.Failed;
            context.ModelUsed = CorrectionConstants.Model.None;
            return context;
        }

        context.Result = result;
        context.ModelUsed = modelUsed;
        context.Status = CorrectionConstants.Status.Success;
        _logger.LogInformation("LLM processing succeeded with model {Model}", modelUsed);

        return context;
    }
}
