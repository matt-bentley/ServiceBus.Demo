using EventBus.Events;
using EventBus.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace EventBus
{
    public class ServiceBusEventBus : IEventBus
    {
		private readonly ILogger<ServiceBusEventBus> _logger;
		private readonly IEventBusSubscriptionsManager _subsManager;
		private readonly ISubscriptionClient _subscriptionClient;
		private ITopicClient _topicClient;
		private readonly bool _labelFiltering;
		private readonly object _subscriptionLock = new object();
		private readonly object _publishLock = new object();
		private readonly string _connectionString;
		private readonly string _topic;
		private readonly IServiceProvider _serviceProvider;

		public ServiceBusEventBus(string connectionString, string topic, ILogger<ServiceBusEventBus> logger, IEventBusSubscriptionsManager subsManager, IServiceProvider serviceProvider, string subscriptionClientName, bool labelFiltering)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_labelFiltering = labelFiltering;
			_topic = topic;
			_connectionString = connectionString;
			_serviceProvider = serviceProvider;

			_topicClient = CreateTopicClient();

			// check if subscriber
			if (!string.IsNullOrEmpty(subscriptionClientName))
			{
				// this is a subscriber
				_subsManager = subsManager ?? new InMemoryEventBusSubscriptionsManager();
				_subscriptionClient = new SubscriptionClient(connectionString, topic, subscriptionClientName);

				if (labelFiltering)
				{
					RemoveDefaultRule();
				}
			}
		}

		public async Task PublishAsync(IntegrationEvent @event)
        {
			var eventName = @event.GetType().Name;
			var jsonMessage = JsonConvert.SerializeObject(@event);
			var body = Encoding.UTF8.GetBytes(jsonMessage);

			var message = new Message
			{
				MessageId = Guid.NewGuid().ToString(),
				Body = body,
				Label = eventName,
			};

			// lazy connection
			if (_topicClient.IsClosedOrClosing)
			{
				_topicClient = CreateTopicClient();
			}

			await _topicClient.SendAsync(message);
		}

        public void Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
			var eventName = typeof(T).Name;

			var containsKey = _subsManager.HasSubscriptionsForEvent<T>();
			if (!containsKey)
			{
				try
				{
					if (_labelFiltering)
					{
						_subscriptionClient.AddRuleAsync(new RuleDescription
						{
							Filter = new CorrelationFilter { Label = eventName },
							Name = eventName
						}).GetAwaiter().GetResult();
					}
				}
				catch (ServiceBusException)
				{
					_logger.LogWarning("The messaging entity {eventName} already exists.", eventName);
				}
			}

			_logger.LogInformation("Subscribing to event {EventName} with {EventHandler}", eventName, nameof(TH));

			bool registerHandler = false;
			lock (_subscriptionLock)
			{
				if (_subsManager.IsEmpty)
				{
					registerHandler = true;
				}
				_subsManager.AddSubscription<T, TH>();
			}
			if (registerHandler)
			{
				RegisterSubscriptionClientMessageHandler();
			}
		}

		private ITopicClient CreateTopicClient()
        {
			return new TopicClient(_connectionString, _topic, RetryPolicy.Default);
		}

		private void RegisterSubscriptionClientMessageHandler()
		{
			_subscriptionClient.RegisterMessageHandler(
				async (message, token) =>
				{
					var eventName = $"{message.Label}";
					var messageData = Encoding.UTF8.GetString(message.Body);

					// Complete the message so that it is not received again.
					if (await ProcessEvent(eventName, messageData))
					{
						await _subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
					}
				},
				new MessageHandlerOptions(ExceptionReceivedHandler) { MaxConcurrentCalls = 1, AutoComplete = false });
		}

		private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
		{
			var ex = exceptionReceivedEventArgs.Exception;
			var context = exceptionReceivedEventArgs.ExceptionReceivedContext;

			_logger.LogError(ex, "ERROR handling message: {ExceptionMessage} - Context: {@ExceptionContext}", ex.Message, context);

			return Task.CompletedTask;
		}

		private async Task<bool> ProcessEvent(string eventName, string message)
		{
			var processed = false;
			if (_subsManager.HasSubscriptionsForEvent(eventName))
			{
				using (var scope = _serviceProvider.CreateScope())
				{
					var subscriptions = _subsManager.GetHandlersForEvent(eventName);
					foreach (var subscriptionHandlerType in subscriptions)
					{
						var handler = scope.ServiceProvider.GetRequiredService(subscriptionHandlerType);
						if (handler == null) continue;
						var eventType = _subsManager.GetEventTypeByName(eventName);
						var integrationEvent = JsonConvert.DeserializeObject(message, eventType);
						var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
						await (Task)concreteType.GetMethod("Handle").Invoke(handler, new[] { integrationEvent });
					}
				}
				processed = true;
			}
			return processed;
		}

		private void RemoveDefaultRule()
		{
			try
			{
				_subscriptionClient
				 .RemoveRuleAsync(RuleDescription.DefaultRuleName)
				 .GetAwaiter()
				 .GetResult();
			}
			catch (MessagingEntityNotFoundException)
			{
				_logger.LogWarning("The messaging entity {DefaultRuleName} Could not be found.", RuleDescription.DefaultRuleName);
			}
		}
	}
}
