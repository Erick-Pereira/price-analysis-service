using Simcag.PriceAnalysisService.Application.Interfaces;
using Simcag.PriceAnalysisService.Application.Services;

using Simcag.PriceAnalysisService.Infrastructure.Configuration;
using Simcag.PriceAnalysisService.Infrastructure.Workers;
using Simcag.Shared.Messaging.Configuration;
using Simcag.Shared.Messaging.Contracts;
using Simcag.Shared.Messaging.Extensions;
using Simcag.PriceAnalysisService.Application.Events;
using Simcag.PriceAnalysisService.Infrastructure.Messaging;
using Simcag.PriceAnalysisService.Application.UseCases;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

var urls = GetListeningUrl();
builder.WebHost.UseUrls(urls);
Console.WriteLine($"🚀 Price Analysis Service listening on: {urls}");
Console.WriteLine($"📡 Access via: http://localhost:{ParsePort(urls)} or http://container-host:{ParsePort(urls)}");

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddSingleton<IPriceAnalysisService, PriceAnalysisService>();
builder.Services.AddSingleton<IPriceStatisticsService, PriceStatisticsService>();
builder.Services.AddSingleton<IPriceOutlierDetectionService, PriceOutlierDetectionService>();
builder.Services.AddSingleton<ProcessPriceDataUseCase>();
builder.Services.AddSingleton<DetectPriceVariationUseCase>();

var rabbitMqHost = Environment.GetEnvironmentVariable("RABBITMQ__HOST") ?? builder.Configuration["RabbitMq:Host"] ?? "localhost";
var rabbitMqPort = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ__PORT") ?? builder.Configuration["RabbitMq:Port"] ?? "5672");
var rabbitMqUserName = Environment.GetEnvironmentVariable("RABBITMQ__USERNAME") ?? builder.Configuration["RabbitMq:UserName"] ?? "guest";
var rabbitMqPassword = Environment.GetEnvironmentVariable("RABBITMQ__PASSWORD") ?? builder.Configuration["RabbitMq:Password"] ?? "guest";
var rabbitMqVirtualHost = Environment.GetEnvironmentVariable("RABBITMQ__VIRTUALHOST") ?? builder.Configuration["RabbitMq:VirtualHost"] ?? "/";

var rabbitMqOptions = new RabbitMqOptions
{
    Host = rabbitMqHost,
    Port = rabbitMqPort,
    UserName = rabbitMqUserName,
    Password = rabbitMqPassword,
    VirtualHost = rabbitMqVirtualHost
};

if (string.IsNullOrEmpty(rabbitMqOptions.Host))
    throw new InvalidOperationException("RabbitMq:Host is not configured. Check appsettings.json or environment variables.");
if (string.IsNullOrEmpty(rabbitMqOptions.UserName))
    throw new InvalidOperationException("RabbitMq:UserName is not configured. Check appsettings.json or environment variables.");

builder.Services.AddRabbitMqMessaging(rabbitMqOptions);
builder.Services.AddSingleton<IRabbitMqChannelFactory, RabbitMqChannelFactory>();
builder.Services.AddSingleton<IQueueConfigurator, PriceAnalysisQueueConfigurator>();
builder.Services.AddRabbitMqEventPublisher<PriceAnalyzedEvent>("simcag-events");
builder.Services.AddRabbitMqEventPublisher<PriceUpdatedEvent>("simcag-events");
builder.Services.AddSingleton<IEventConsumer<DataProcessedEvent>, PriceAnalysisDataProcessedEventConsumer>();

builder.Services.AddPriceAnalysisInfrastructure(builder.Configuration);

builder.Services.AddHostedService<PriceAnalysisWorker>();

builder.Services.AddLogging(config => config.SetMinimumLevel(LogLevel.Information));

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

static string GetListeningUrl()
{
    const int defaultPort = 8080;

    var envUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
    var envPort = Environment.GetEnvironmentVariable("PORT");
    var requestedUrl = !string.IsNullOrWhiteSpace(envUrls)
        ? envUrls
        : !string.IsNullOrWhiteSpace(envPort)
            ? $"http://0.0.0.0:{envPort}"
            : $"http://0.0.0.0:{defaultPort}";

    var requestedPort = ParsePort(requestedUrl) ?? defaultPort;
    var port = FindAvailablePort(requestedPort);
    return $"http://0.0.0.0:{port}";
}

static int? ParsePort(string url)
{
    var match = System.Text.RegularExpressions.Regex.Match(url, @":(\d+)");
    return match.Success && int.TryParse(match.Groups[1].Value, out var port)
        ? port
        : null;
}

static int FindAvailablePort(int startPort)
{
    for (var port = startPort; port < startPort + 50; port++)
    {
        if (IsPortAvailable(port))
            return port;
    }

    return startPort;
}

static bool IsPortAvailable(int port)
{
    try
    {
        using var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, port);
        listener.Start();
        return true;
    }
    catch
    {
        return false;
    }
}