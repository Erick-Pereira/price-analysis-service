using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Simcag.PriceAnalysisService.Application.Interfaces;
using Simcag.PriceAnalysisService.Infrastructure.Persistence;

namespace Simcag.PriceAnalysisService.Infrastructure.Configuration;

public static class InfrastructureServiceRegistration
{
    public static void AddPriceAnalysisInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IPriceRepository, PriceRepository>();
    }
}
