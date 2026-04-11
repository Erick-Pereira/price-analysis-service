using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Simcag.Shared.Messaging.Configuration;
using Simcag.Shared.Messaging.Extensions;

namespace Simcag.PriceAnalysisService.Infrastructure.Extensions
{
    public static class MessagingExtensions
    {
        public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddRabbitMqMessaging(configuration);
            return services;
        }
    }
}