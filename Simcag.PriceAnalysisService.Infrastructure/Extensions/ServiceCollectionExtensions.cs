using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Simcag.PriceAnalysisService.Application.Interfaces;
using Simcag.PriceAnalysisService.Infrastructure.Redis;
using Simcag.Shared.Messaging.Configuration;
using Simcag.Shared.Messaging.Extensions;

namespace Simcag.PriceAnalysisService.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    //public static IServiceCollection AddPriceAnalysisInfrastructure(
    //    this IServiceCollection services,
    //    IConfiguration configuration)
    //{
    //    var redisConnectionString = configuration.GetConnectionString("Redis") ?? "potato-server:6379,abortConnect=false";

    //    services.AddSingleton<IConnectionMultiplexer>(provider =>
    //        ConnectionMultiplexer.Connect(new ConfigurationOptions
    //        {
    //            EndPoints = { redisConnectionString },
    //            AbortOnConnectFail = false,
    //            ConnectRetry = 5,
    //            ConnectTimeout = 5000,
    //            SyncTimeout = 5000
    //        }));

    //    services.AddSingleton<IPriceRepository, PriceRepository>();

    //    return services;
    //}
}