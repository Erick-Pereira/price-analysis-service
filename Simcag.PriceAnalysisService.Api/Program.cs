using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simcag.PriceAnalysisService.Application.Interfaces;
using Simcag.PriceAnalysisService.Application.Services;
using Simcag.PriceAnalysisService.Application.UseCases;
using Simcag.PriceAnalysisService.Application.Workers;
using Simcag.PriceAnalysisService.Infrastructure.Persistence.DbContext;
using Simcag.PriceAnalysisService.Infrastructure.Repositories;
using Simcag.PriceAnalysisService.Infrastructure.Redis;
using Simcag.Shared.Events;
using Simcag.Shared.Messaging;
using Simcag.Shared.Messaging.Configuration;
using Simcag.Shared.Messaging.Extensions;
using Simcag.Shared.Hosting;
using Simcag.Shared.Telemetry;
using Polly;
using Polly.Extensions.Http;
using StackExchange.Redis;

// Load .env so DB__* and others are in Environment; try content root first when known (e.g. IDE), then cwd / app base.
static void LoadEnvFromCommonPaths(string? contentRoot = null)
{
    var dirs = new[] { contentRoot, Directory.GetCurrentDirectory(), AppContext.BaseDirectory }
        .Where(d => !string.IsNullOrEmpty(d))!
        .Cast<string>();
    foreach (var dir in dirs.Distinct())
    {
        var p = Path.Combine(dir, ".env");
        if (File.Exists(p))
        {
            DotNetEnv.Env.NoClobber().Load(p);
            return;
        }
    }
}

LoadEnvFromCommonPaths();
ContainerListenConfiguration.NormalizeAspNetCoreListenUrlsInContainer();
var builder = WebApplication.CreateBuilder(args);
if (!string.IsNullOrEmpty(builder.Environment.ContentRootPath))
    LoadEnvFromCommonPaths(builder.Environment.ContentRootPath);
ContainerListenConfiguration.NormalizeAspNetCoreListenUrlsInContainer();
ContainerListenConfiguration.ApplyDockerListenUrls(builder);
builder.AddSimcagDistributedTelemetry("Simcag.PriceAnalysisService");
static string? GetEnv(params string[] keys)
{
    foreach (var k in keys)
    {
        var v = Environment.GetEnvironmentVariable(k);
        if (!string.IsNullOrWhiteSpace(v))
            return v;
    }
    return null;
}

static string GetNpgsqlConnectionString(IConfiguration configuration)
{
    var fromSettings = configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(fromSettings))
        return fromSettings;

    // Env vars (DB__HOST) are exposed as DB:Host by ASP.NET configuration (aliases legados)
    var host = configuration["DB:Host"] ?? GetEnv("DB__HOST", "DB_HOST") ?? "localhost";
    var port = configuration["DB:Port"] ?? GetEnv("DB__PORT", "DB_PORT") ?? "5432";
    var database = configuration["DB:Name"] ?? configuration["DB:Database"] ?? GetEnv("DB__NAME", "DB__DATABASE", "DB_NAME") ?? "simcag_pricing";
    var user = configuration["DB:User"] ?? GetEnv("DB__USER", "DB_USER", "DB__USERNAME") ?? "postgres";
    var password = configuration["DB:Password"] ?? GetEnv("DB__PASSWORD", "DB_PASSWORD") ?? "postgres";

    return $"Host={host};Port={port};Database={database};Username={user};Password={password}";
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo { Title = "SIMC-AG Service", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name        = "Authorization",
        In          = Microsoft.OpenApi.ParameterLocation.Header,
        Type        = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme      = "bearer",
        BearerFormat = "JWT",
        Description = "Cole apenas o JWT (sem 'Bearer ')."
    });
    c.AddSecurityRequirement(document => new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

// Database — DefaultConnection in appsettings.* + optional DB__* / .env
var connectionString = GetNpgsqlConnectionString(builder.Configuration);

builder.Services.AddDbContext<PriceAnalysisDbContext>(options =>
    options.UseNpgsql(connectionString));

// Repositories
builder.Services.AddScoped<IPriceRepository, Simcag.PriceAnalysisService.Infrastructure.Persistence.PriceRepository>();
builder.Services.AddScoped<IPriceAnalysisRepository, PriceAnalysisRepository>();

// Services
builder.Services.AddScoped<IPriceAnalysisService, PriceAnalysisService>();
builder.Services.AddScoped<DetectPriceVariationUseCase>();
builder.Services.AddScoped<ProcessPriceDataUseCase>();

// HTTP Client with Polly for Market Data Service
builder.Services.AddHttpClient("MarketDataClient", client =>
{
    client.BaseAddress = new Uri(
        GetEnv("MARKET_DATA_API_URL", "MARKETDATA_SERVICE_URL", "MarketData__BaseUrl") ?? "http://localhost:8082");
    client.Timeout = TimeSpan.FromSeconds(10);
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

// Redis: optional market reference cache
var redisConnection = GetEnv("REDIS__CONNECTION", "REDIS_CONNECTION", "ConnectionStrings__Redis", "REDIS__CONNECTION_STRING");
if (string.IsNullOrWhiteSpace(redisConnection))
    builder.Services.AddSingleton<IMarketDataCacheService, NullMarketDataCacheService>();
else
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
    builder.Services.AddSingleton<IMarketDataCacheService, RedisMarketDataCacheService>();
}

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "PostgreSQL");

// RabbitMQ
var rabbitMqOptions = new RabbitMqOptions
{
    Host = GetEnv("RABBITMQ__HOST", "RABBITMQ_HOST") ?? "localhost",
    Port = int.Parse(GetEnv("RABBITMQ__PORT", "RABBITMQ_PORT") ?? "5672"),
    UserName = GetEnv("RABBITMQ__USERNAME", "RABBITMQ_USERNAME") ?? "guest",
    Password = GetEnv("RABBITMQ__PASSWORD", "RABBITMQ_PASSWORD") ?? "guest",
    VirtualHost = GetEnv("RABBITMQ__VIRTUALHOST", "RABBITMQ_VIRTUALHOST") ?? "/"
};
rabbitMqOptions.ApplyMessageSigningFromEnvironment();

builder.Services.AddRabbitMqMessaging(rabbitMqOptions);

var eventsExchange = EventBusConstants.GetEventsExchangeName();
builder.Services.AddRabbitMqEventConsumer<PriceDataProcessedEvent>(nameof(PriceDataProcessedEvent), eventsExchange);
builder.Services.AddRabbitMqEventConsumer<EnrichedFinancialDataEvent>(EventNames.EnrichedFinancialData, eventsExchange);
builder.Services.AddRabbitMqEventPublisher<Simcag.Shared.Events.PriceAnalyzedEvent>(eventsExchange);
builder.Services.AddRabbitMqEventPublisher<Simcag.PriceAnalysisService.Domain.Events.PriceUpdatedEvent>(eventsExchange);
builder.Services.AddRabbitMqEventPublisher<Simcag.Shared.Events.PriceAnalysisCompletedEvent>(eventsExchange);

// Background Services
builder.Services.AddHostedService<DataProcessedEventConsumer>();
builder.Services.AddHostedService<EnrichedFinancialDataConsumer>();

ContainerListenConfiguration.NormalizeAspNetCoreListenUrlsInContainer();
ContainerListenConfiguration.ApplyDockerListenUrls(builder);
var app = builder.Build();

app.UseSimcagHttpCorrelationActivityTags();

// Cria/atualiza tabelas (PriceAnalyses, etc.). Sem isto, PG devolve 42P01 ao gravar análise.
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<PriceAnalysisDbContext>();
        await db.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Falha ao aplicar migrations do PriceAnalysisDbContext.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.UseSimcagTelemetryEndpoints();

app.Run();

// Polly policies for HTTP resilience
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
}