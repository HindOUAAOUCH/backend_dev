using System.Text;
using System.Text.Json;
using AddressCorrection.src.AddressCorrection.Application.DTOs;
using AddressCorrection.src.AddressCorrection.Application.Interfaces;

namespace AddressCorrection.src.AddressCorrection.Infrastructure.Services;

/// <summary>
/// Envoie des notifications POST en JSON vers les URLs de webhook configurées.
///
/// Comportement :
///   - Fire-and-forget : les erreurs sont loguées mais ne font JAMAIS échouer la correction.
///   - Timeout court (5 secondes) pour éviter de bloquer le thread.
///   - Pas de retry : la responsabilité de la réception incombe au destinataire.
///   - Un header X-Webhook-Event identifie le type d'événement.
/// </summary>
public sealed class WebhookDispatcher : IWebhookDispatcher
{
    private readonly IIntegrationRepository _integrationRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookDispatcher> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public WebhookDispatcher(
        IIntegrationRepository integrationRepository,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookDispatcher> logger)
    {
        _integrationRepository = integrationRepository;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task DispatchAsync(
        string integrationId,
        WebhookPayload payload,
        CancellationToken ct = default)
    {
        var integration = await _integrationRepository.GetByIdAsync(integrationId, ct);

        // Aucun webhook configuré ou intégration introuvable → skip silencieux
        if (integration is null || string.IsNullOrWhiteSpace(integration.WebhookUrl))
            return;

        try
        {
            var httpClient = _httpClientFactory.CreateClient("Webhook");
            var json = JsonSerializer.Serialize(payload, JsonOptions);

            using var request = new HttpRequestMessage(HttpMethod.Post, integration.WebhookUrl);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            request.Headers.Add("X-Webhook-Event", payload.EventType);
            request.Headers.Add("X-Integration-Id", integrationId);

            using var response = await httpClient.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Webhook delivery failed for integration {IntegrationId}: HTTP {StatusCode} from {Url}",
                    integrationId, (int)response.StatusCode, integration.WebhookUrl);
            }
            else
            {
                _logger.LogDebug(
                    "Webhook delivered for integration {IntegrationId}, event={EventType}",
                    integrationId, payload.EventType);
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning(
                "Webhook timed out for integration {IntegrationId} at {Url}",
                integrationId, integration.WebhookUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Webhook dispatch error for integration {IntegrationId} at {Url}",
                integrationId, integration.WebhookUrl);
        }
    }
}