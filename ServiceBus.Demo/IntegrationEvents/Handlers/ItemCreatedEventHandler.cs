using EventBus.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace ServiceBus.Demo.IntegrationEvents.Handlers
{
    public class ItemCreatedEventHandler : IIntegrationEventHandler<ItemCreatedEvent>
    {
        private readonly ILogger<ItemCreatedEventHandler> _logger;

        public ItemCreatedEventHandler(ILogger<ItemCreatedEventHandler> logger)
        {
            _logger = logger;
        }

        public async Task Handle(ItemCreatedEvent @event)
        {
            _logger.LogInformation($"{@event.Id} was created");
            await Task.CompletedTask;
        }
    }
}
