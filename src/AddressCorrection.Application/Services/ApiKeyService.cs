using System.Security.Cryptography;
using System.Text;
using AddressCorrection.src.AddressCorrection.Application.DTOs;
using AddressCorrection.src.AddressCorrection.Application.Exceptions;
using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using AddressCorrection.src.AddressCorrection.Application.Mappers;
using AddressCorrection.src.AddressCorrection.Domain.Constants;
using AddressCorrection.src.AddressCorrection.Domain.Entities;

namespace AddressCorrection.src.AddressCorrection.Application.Services;

/// <summary>
/// Gestion du cycle de vie des clés API : génération sécurisée, validation, révocation.
///
/// Stratégie de sécurité :
///   - La clé en clair est générée avec RandomNumberGenerator (CSPRNG).
///   - Elle est hachée avec PBKDF2-SHA256 + sel aléatoire avant stockage.
///   - La clé en clair n'est JAMAIS persistée ni loguée.
///   - La validation compare le hash de la clé fournie avec le hash stocké.
/// </summary>
public sealed class ApiKeyService : IApiKeyService
{
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly IIntegrationRepository _integrationRepository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ApiKeyService> _logger;

    public ApiKeyService(
        IApiKeyRepository apiKeyRepository,
        IIntegrationRepository integrationRepository,
        TimeProvider timeProvider,
        ILogger<ApiKeyService> logger)
    {
        _apiKeyRepository = apiKeyRepository;
        _integrationRepository = integrationRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    // ── GenerateAsync ─────────────────────────────────────────────────────────
    public async Task<ApiKeyCreatedDto> GenerateAsync(
        string integrationId,
        string clientId,
        GenerateApiKeyRequest request,
        CancellationToken ct = default)
    {
        // Validation des scopes demandés
        var invalidScopes = request.Scopes
            .Where(s => !IntegrationConstants.Scope.All.Contains(s))
            .ToList();
        if (invalidScopes.Count > 0)
            throw new InvalidApiKeyScopeException(invalidScopes);

        // Vérification de l'intégration
        var integration = await _integrationRepository.GetByIdAsync(integrationId, ct)
            ?? throw new IntegrationNotFoundException(integrationId);

        if (integration.ClientId != clientId)
            throw new UnauthorizedIntegrationAccessException(integrationId, clientId);

        // Vérification de la limite de clés actives
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var activeCount = await _apiKeyRepository.CountActiveAsync(integrationId, now, ct);
        if (activeCount >= IntegrationConstants.Limits.MaxActiveKeysPerIntegration)
            throw new ApiKeyLimitExceededException(integrationId, IntegrationConstants.Limits.MaxActiveKeysPerIntegration);

        // Génération cryptographique de la clé
        var (rawKey, prefix, hashedKey, salt) = GenerateSecureKey();

        var effectiveScopes = request.Scopes.Count > 0
            ? request.Scopes
            : [IntegrationConstants.Scope.FullAccess];

        var apiKey = new ApiKey
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            IntegrationId = integrationId,
            ClientId = clientId,
            Name = request.Name.Trim(),
            Prefix = prefix,
            HashedKey = hashedKey,
            Salt = salt,
            Scopes = effectiveScopes,
            ExpiresAt = request.ExpiresAt,
            CreatedAt = now,
        };

        await _apiKeyRepository.CreateAsync(apiKey, ct);

        _logger.LogInformation(
            "API key generated: prefix={Prefix}, integration={IntegrationId}",
            prefix, integrationId);

        return new ApiKeyCreatedDto
        {
            Id = apiKey.Id,
            Name = apiKey.Name,
            Key = rawKey,   // seule et unique fois où la clé brute est retournée
            Prefix = prefix,
            Scopes = effectiveScopes,
            ExpiresAt = request.ExpiresAt,
            CreatedAt = now,
        };
    }

    // ── ListAsync ─────────────────────────────────────────────────────────────
    public async Task<IReadOnlyList<ApiKeyDto>> ListAsync(
        string integrationId,
        string clientId,
        CancellationToken ct = default)
    {
        var integration = await _integrationRepository.GetByIdAsync(integrationId, ct)
            ?? throw new IntegrationNotFoundException(integrationId);

        if (integration.ClientId != clientId)
            throw new UnauthorizedIntegrationAccessException(integrationId, clientId);

        var keys = await _apiKeyRepository.GetByIntegrationIdAsync(integrationId, ct);
        return keys.Select(ApiKeyMapper.ToDto).ToList().AsReadOnly();
    }

    // ── RevokeAsync ───────────────────────────────────────────────────────────
    public async Task RevokeAsync(
        string keyId,
        string integrationId,
        string clientId,
        CancellationToken ct = default)
    {
        var key = await _apiKeyRepository.GetByIdAsync(keyId, ct)
            ?? throw new ApiKeyNotFoundException(keyId);

        if (key.IntegrationId != integrationId || key.ClientId != clientId)
            throw new UnauthorizedIntegrationAccessException(integrationId, clientId);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        await _apiKeyRepository.RevokeAsync(keyId, now, ct);

        _logger.LogInformation("API key revoked: {KeyId}, prefix={Prefix}", keyId, key.Prefix);
    }

    // ── ValidateAsync ─────────────────────────────────────────────────────────
    public async Task<ApiKeyValidationResult?> ValidateAsync(
        string rawApiKey,
        CancellationToken ct = default)
    {
        // Extraction du préfixe pour le lookup initial (évite un full-scan)
        if (string.IsNullOrWhiteSpace(rawApiKey) || rawApiKey.Length < 12)
            return null;

        var prefix = rawApiKey[..10];   // sk_live_XX
        var keyWithHash = await _apiKeyRepository.GetWithHashByPrefixAsync(prefix, ct);
        if (keyWithHash is null)
            return null;

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (!keyWithHash.IsActive(now))
            return null;

        // Comparaison PBKDF2 (constant-time via CryptographicOperations)
        var computedHash = ComputeHash(rawApiKey, keyWithHash.Salt);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedHash),
                Encoding.UTF8.GetBytes(keyWithHash.HashedKey)))
            return null;

        // Mise à jour asynchrone non-bloquante du dernier usage
        _ = _apiKeyRepository.RecordUsageAsync(keyWithHash.Id, now, ct);

        _logger.LogDebug("API key validated: prefix={Prefix}", prefix);

        return new ApiKeyValidationResult
        {
            KeyId = keyWithHash.Id,
            IntegrationId = keyWithHash.IntegrationId,
            ClientId = keyWithHash.ClientId,
            Scopes = keyWithHash.Scopes,
        };
    }

    // ── Private: cryptographic helpers ───────────────────────────────────────

    /// <summary>
    /// Génère une clé aléatoire sécurisée, son préfixe lisible, son hash et son sel.
    /// Format de la clé : sk_live_ + 43 chars base64url (256 bits d'entropie).
    /// </summary>
    private static (string rawKey, string prefix, string hashedKey, string salt) GenerateSecureKey()
    {
        var randomBytes = new byte[IntegrationConstants.Limits.KeyRandomByteLength];
        RandomNumberGenerator.Fill(randomBytes);

        var rawKey = IntegrationConstants.KeyPrefix.Live
                   + Convert.ToBase64String(randomBytes)
                       .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        var prefix = rawKey[..10];

        var saltBytes = new byte[16];
        RandomNumberGenerator.Fill(saltBytes);
        var salt = Convert.ToBase64String(saltBytes);

        var hashedKey = ComputeHash(rawKey, salt);

        return (rawKey, prefix, hashedKey, salt);
    }

    /// <summary>
    /// Calcule le hash PBKDF2-SHA256 de la clé avec le sel fourni.
    /// Résultat encodé en base64 pour le stockage.
    /// </summary>
    private static string ComputeHash(string rawKey, string salt)
    {
        var keyBytes = Encoding.UTF8.GetBytes(rawKey);
        var saltBytes = Convert.FromBase64String(salt);

        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            keyBytes,
            saltBytes,
            IntegrationConstants.Limits.Pbkdf2Iterations,
            HashAlgorithmName.SHA256,
            outputLength: 32);

        return Convert.ToBase64String(hashBytes);
    }
}