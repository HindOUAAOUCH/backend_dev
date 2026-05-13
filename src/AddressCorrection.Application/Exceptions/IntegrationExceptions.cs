namespace AddressCorrection.src.AddressCorrection.Application.Exceptions;

// ── Intégrations ─────────────────────────────────────────────────────────────

/// <summary>
/// Levée quand une intégration référencée n'existe pas ou a été supprimée.
/// Traduite en HTTP 404 par le controller.
/// </summary>
public sealed class IntegrationNotFoundException(string integrationId)
    : Exception($"Integration '{integrationId}' was not found or has been deleted.");

/// <summary>
/// Levée quand une opération tente d'accéder à une intégration
/// qui n'appartient pas au client authentifié.
/// Traduite en HTTP 403 par le controller.
/// </summary>
public sealed class UnauthorizedIntegrationAccessException(string integrationId, string clientId)
    : Exception($"Client '{clientId}' is not authorized to access integration '{integrationId}'.");

/// <summary>
/// Levée quand un client tente de créer plus d'intégrations que son plan ne le permet.
/// Traduite en HTTP 422 par le controller.
/// </summary>
public sealed class IntegrationLimitExceededException(string clientId, int limit)
    : Exception($"Client '{clientId}' has reached the maximum number of integrations ({limit}).");

// ── Clés API ──────────────────────────────────────────────────────────────────

/// <summary>
/// Levée quand une clé API référencée n'existe pas ou a été révoquée.
/// Traduite en HTTP 404 par le controller.
/// </summary>
public sealed class ApiKeyNotFoundException(string keyId)
    : Exception($"API key '{keyId}' was not found.");

/// <summary>
/// Levée quand une intégration a déjà atteint le nombre maximum de clés actives.
/// Traduite en HTTP 422 par le controller.
/// </summary>
public sealed class ApiKeyLimitExceededException(string integrationId, int limit)
    : Exception($"Integration '{integrationId}' has reached the maximum number of active API keys ({limit}).");

/// <summary>
/// Levée lors d'une tentative de génération de clé avec des scopes invalides.
/// Traduite en HTTP 400 par le controller.
/// </summary>
public sealed class InvalidApiKeyScopeException(IEnumerable<string> invalidScopes)
    : Exception($"The following scopes are not valid: {string.Join(", ", invalidScopes)}.");

// ── Client (réutilisé depuis Phase 1 si elle existait) ────────────────────────

/// <summary>
/// Levée quand un clientId référencé n'existe pas.
/// Traduite en HTTP 404 par le controller.
/// </summary>
public sealed class ClientNotFoundException(string clientId)
    : Exception($"Client '{clientId}' was not found.");