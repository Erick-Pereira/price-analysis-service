using Microsoft.EntityFrameworkCore;
using Simcag.ProcessingService.Domain.Events;
using Simcag.PriceAnalysisService.Application.Interfaces;
using Simcag.PriceAnalysisService.Application.Services;
using Simcag.PriceAnalysisService.Application.Workers;
using Simcag.PriceAnalysisService.Domain.Entities;
using Simcag.PriceAnalysisService.Infrastructure.Persistence.DbContext;
using Simcag.PriceAnalysisService.Infrastructure.Repositories;
using Simcag.Shared.Messaging.Configuration;
using Simcag.Shared.Messaging.Contracts;
using Simcag.Shared.Messaging.Extensions;
using Polly;
using Polly.Extensions.Http;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
var connectionString = $"Host={Environment.GetEnvironmentVariable("DB__HOST")};" +
                      $"Port={Environment.GetEnvironmentVariable("DB__PORT")};" +
                      $"Database={Environment.GetEnvironmentVariable("DB__NAME")};" +
                      $"Username={Environment.GetEnvironmentVariable("DB__USER")};" +
                      $"Password={Environment.GetEnvironmentVariable("DB__PASSWORD")}";

builder.Services.AddDbContext<PriceAnalysisDbContext>(options =>
    options.UseNpgsql(connectionString));

// Repositories
builder.Services.AddScoped<IPriceAnalysisRepository, PriceAnalysisRepository>();

// Services
builder.Services.AddScoped<IPriceAnalysisService, PriceAnalysisService>();

// HTTP Client with Polly for Market Data Service
builder.Services.AddHttpClient("MarketDataClient", client =>
{
    client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("MARKETDATA_SERVICE_URL") ?? "http://localhost:8082");
    client.Timeout = TimeSpan.FromSeconds(10);
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "PostgreSQL");

// RabbitMQ Configuration
var rabbitMqOptions = new RabbitMqOptions
{
    Host = Environment.GetEnvironmentVariable("RABBITMQ__HOST") ?? "localhost",
    Port = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ__PORT") ?? "5672"),
    UserName = Environment.GetEnvironmentVariable("RABBITMQ__USERNAME") ?? "guest",
    Password = Environment.GetEnvironmentVariable("RABBITMQ__PASSWORD") ?? "guest",
    VirtualHost = Environment.GetEnvironmentVariable("RABBITMQ__VIRTUALHOST") ?? "/"
};

builder.Services.AddRabbitMqMessaging(rabbitMqOptions);
builder.Services.AddRabbitMqEventConsumer<DataProcessedEvent>("data.processed");
builder.Services.AddRabbitMqEventPublisher<PriceAnalyzedEvent>("simcag-events");

// Background Services
builder.Services.AddHostedService<DataProcessedEventConsumer>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

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