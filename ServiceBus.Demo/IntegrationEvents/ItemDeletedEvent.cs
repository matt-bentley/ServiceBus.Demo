using EventBus.Events;
using System;

namespace ServiceBus.Demo.IntegrationEvents
{
    public class ItemDeletedEvent : IntegrationEvent
    {
        public ItemDeletedEvent(string id) : base(id, DateTime.UtcNow)
        {

        }
    }
}
