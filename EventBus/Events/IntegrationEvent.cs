using Newtonsoft.Json;
using System;

namespace EventBus.Events
{
	public class IntegrationEvent
	{
		public IntegrationEvent()
		{
			Id = Guid.NewGuid().ToString();
			CreationDate = DateTime.UtcNow;
		}

		[JsonConstructor]
		public IntegrationEvent(string id, DateTime createDate)
		{
			Id = id;
			CreationDate = createDate;
		}

		[JsonProperty]
		public string Id { get; private set; }

		[JsonProperty]
		public DateTime CreationDate { get; private set; }
	}
}
