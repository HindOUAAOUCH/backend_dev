using AddressCorrection.src.AddressCorrection.Application.Interfaces;

namespace AddressCorrection.src.AddressCorrection.API.Middleware;

/// <summary>
/// Middleware d'authentification par clé API.
///
/// Comportement :
///   - Lit le header HTTP "X-Api-Key".
///   - Si absent ou vide : laisse passer la requête (auth optionnelle — sera
///     rendue obligatoire en Phase 3 via une policy d'autorisation).
///   - Si présent : valide via IApiKeyService.ValidateAsync().
///     - Clé valide  → injecte ClientId, IntegrationId et Scopes dans HttpContext.Items.
///     - Clé invalide → retourne HTTP 401 immédiatement sans appeler le handler suivant.
///
/// Pourquoi un middleware et pas un AuthenticationHandler ?
///   En Phase 2 (sans JWT), un middleware simple suffit.
///   En Phase 3, ce middleware sera remplacé par un AuthenticationHandler
///   branché sur AddAuthentication() pour supporter les deux schémas (JWT + ApiKey).
/// </summary>
public sealed class ApiKeyAuthenticationMiddleware
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

    public ApiKeyAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IApiKeyService apiKeyService)
    {
        var rawKey = context.Request.Headers[ApiKeyHeaderName].FirstOrDefault();

        // Pas de clé dans le header → pass-through (non authentifié)
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            await _next(context);
            return;
        }

        // Clé présente → validation
        var validationResult = await apiKeyService.ValidateAsync(rawKey, context.RequestAborted);

        if (validationResult is null)
        {
            _logger.LogWarning(
                "Invalid API key attempt from {IP} on {Path}",
                context.Connection.RemoteIpAddress,
                context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                """{"status":401,"title":"Unauthorized","detail":"Invalid or expired API key."}""",
                context.RequestAborted);
            return;
        }

        // Clé valide → injection du contexte d'auth dans HttpContext.Items
        // Ces valeurs sont lues par les controllers via GetClientId() et GetIntegrationId()
        context.Items["ClientId"] = validationResult.ClientId;
        context.Items["IntegrationId"] = validationResult.IntegrationId;
        context.Items["ApiKeyId"] = validationResult.KeyId;
        context.Items["Scopes"] = validationResult.Scopes;

        _logger.LogDebug(
            "API key authenticated: integration={IntegrationId}, client={ClientId}",
            validationResult.IntegrationId,
            validationResult.ClientId);

        await _next(context);
    }
}