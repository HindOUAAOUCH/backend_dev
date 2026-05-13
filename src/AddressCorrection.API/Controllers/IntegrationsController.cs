using AddressCorrection.src.AddressCorrection.Application.DTOs;
using AddressCorrection.src.AddressCorrection.Application.Exceptions;
using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AddressCorrection.src.AddressCorrection.API.Controllers;

/// <summary>
/// Gestion des intégrations e-commerce et de leurs clés API.
///
/// Toutes les routes supposent que le clientId est connu du contexte HTTP.
/// En Phase 2 (sans auth JWT), le clientId est passé via le header X-Client-Id.
/// En Phase 3 (avec auth JWT), il sera extrait du claim directement.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class IntegrationsController : ControllerBase
{
    private readonly IIntegrationService _integrationService;
    private readonly IApiKeyService _apiKeyService;
    private readonly ILogger<IntegrationsController> _logger;

    public IntegrationsController(
        IIntegrationService integrationService,
        IApiKeyService apiKeyService,
        ILogger<IntegrationsController> logger)
    {
        _integrationService = integrationService;
        _apiKeyService = apiKeyService;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // INTÉGRATIONS
    // ═══════════════════════════════════════════════════════════════════════════

    // ── POST /api/integrations ────────────────────────────────────────────────
    /// <summary>
    /// Crée une nouvelle intégration e-commerce pour le client authentifié.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(IntegrationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<IntegrationDto>> Create(
        [FromBody] CreateIntegrationRequest request,
        CancellationToken ct)
    {
        var clientId = GetClientId();
        if (clientId is null)
            return BadRequest(Problem(detail: "Header X-Client-Id is required.", statusCode: 400));

        try
        {
            var dto = await _integrationService.CreateAsync(clientId, request, ct);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }
        catch (ClientNotFoundException ex)
        {
            return NotFound(Problem(detail: ex.Message, statusCode: 404));
        }
        catch (IntegrationLimitExceededException ex)
        {
            return UnprocessableEntity(Problem(detail: ex.Message, statusCode: 422));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating integration for client {ClientId}", clientId);
            return StatusCode(500, Problem(detail: "An unexpected error occurred.", statusCode: 500));
        }
    }

    // ── GET /api/integrations ─────────────────────────────────────────────────
    /// <summary>
    /// Retourne toutes les intégrations actives du client authentifié.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<IntegrationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<IntegrationDto>>> GetAll(CancellationToken ct)
    {
        var clientId = GetClientId();
        if (clientId is null)
            return BadRequest(Problem(detail: "Header X-Client-Id is required.", statusCode: 400));

        try
        {
            var integrations = await _integrationService.GetByClientAsync(clientId, ct);
            return Ok(integrations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching integrations for client {ClientId}", clientId);
            return StatusCode(500, Problem(detail: "An unexpected error occurred.", statusCode: 500));
        }
    }

    // ── GET /api/integrations/{id} ────────────────────────────────────────────
    /// <summary>
    /// Retourne le détail d'une intégration.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(IntegrationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IntegrationDto>> GetById(string id, CancellationToken ct)
    {
        var clientId = GetClientId();
        if (clientId is null)
            return BadRequest(Problem(detail: "Header X-Client-Id is required.", statusCode: 400));

        try
        {
            var dto = await _integrationService.GetByIdAsync(id, clientId, ct);
            return Ok(dto);
        }
        catch (IntegrationNotFoundException ex)
        {
            return NotFound(Problem(detail: ex.Message, statusCode: 404));
        }
        catch (UnauthorizedIntegrationAccessException ex)
        {
            return StatusCode(403, Problem(detail: ex.Message, statusCode: 403));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching integration {IntegrationId}", id);
            return StatusCode(500, Problem(detail: "An unexpected error occurred.", statusCode: 500));
        }
    }

    // ── PUT /api/integrations/{id} ────────────────────────────────────────────
    /// <summary>
    /// Met à jour le nom ou l'URL webhook d'une intégration.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(IntegrationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IntegrationDto>> Update(
        string id,
        [FromBody] UpdateIntegrationRequest request,
        CancellationToken ct)
    {
        var clientId = GetClientId();
        if (clientId is null)
            return BadRequest(Problem(detail: "Header X-Client-Id is required.", statusCode: 400));

        try
        {
            var dto = await _integrationService.UpdateAsync(id, clientId, request, ct);
            return Ok(dto);
        }
        catch (IntegrationNotFoundException ex)
        {
            return NotFound(Problem(detail: ex.Message, statusCode: 404));
        }
        catch (UnauthorizedIntegrationAccessException ex)
        {
            return StatusCode(403, Problem(detail: ex.Message, statusCode: 403));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating integration {IntegrationId}", id);
            return StatusCode(500, Problem(detail: "An unexpected error occurred.", statusCode: 500));
        }
    }

    // ── POST /api/integrations/{id}/pause ────────────────────────────────────
    /// <summary>
    /// Suspend temporairement une intégration.
    /// Les clés API restent valides mais les requêtes de correction sont rejetées.
    /// </summary>
    [HttpPost("{id}/pause")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Pause(string id, CancellationToken ct)
    {
        var clientId = GetClientId();
        if (clientId is null)
            return BadRequest(Problem(detail: "Header X-Client-Id is required.", statusCode: 400));

        try
        {
            await _integrationService.PauseAsync(id, clientId, ct);
            return NoContent();
        }
        catch (IntegrationNotFoundException ex)
        {
            return NotFound(Problem(detail: ex.Message, statusCode: 404));
        }
        catch (UnauthorizedIntegrationAccessException ex)
        {
            return StatusCode(403, Problem(detail: ex.Message, statusCode: 403));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error pausing integration {IntegrationId}", id);
            return StatusCode(500, Problem(detail: "An unexpected error occurred.", statusCode: 500));
        }
    }

    // ── POST /api/integrations/{id}/resume ───────────────────────────────────
    /// <summary>
    /// Réactive une intégration suspendue.
    /// </summary>
    [HttpPost("{id}/resume")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Resume(string id, CancellationToken ct)
    {
        var clientId = GetClientId();
        if (clientId is null)
            return BadRequest(Problem(detail: "Header X-Client-Id is required.", statusCode: 400));

        try
        {
            await _integrationService.ResumeAsync(id, clientId, ct);
            return NoContent();
        }
        catch (IntegrationNotFoundException ex)
        {
            return NotFound(Problem(detail: ex.Message, statusCode: 404));
        }
        catch (UnauthorizedIntegrationAccessException ex)
        {
            return StatusCode(403, Problem(detail: ex.Message, statusCode: 403));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error resuming integration {IntegrationId}", id);
            return StatusCode(500, Problem(detail: "An unexpected error occurred.", statusCode: 500));
        }
    }

    // ── DELETE /api/integrations/{id} ─────────────────────────────────────────
    /// <summary>
    /// Supprime définitivement une intégration (soft-delete).
    /// Révoque automatiquement toutes les clés API associées.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var clientId = GetClientId();
        if (clientId is null)
            return BadRequest(Problem(detail: "Header X-Client-Id is required.", statusCode: 400));

        try
        {
            await _integrationService.DeleteAsync(id, clientId, ct);
            return NoContent();
        }
        catch (IntegrationNotFoundException ex)
        {
            return NotFound(Problem(detail: ex.Message, statusCode: 404));
        }
        catch (UnauthorizedIntegrationAccessException ex)
        {
            return StatusCode(403, Problem(detail: ex.Message, statusCode: 403));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting integration {IntegrationId}", id);
            return StatusCode(500, Problem(detail: "An unexpected error occurred.", statusCode: 500));
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CLÉS API
    // ═══════════════════════════════════════════════════════════════════════════

    // ── GET /api/integrations/{id}/keys ──────────────────────────────────────
    /// <summary>
    /// Liste les clés API d'une intégration.
    /// Ne retourne jamais la clé en clair, uniquement le préfixe et les métadonnées.
    /// </summary>
    [HttpGet("{id}/keys")]
    [ProducesResponseType(typeof(IReadOnlyList<ApiKeyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ApiKeyDto>>> ListKeys(string id, CancellationToken ct)
    {
        var clientId = GetClientId();
        if (clientId is null)
            return BadRequest(Problem(detail: "Header X-Client-Id is required.", statusCode: 400));

        try
        {
            var keys = await _apiKeyService.ListAsync(id, clientId, ct);
            return Ok(keys);
        }
        catch (IntegrationNotFoundException ex)
        {
            return NotFound(Problem(detail: ex.Message, statusCode: 404));
        }
        catch (UnauthorizedIntegrationAccessException ex)
        {
            return StatusCode(403, Problem(detail: ex.Message, statusCode: 403));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error listing keys for integration {IntegrationId}", id);
            return StatusCode(500, Problem(detail: "An unexpected error occurred.", statusCode: 500));
        }
    }

    // ── POST /api/integrations/{id}/keys ─────────────────────────────────────
    /// <summary>
    /// Génère une nouvelle clé API pour l'intégration.
    /// ⚠️ La clé en clair n'est retournée qu'une seule fois dans cette réponse.
    ///    Elle ne sera plus jamais accessible. Le client doit la copier immédiatement.
    /// </summary>
    [HttpPost("{id}/keys")]
    [ProducesResponseType(typeof(ApiKeyCreatedDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiKeyCreatedDto>> GenerateKey(
        string id,
        [FromBody] GenerateApiKeyRequest request,
        CancellationToken ct)
    {
        var clientId = GetClientId();
        if (clientId is null)
            return BadRequest(Problem(detail: "Header X-Client-Id is required.", statusCode: 400));

        try
        {
            var dto = await _apiKeyService.GenerateAsync(id, clientId, request, ct);

            // 201 sans Location header — la clé ne peut pas être re-consultée par son ID
            return StatusCode(StatusCodes.Status201Created, dto);
        }
        catch (IntegrationNotFoundException ex)
        {
            return NotFound(Problem(detail: ex.Message, statusCode: 404));
        }
        catch (UnauthorizedIntegrationAccessException ex)
        {
            return StatusCode(403, Problem(detail: ex.Message, statusCode: 403));
        }
        catch (InvalidApiKeyScopeException ex)
        {
            return BadRequest(Problem(detail: ex.Message, statusCode: 400));
        }
        catch (ApiKeyLimitExceededException ex)
        {
            return UnprocessableEntity(Problem(detail: ex.Message, statusCode: 422));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error generating key for integration {IntegrationId}", id);
            return StatusCode(500, Problem(detail: "An unexpected error occurred.", statusCode: 500));
        }
    }

    // ── DELETE /api/integrations/{id}/keys/{keyId} ────────────────────────────
    /// <summary>
    /// Révoque définitivement une clé API. Opération irréversible.
    /// </summary>
    [HttpDelete("{id}/keys/{keyId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeKey(string id, string keyId, CancellationToken ct)
    {
        var clientId = GetClientId();
        if (clientId is null)
            return BadRequest(Problem(detail: "Header X-Client-Id is required.", statusCode: 400));

        try
        {
            await _apiKeyService.RevokeAsync(keyId, id, clientId, ct);
            return NoContent();
        }
        catch (ApiKeyNotFoundException ex)
        {
            return NotFound(Problem(detail: ex.Message, statusCode: 404));
        }
        catch (UnauthorizedIntegrationAccessException ex)
        {
            return StatusCode(403, Problem(detail: ex.Message, statusCode: 403));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error revoking key {KeyId}", keyId);
            return StatusCode(500, Problem(detail: "An unexpected error occurred.", statusCode: 500));
        }
    }

    // ── Private helper ────────────────────────────────────────────────────────

    /// <summary>
    /// Extrait le clientId depuis les sources disponibles, par ordre de priorité :
    ///   1. HttpContext.Items["ClientId"]  → injecté par ApiKeyAuthenticationMiddleware (Phase 3)
    ///   2. Header X-Client-Id            → usage direct en Phase 2 (sans JWT)
    /// Retourne null si aucune source ne fournit de clientId.
    /// </summary>
    private string? GetClientId()
    {
        // Priorité 1 : contexte injecté par le middleware d'auth (Phase 3)
        if (HttpContext.Items.TryGetValue("ClientId", out var clientIdFromContext)
            && clientIdFromContext is string clientIdStr
            && !string.IsNullOrWhiteSpace(clientIdStr))
        {
            return clientIdStr;
        }

        // Priorité 2 : header direct (Phase 2 — à remplacer par JWT en Phase 3)
        var headerValue = HttpContext.Request.Headers["X-Client-Id"].FirstOrDefault();
        return string.IsNullOrWhiteSpace(headerValue) ? null : headerValue;
    }
}