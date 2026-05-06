using AddressCorrection.src.AddressCorrection.Application.Configuration;
using AddressCorrection.src.AddressCorrection.Application.DTOs;
using AddressCorrection.src.AddressCorrection.Application.Exceptions;
using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using AddressCorrection.src.AddressCorrection.Application.Mappers;
using AddressCorrection.src.AddressCorrection.Application.Prompts;
using AddressCorrection.src.AddressCorrection.Domain.Constants;
using AddressCorrection.src.AddressCorrection.Domain.Entities;
using AddressCorrection.src.AddressCorrection.Infrastructure.Policies;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.Json;

namespace AddressCorrection.src.AddressCorrection.Application.Services;

public class AddressCorrectionService : IAddressService
{
    private readonly ILlmClient _llmClient;
    private readonly GitHubModelsConfig _config;
    private readonly IModelSelectionService _modelSelectionService;
    private readonly IAddressRepository _addressRepository;
    private readonly ICorrectionRequestRepository _correctionRequestRepository;
    private readonly IUsageTrackingRepository _usageTrackingRepository;
    private readonly IAddressReferentialService _referentialService;
    private readonly ILogger<AddressCorrectionService> _logger;

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public AddressCorrectionService(
        ILlmClient llmClient,
        IOptions<GitHubModelsConfig> config,
        IModelSelectionService modelSelectionService,
        IAddressRepository addressRepository,
        ICorrectionRequestRepository correctionRequestRepository,
        IUsageTrackingRepository usageTrackingRepository,
        IAddressReferentialService referentialService,
        ILogger<AddressCorrectionService> logger)
    {
        _llmClient = llmClient;
        _config = config.Value;
        _modelSelectionService = modelSelectionService;
        _addressRepository = addressRepository;
        _correctionRequestRepository = correctionRequestRepository;
        _usageTrackingRepository = usageTrackingRepository;
        _referentialService = referentialService;
        _logger = logger;
    }

    public async Task<AddressResponse?> CorrectAsync(AddressRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RawAddress))
            throw new ArgumentException("RawAddress is required.", nameof(request));

        var stopwatch = Stopwatch.StartNew();
        var normalizedAddress = request.RawAddress.Trim().ToLowerInvariant();

        // ── Étape 1 : Cache ──────────────────────────────────────────────────
        var cached = await _addressRepository.FindByNormalizedAddressAsync(normalizedAddress);
        if (cached != null)
        {
            stopwatch.Stop();
            _logger.LogInformation("Cache hit for: {Address}", normalizedAddress);

            var cachedResponse = AddressMapper.ToResponse(cached);
            await SaveRequestTrace(
                rawAddress: request.RawAddress,
                correctedAddress: AddressMapper.ToFormattedLine(cachedResponse),
                fromCache: true,
                modelUsed: cached.ModelUsed,
                status: CorrectionConstants.Status.Success,
                durationMs: stopwatch.ElapsedMilliseconds);
            await _usageTrackingRepository.IncrementAsync(fromCache: true);
            return cachedResponse;
        }

        // ── Étape 2 : Construction du prompt ─────────────────────────────────
        var prompt = AddressCorrectionPrompt.Build(request.RawAddress);

        // ── Étape 3 : LLM avec fallback multi-modèles ────────────────────────
        AddressResponse? llmResult = null;
        string? modelUsed = null;

        foreach (var modelName in BuildModelsList())
        {
            try
            {
                var policy = LlmResiliencePolicy.Build(_logger, modelName);
                var responseText = await policy.ExecuteAsync(() =>
                    _llmClient.CompleteAsync(prompt, request.RawAddress!, modelName));

                llmResult = JsonSerializer.Deserialize<AddressResponse>(responseText, JsonOptions);

                if (llmResult == null) continue;

                modelUsed = modelName;
                _logger.LogInformation("LLM success with model {Model} in {Duration}ms",
                    modelName, stopwatch.ElapsedMilliseconds);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Model {Model} failed: {Error}", modelName, ex.Message);
            }
        }

        if (llmResult == null)
        {
            stopwatch.Stop();
            await SaveRequestTrace(
                rawAddress: request.RawAddress,
                correctedAddress: null,
                fromCache: false,
                modelUsed: CorrectionConstants.Model.None,
                status: CorrectionConstants.Status.Failed,
                durationMs: stopwatch.ElapsedMilliseconds);
            throw new AllModelsFailedException();
        }

        // ── Étape 4 : Validation référentiel ─────────────────────────────────
        var referentialResult = await GetReferentialResultSafeAsync(llmResult);

        if (referentialResult != null)
        {
            if (!string.IsNullOrWhiteSpace(referentialResult.PostalCode))
                llmResult.PostalCode = referentialResult.PostalCode;
            if (!string.IsNullOrWhiteSpace(referentialResult.City))
                llmResult.City = referentialResult.City;
            if (!string.IsNullOrWhiteSpace(referentialResult.Street))
                llmResult.Street = referentialResult.Street;
        }

        // ── Étape 5 : Sauvegarde ─────────────────────────────────────────────
        stopwatch.Stop();

        // Sauvegarde dans le cache d'adresses
        var record = AddressMapper.ToRecord(request, llmResult, normalizedAddress, modelUsed!);
        await _addressRepository.SaveAsync(record);

        // Trace de la requête avec l'adresse corrigée formatée
        await SaveRequestTrace(
            rawAddress: request.RawAddress,
            correctedAddress: AddressMapper.ToFormattedLine(llmResult),
            fromCache: false,
            modelUsed: modelUsed!,
            status: CorrectionConstants.Status.Success,
            durationMs: stopwatch.ElapsedMilliseconds);

        await _usageTrackingRepository.IncrementAsync(fromCache: false);

        return llmResult;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<ReferentialResult?> GetReferentialResultSafeAsync(AddressResponse llmResult)
    {
        try
        {
            return await _referentialService.ValidateAsync(
                llmResult.HouseNumber,
                llmResult.Street,
                llmResult.City,
                llmResult.PostalCode,
                llmResult.Country);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Referential call failed (non-blocking): {Error}", ex.Message);
            return null;
        }
    }

    private async Task SaveRequestTrace(
        string rawAddress,
        string? correctedAddress,
        bool fromCache,
        string modelUsed,
        string status,
        long durationMs)
    {
        await _correctionRequestRepository.SaveAsync(new CorrectionRequest
        {
            RawAddress = rawAddress,
            CorrectedAddress = correctedAddress,
            FromCache = fromCache,
            ModelUsed = modelUsed,
            Status = status,
            DurationMs = durationMs,
            Source = CorrectionConstants.Source.Api,
            SentAt = DateTime.UtcNow,
        });
    }

    private List<string> BuildModelsList()
    {
        var selectedModel = _modelSelectionService.GetSelectedModel();
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