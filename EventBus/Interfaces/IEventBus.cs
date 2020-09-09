using EventBus.Events;
using System.Threading.Tasks;

namespace EventBus.Interfaces
{
	public interface IEventBus
	{
		Task PublishAsync(IntegrationEvent @event);

		void Subscribe<T, TH>()
			where T : IntegrationEvent
			where TH : IIntegrationEventHandler<T>;
	}
}
