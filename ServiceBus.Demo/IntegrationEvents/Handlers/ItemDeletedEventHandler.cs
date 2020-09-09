using EventBus.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ServiceBus.Demo.IntegrationEvents.Handlers
{
    public class ItemDeletedEventHandler : IIntegrationEventHandler<ItemDeletedEvent>
    {
        private readonly ILogger<ItemDeletedEventHandler> _logger;

        public ItemDeletedEventHandler(ILogger<ItemDeletedEventHandler> logger)
        {
            _logger = logger;
        }

        public async Task Handle(ItemDeletedEvent @event)
        {
            _logger.LogInformation($"{@event.Id} was deleted");
            await Task.CompletedTask;
        }
    }
}
