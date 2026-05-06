using AddressCorrection.src.AddressCorrection.Application.Configuration;
using AddressCorrection.src.AddressCorrection.Application.Exceptions;
using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace AddressCorrection.src.AddressCorrection.Infrastructure.LLMClients;

public class GitHubLlmClient : ILlmClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GitHubLlmClient> _logger;
    private readonly GitHubModelsConfig _config;

    public GitHubLlmClient(
        IHttpClientFactory httpClientFactory,
        IOptions<GitHubModelsConfig> config,
        ILogger<GitHubLlmClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userMessage, string modelName)
    {
        var token = ResolveToken(modelName);

        _logger.LogDebug("Calling model {Model} with token ending in ...{Suffix}",
            modelName, token.Length > 4 ? token[^4..] : "????");

        var httpClient = _httpClientFactory.CreateClient("GitHubModels");
        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var requestBody = new
        {
            model = modelName,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userMessage  }
            }
        };

        var json    = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("/chat/completions", content);

        // Erreurs non-retriables : authentification (401/403) et rate limit (429)
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
            response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new LlmAuthenticationException(modelName, (int)response.StatusCode);

        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            throw new LlmRateLimitException(modelName);

        if (!response.IsSuccessStatusCode)
            throw new LlmClientException(
                $"HTTP {(int)response.StatusCode} from model '{modelName}' — {response.ReasonPhrase}",
                (int)response.StatusCode);

        var responseJson = await response.Content.ReadAsStringAsync();

        JsonElement root;
        try
        {
            root = JsonDocument.Parse(responseJson).RootElement;
        }
        catch (JsonException ex)
        {
            throw new LlmClientException($"Invalid JSON response from model '{modelName}'.", ex);
        }

        var responseContent = root
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(responseContent))
            throw new LlmEmptyResponseException(modelName);

        // Nettoyer les éventuels blocs markdown renvoyés par certains modèles
        return responseContent
            .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
            .Replace("```", "")
            .Trim();
    }

    /// <summary>
    /// Résout le token à utiliser : token spécifique au modèle en priorité,
    /// sinon token global. Lève une exception claire si aucun token n'est configuré.
    /// </summary>
    private string ResolveToken(string modelName)
    {
        var modelConfig = _config.Models.FirstOrDefault(m => m.Name == modelName);

        var token = !string.IsNullOrWhiteSpace(modelConfig?.Token)
            ? modelConfig.Token
            : _config.Token;

        if (string.IsNullOrWhiteSpace(token))
            throw new LlmAuthenticationException(modelName, 0);

        return token;
    }
}
