using AddressCorrection.src.AddressCorrection.Application.Configuration;
using AddressCorrection.src.AddressCorrection.Application.Interfaces;
using AddressCorrection.src.AddressCorrection.Application.Pipeline;
using AddressCorrection.src.AddressCorrection.Application.Pipeline.Steps;
using AddressCorrection.src.AddressCorrection.Application.Services;
using AddressCorrection.src.AddressCorrection.Infrastructure.LLMClients;
using AddressCorrection.src.AddressCorrection.Infrastructure.Persistence;
using AddressCorrection.src.AddressCorrection.Infrastructure.Referentials;
using AddressCorrection.src.AddressCorrection.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ── CORS ──────────────────────────────────────────────────────────────────────
// Autorise le frontend React (localhost:3000) à appeler le backend.
// En production, remplacer par l'URL réelle du frontend déployé.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy
            .WithOrigins(
                "http://localhost:3000",   // frontend dev
                "http://localhost:5173"    // Vite fallback
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("Content-Disposition") // utile pour les exports CSV futurs
    );
});

// ── Configuration ────────────────────────────────────────────────────────────
// Les secrets ne doivent JAMAIS être dans appsettings.json (versionné).
// Utiliser : variables d'environnement, dotnet user-secrets (dev), Azure Key Vault (prod).
//
// Exemples de variables d'environnement :
//   GitHubModels__Token=ghp_xxxx
//   MongoDB__ConnectionString=mongodb://...
//
// La validation est différée (lazy) : elle s'exécute lors du premier accès à la
// configuration, une fois que tous les providers (dont appsettings.Development.json)
// ont été chargés.
builder.Services
    .AddOptions<GitHubModelsConfig>()
    .Bind(builder.Configuration.GetSection("GitHubModels"))
    .Validate(
        cfg => !string.IsNullOrWhiteSpace(cfg.Token)
               || cfg.Models.Any(m => !string.IsNullOrWhiteSpace(m.Token)),
        "GitHubModels: at least a global Token or one per-model Token must be configured.");

// ── HTTP Clients ──────────────────────────────────────────────────────────────

// LLM GitHub Models
builder.Services.AddHttpClient("GitHubModels", client =>
{
    client.BaseAddress = new Uri("https://models.inference.ai.azure.com");
    client.Timeout = TimeSpan.FromSeconds(90); // Polly gère le timeout réel (60s)
});

// BAN (France) et CartoCiudad (Espagne) — timeout 3s strict
builder.Services.AddHttpClient("Referential", client =>
{
    client.Timeout = TimeSpan.FromSeconds(3);
});

// Nominatim — User-Agent OBLIGATOIRE selon la politique d'usage OSM
builder.Services.AddHttpClient("Nominatim", client =>
{
    client.Timeout = TimeSpan.FromSeconds(3);
    client.DefaultRequestHeaders.UserAgent
        .ParseAdd("AddressCorrectionApp/1.0 (contact@yourdomain.com)");
});

// ── MongoDB ───────────────────────────────────────────────────────────────────
var mongoSettings = builder.Configuration
    .GetSection("MongoDB")
    .Get<MongoDbSettings>()
    ?? throw new InvalidOperationException(
        "MongoDB configuration section is missing. " +
        "Set MongoDB__ConnectionString and MongoDB__DatabaseName.");

builder.Services.AddSingleton(mongoSettings);
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<ICorrectionRequestRepository, CorrectionRequestRepository>();
builder.Services.AddScoped<IUsageTrackingRepository, UsageTrackingRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ── Référentiels d'adresses ───────────────────────────────────────────────────
builder.Services.AddScoped<IAddressReferentialClient, BanClient>();         // 🇫🇷 France
builder.Services.AddScoped<IAddressReferentialClient, CartoCiudadClient>(); // 🇪🇸 Espagne
builder.Services.AddScoped<IAddressReferentialClient, NominatimClient>();   // 🌍 Reste Europe
builder.Services.AddScoped<IAddressReferentialService, AddressReferentialService>();

// ── Services applicatifs ──────────────────────────────────────────────────────
builder.Services.AddScoped<ILlmClient, GitHubLlmClient>();
builder.Services.AddScoped<IAddressCacheStrategy, AddressCacheStrategy>();
builder.Services.AddScoped<ILlmOrchestrator, LlmOrchestrator>();
builder.Services.AddScoped<IRequestTracker, RequestTracker>();
builder.Services.AddSingleton<IActiveLlmModelProvider, ActiveLlmModelProvider>(); // thread-safe

// ── Pipeline Steps ────────────────────────────────────────────────────────────
// Steps are executed in the order they are registered.
// Changing this order will break the correction flow:
//   1. CacheLookupStep        — return early if address is already cached
//   2. LlmProcessingStep      — call LLM(s) with multi-model fallback
//   3. ReferentialValidationStep — enrich/validate with address referentials (BAN, etc.)
//   4. PersistenceStep        — save the corrected address to the cache
//   5. TrackingStep           — log the request for analytics (always runs, even on failure)
builder.Services.AddScoped<ICorrectionStep, CacheLookupStep>();
builder.Services.AddScoped<ICorrectionStep, LlmProcessingStep>();
builder.Services.AddScoped<ICorrectionStep, ReferentialValidationStep>();
builder.Services.AddScoped<ICorrectionStep, PersistenceStep>();
builder.Services.AddScoped<ICorrectionStep, TrackingStep>();
builder.Services.AddScoped<AddressCorrectionPipeline>();

// ── Orchestrator ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IAddressCorrector, AddressCorrectionOrchestrator>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS doit être placé AVANT UseAuthorization et MapControllers
app.UseCors("AllowFrontend");

// En développement, désactiver la redirection HTTPS pour éviter les conflits HTTP/HTTPS
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthorization();
app.MapControllers();
app.Run();