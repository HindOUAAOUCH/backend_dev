using AddressCorrection.src.AddressCorrection.Application.DTOs;
using AddressCorrection.src.AddressCorrection.Application.Exceptions;
using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using AddressCorrection.src.AddressCorrection.Application.Mappers;
using AddressCorrection.src.AddressCorrection.Domain.Constants;
using AddressCorrection.src.AddressCorrection.Domain.Entities;
using AddressCorrection.src.AddressCorrection.Domain.Enums;

namespace AddressCorrection.src.AddressCorrection.Application.Services;

/// <summary>
/// Implémente les cas d'utilisation de gestion des intégrations e-commerce.
/// Toute la logique métier réside ici — les controllers ne font que déléguer.
/// </summary>
public sealed class IntegrationService : IIntegrationService
{
    private readonly IIntegrationRepository _integrationRepository;
    private readonly IApiKeyService _apiKeyService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<IntegrationService> _logger;

    public IntegrationService(
        IIntegrationRepository integrationRepository,
        IApiKeyService apiKeyService,
        TimeProvider timeProvider,
        ILogger<IntegrationService> logger)
    {
        _integrationRepository = integrationRepository;
        _apiKeyService = apiKeyService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────
    public async Task<IntegrationDto> CreateAsync(
        string clientId,
        CreateIntegrationRequest request,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        var existingCount = await _integrationRepository.GetByClientIdAsync(clientId, ct: ct);
        if (existingCount.Count >= IntegrationConstants.Limits.MaxIntegrationsPerClient)
            throw new IntegrationLimitExceededException(clientId, IntegrationConstants.Limits.MaxIntegrationsPerClient);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var integration = new Integration
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            ClientId = clientId,
            Name = request.Name.Trim(),
            Platform = request.Platform.ToLowerInvariant().Trim(),
            WebhookUrl = string.IsNullOrWhiteSpace(request.WebhookUrl) ? null : request.WebhookUrl.Trim(),
            Status = IntegrationStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await _integrationRepository.CreateAsync(integration, ct);

        _logger.LogInformation(
            "Integration created: {IntegrationId} for client {ClientId} on platform {Platform}",
            integration.Id, clientId, integration.Platform);

        return IntegrationMapper.ToDto(integration);
    }

    // ── GetByClientAsync ──────────────────────────────────────────────────────
    public async Task<IReadOnlyList<IntegrationDto>> GetByClientAsync(
        string clientId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        var integrations = await _integrationRepository.GetByClientIdAsync(clientId, ct: ct);
        return integrations.Select(IntegrationMapper.ToDto).ToList().AsReadOnly();
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────
    public async Task<IntegrationDto> GetByIdAsync(
        string integrationId,
        string clientId,
        CancellationToken ct = default)
    {
        var integration = await ResolveAndAuthorizeAsync(integrationId, clientId, ct);
        return IntegrationMapper.ToDto(integration);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────
    public async Task<IntegrationDto> UpdateAsync(
        string integrationId,
        string clientId,
        UpdateIntegrationRequest request,
        CancellationToken ct = default)
    {
        var integration = await ResolveAndAuthorizeAsync(integrationId, clientId, ct);

        if (request.Name is not null)
            integration.Name = request.Name.Trim();

        if (request.WebhookUrl is not null)
            integration.WebhookUrl = string.IsNullOrWhiteSpace(request.WebhookUrl)
                ? null
                : request.WebhookUrl.Trim();

        integration.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;

        await _integrationRepository.UpdateAsync(integration, ct);

        _logger.LogInformation("Integration updated: {IntegrationId}", integrationId);
        return IntegrationMapper.ToDto(integration);
    }

    // ── PauseAsync ────────────────────────────────────────────────────────────
    public async Task PauseAsync(
        string integrationId,
        string clientId,
        CancellationToken ct = default)
    {
        var integration = await ResolveAndAuthorizeAsync(integrationId, clientId, ct);
        integration.Status = IntegrationStatus.Paused;
        integration.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
        await _integrationRepository.UpdateAsync(integration, ct);

        _logger.LogInformation("Integration paused: {IntegrationId}", integrationId);
    }

    // ── ResumeAsync ───────────────────────────────────────────────────────────
    public async Task ResumeAsync(
        string integrationId,
        string clientId,
        CancellationToken ct = default)
    {
        var integration = await ResolveAndAuthorizeAsync(integrationId, clientId, ct);
        integration.Status = IntegrationStatus.Active;
        integration.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
        await _integrationRepository.UpdateAsync(integration, ct);

        _logger.LogInformation("Integration resumed: {IntegrationId}", integrationId);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────
    public async Task DeleteAsync(
        string integrationId,
        string clientId,
        CancellationToken ct = default)
    {
        var integration = await ResolveAndAuthorizeAsync(integrationId, clientId, ct);

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // Révocation de toutes les clés API associées avant suppression
        var keys = await _apiKeyService.ListAsync(integrationId, clientId, ct);
        foreach (var key in keys.Where(k => !k.IsRevoked))
            await _apiKeyService.RevokeAsync(key.Id, integrationId, clientId, ct);

        await _integrationRepository.SoftDeleteAsync(integrationId, now, ct);

        _logger.LogInformation(
            "Integration deleted: {IntegrationId}, {KeyCount} key(s) revoked",
            integrationId, keys.Count);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Résout une intégration par son ID et vérifie qu'elle appartient au client.
    /// Lève les exceptions métier appropriées si l'intégration est introuvable ou non autorisée.
    /// </summary>
    private async Task<Integration> ResolveAndAuthorizeAsync(
        string integrationId,
        string clientId,
        CancellationToken ct)
    {
        var integration = await _integrationRepository.GetByIdAsync(integrationId, ct)
            ?? throw new IntegrationNotFoundException(integrationId);

        if (integration.ClientId != clientId)
            throw new UnauthorizedIntegrationAccessException(integrationId, clientId);

        return integration;
    }
}