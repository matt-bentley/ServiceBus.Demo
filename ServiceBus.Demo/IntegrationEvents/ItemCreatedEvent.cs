using EventBus.Events;
using System;

namespace ServiceBus.Demo.IntegrationEvents
{
    public class ItemCreatedEvent : IntegrationEvent
    {
        public ItemCreatedEvent(string id) : base(id, DateTime.UtcNow)
        {

        }
    }
}
