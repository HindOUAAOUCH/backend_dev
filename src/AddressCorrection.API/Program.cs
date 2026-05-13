using AddressCorrection.src.AddressCorrection.Application.Configuration;
using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using AddressCorrection.src.AddressCorrection.Application.Pipeline;
using AddressCorrection.src.AddressCorrection.Application.Pipeline.Steps;
using AddressCorrection.src.AddressCorrection.Application.Services;
using AddressCorrection.src.AddressCorrection.Infrastructure;
using AddressCorrection.src.AddressCorrection.Infrastructure.LLMClients;
using AddressCorrection.src.AddressCorrection.Infrastructure.Persistence;
using AddressCorrection.src.AddressCorrection.Infrastructure.Referentials;
using AddressCorrection.src.AddressCorrection.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy
            .WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("Content-Disposition")
    );
});

// ── Configuration ─────────────────────────────────────────────────────────────
builder.Services
    .AddOptions<GitHubModelsConfig>()
    .Bind(builder.Configuration.GetSection("GitHubModels"))
    .Validate(
        cfg => !string.IsNullOrWhiteSpace(cfg.Token)
               || cfg.Models.Any(m => !string.IsNullOrWhiteSpace(m.Token)),
        "GitHubModels config invalid"
    );

// ── HTTP Clients ──────────────────────────────────────────────────────────────
builder.Services.AddHttpClient("GitHubModels", client =>
{
    client.BaseAddress = new Uri("https://models.inference.ai.azure.com");
    client.Timeout = TimeSpan.FromSeconds(90);
});

builder.Services.AddHttpClient("Referential", client =>
{
    client.Timeout = TimeSpan.FromSeconds(3);
});

builder.Services.AddHttpClient("Nominatim", client =>
{
    client.Timeout = TimeSpan.FromSeconds(3);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("AddressCorrectionApp/1.0");
});

builder.Services.AddHttpClient("Webhook", client =>
{
    client.Timeout = TimeSpan.FromSeconds(5);
});

// ── MongoDB ───────────────────────────────────────────────────────────────────
var mongoSettings = builder.Configuration
    .GetSection("MongoDB")
    .Get<MongoDbSettings>()
    ?? throw new InvalidOperationException("MongoDB config missing");

builder.Services.AddSingleton(mongoSettings);
builder.Services.AddSingleton<MongoDbContext>();

// ── Repositories ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<ICorrectionRequestRepository, CorrectionRequestRepository>();
builder.Services.AddScoped<IUsageTrackingRepository, UsageTrackingRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ── Referentials ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IAddressReferentialClient, BanClient>();
builder.Services.AddScoped<IAddressReferentialClient, CartoCiudadClient>();
builder.Services.AddScoped<IAddressReferentialClient, NominatimClient>();
builder.Services.AddScoped<IAddressReferentialService, AddressReferentialService>();

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<ILlmClient, GitHubLlmClient>();
builder.Services.AddScoped<IAddressCacheStrategy, AddressCacheStrategy>();
builder.Services.AddScoped<ILlmOrchestrator, LlmOrchestrator>();
builder.Services.AddScoped<IRequestTracker, RequestTracker>();
builder.Services.AddSingleton<IActiveLlmModelProvider, ActiveLlmModelProvider>();

// ── Pipeline ───────────────────────────────────────────────────────────────────
builder.Services.AddScoped<ICorrectionStep, CacheLookupStep>();
builder.Services.AddScoped<ICorrectionStep, LlmProcessingStep>();
builder.Services.AddScoped<ICorrectionStep, ReferentialValidationStep>();
builder.Services.AddScoped<ICorrectionStep, PersistenceStep>();
builder.Services.AddScoped<ICorrectionStep, TrackingStep>();
builder.Services.AddScoped<AddressCorrectionPipeline>();

// ── Orchestrator ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IAddressCorrector, AddressCorrectionOrchestrator>();
builder.Services.AddScoped<IDashboardService, DashboardService>();


// ── Phase 2 (SI EXISTE) ───────────────────────────────────────────────────────
builder.Services.AddPhase2IntegrationsFeature();

// ── Hosted service ────────────────────────────────────────────────────────────
builder.Services.AddHostedService<MongoIndexInitializer>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseApiKeyAuthentication();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.MapControllers();
app.Run();

// ── Mongo index initializer ───────────────────────────────────────────────────
public sealed class MongoIndexInitializer : IHostedService
{
    private readonly ILogger<MongoIndexInitializer> _logger;

    public MongoIndexInitializer(ILogger<MongoIndexInitializer> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MongoDB indexes initialization skipped (not implemented yet)");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}