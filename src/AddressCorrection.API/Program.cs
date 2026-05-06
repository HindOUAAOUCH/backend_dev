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
builder.Services.AddSingleton<IActiveLlmModelProvider, ActiveLlmModelProvider>(); // thread-safe

// ── Stratégies et orchestrateurs (Infrastructure) ────────────────────────────
builder.Services.AddScoped<IAddressCacheStrategy, AddressCacheStrategy>();
builder.Services.AddScoped<ILlmOrchestrator, LlmOrchestrator>();
builder.Services.AddScoped<IRequestTracker, RequestTracker>();

// ── Pipeline de correction ────────────────────────────────────────────────────
// Les étapes sont enregistrées individuellement pour résolution par le conteneur DI,
// puis assemblées dans un ordre explicite via une factory pour garantir la séquence.
builder.Services.AddScoped<CacheLookupStep>();
builder.Services.AddScoped<LlmProcessingStep>();
builder.Services.AddScoped<ReferentialValidationStep>();
builder.Services.AddScoped<PersistenceStep>();
builder.Services.AddScoped<TrackingStep>();
builder.Services.AddScoped<AddressCorrectionPipeline>(sp => new AddressCorrectionPipeline(
[
    sp.GetRequiredService<CacheLookupStep>(),
    sp.GetRequiredService<LlmProcessingStep>(),
    sp.GetRequiredService<ReferentialValidationStep>(),
    sp.GetRequiredService<PersistenceStep>(),
    sp.GetRequiredService<TrackingStep>(),
]));

// ── Orchestrateur final ───────────────────────────────────────────────────────
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