using EventBus.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventBus.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEventBus(this IServiceCollection services, string connectionString, string topicName, string subscriptionName)
        {
            services.AddSingleton<IEventBus>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<ServiceBusEventBus>>();
                var eventBus = new ServiceBusEventBus(connectionString, topicName, logger, new InMemoryEventBusSubscriptionsManager(), sp, subscriptionName, true);
                return eventBus;
            });
            return services;
        }
    }
}
