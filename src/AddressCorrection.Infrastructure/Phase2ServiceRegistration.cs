using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using AddressCorrection.src.AddressCorrection.Application.Services;
using AddressCorrection.src.AddressCorrection.Infrastructure.Persistence;
using AddressCorrection.src.AddressCorrection.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AddressCorrection.src.AddressCorrection.Infrastructure;

/// <summary>
/// Extension methods pour enregistrer tous les services de la Phase 2
/// dans le conteneur DI depuis Program.cs.
///
/// Usage dans Program.cs :
/// <code>
/// builder.Services.AddPhase2IntegrationsFeature();
/// </code>
/// Et pour le middleware, après app.Build() :
/// <code>
/// app.UseApiKeyAuthentication();
/// </code>
/// </summary>
public static class Phase2ServiceRegistration
{
    /// <summary>
    /// Enregistre les repositories, services applicatifs et clients HTTP
    /// de la feature Intégrations &amp; API Keys.
    /// </summary>
    public static IServiceCollection AddPhase2IntegrationsFeature(
        this IServiceCollection services)
    {


        // ── Repositories ──────────────────────────────────────────────────────────
        services.AddScoped<IIntegrationRepository, IntegrationRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();

        // ── TimeProvider (.NET 8+) ─────────────────────────────────────────────────
        services.AddSingleton(TimeProvider.System);   // ← ajoute cette ligne

        // ── Services applicatifs ───────────────────────────────────────────────────
        services.AddScoped<IIntegrationService, IntegrationService>();
        services.AddScoped<IApiKeyService, ApiKeyService>();
        services.AddScoped<IWebhookDispatcher, WebhookDispatcher>();


        // ── TimeProvider (.NET 8+) — injecté dans les services pour testabilité ─
        // Si déjà enregistré par une autre feature, cette ligne est idempotente.
        // TimeProvider is handled by .NET runtime or injected elsewhere
        // ── HttpClient nommé pour les webhooks ─────────────────────────────────
        services.AddHttpClient("Webhook", client =>
        {
            // Timeout court : les webhooks sont fire-and-forget,
            // on ne bloque pas la correction principale plus de 5 secondes.
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        return services;
    }

    /// <summary>
    /// Branche le middleware d'authentification par clé API dans le pipeline HTTP.
    /// À appeler APRÈS UseRouting() et AVANT UseAuthorization() dans Program.cs.
    /// </summary>
    public static IApplicationBuilder UseApiKeyAuthentication(
        this IApplicationBuilder app)
    {
        return app.UseMiddleware<API.Middleware.ApiKeyAuthenticationMiddleware>();
    }
}