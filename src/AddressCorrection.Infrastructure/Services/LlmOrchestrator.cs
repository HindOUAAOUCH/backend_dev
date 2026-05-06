using AddressCorrection.src.AddressCorrection.Application.Configuration;
using AddressCorrection.src.AddressCorrection.Application.DTOs;
using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using AddressCorrection.src.AddressCorrection.Infrastructure.Policies;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AddressCorrection.src.AddressCorrection.Infrastructure.Services;

public class LlmOrchestrator : ILlmOrchestrator
{
    private readonly ILlmClient _llmClient;
    private readonly GitHubModelsConfig _config;
    private readonly IActiveLlmModelProvider _modelProvider;
    private readonly ILogger<LlmOrchestrator> _logger;

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public LlmOrchestrator(
        ILlmClient llmClient,
        IOptions<GitHubModelsConfig> config,
        IActiveLlmModelProvider modelProvider,
        ILogger<LlmOrchestrator> logger)
    {
        _llmClient = llmClient;
        _config = config.Value;
        _modelProvider = modelProvider;
        _logger = logger;
    }

    public async Task<(AddressResponse? Result, string? ModelUsed)> CompleteWithFallbackAsync(
        string prompt, string rawAddress)
    {
        foreach (var modelName in BuildModelsList())
        {
            try
            {
                var policy = LlmResiliencePolicy.Build(_logger, modelName);
                var responseText = await policy.ExecuteAsync(() =>
                    _llmClient.CompleteAsync(prompt, rawAddress, modelName));

                var result = JsonSerializer.Deserialize<AddressResponse>(responseText, JsonOptions);
                if (result == null) continue;

                _logger.LogInformation("LLM success with model {Model}", modelName);
                return (result, modelName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Model {Model} failed: {Error}", modelName, ex.Message);
            }
        }

        return (null, null);
    }

    private List<string> BuildModelsList()
    {
        var selectedModel = _modelProvider.GetSelectedModel();
        var modelsToTry = new List<string>();

        if (selectedModel != null)
            modelsToTry.Add(selectedModel);

        modelsToTry.AddRange(_config.Models
            .Where(m => m.Name != selectedModel)
            .OrderBy(m => m.Priority)
            .Select(m => m.Name));

        return modelsToTry;
    }
}
