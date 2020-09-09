using EventBus;
using EventBus.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceBus.Demo.IntegrationEvents;
using ServiceBus.Demo.IntegrationEvents.Handlers;
using System;
using System.Threading.Tasks;
using EventBus.Extensions;

namespace ServiceBus.Demo
{
    class Program
    {
        private const string CONNECTION_STRING = "";
        private const string TOPIC_NAME = "events";
        private const string SUBSCRIPTION_NAME = "ItemEvents";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting...");

            var services = new ServiceCollection();
            services.AddLogging(LoggingBuilder =>
            {
                LoggingBuilder.AddConsole();
            });
            services.AddEventBus(CONNECTION_STRING, TOPIC_NAME, SUBSCRIPTION_NAME);

            services.AddTransient<ItemCreatedEventHandler>();
            services.AddTransient<ItemDeletedEventHandler>();

            var serviceProvider = services.BuildServiceProvider();
            var eventBus = serviceProvider.GetRequiredService<IEventBus>();

            eventBus.Subscribe<ItemCreatedEvent, ItemCreatedEventHandler>();
            eventBus.Subscribe<ItemDeletedEvent, ItemDeletedEventHandler>();

            var id = Guid.NewGuid().ToString();

            await eventBus.PublishAsync(new ItemCreatedEvent(id));
            await eventBus.PublishAsync(new ItemDeletedEvent(id));

            Console.ReadKey();
        }
    }
}
