using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces;
using Polly;
using Polly.Retry;
using SharedKernel.Events;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace OrderService.Infrastructure.Messaging;

[ExcludeFromCodeCoverage]
public class ServiceBusEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ServiceBusSender _serviceBusSender;
    private readonly ILogger<ServiceBusEventPublisher> _logger;
    private readonly string _topicName;
    private readonly AsyncRetryPolicy _retryPolicy;

    public ServiceBusEventPublisher(IConfiguration configuration, ILogger<ServiceBusEventPublisher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var connectionString = configuration["AzureServiceBus:ConnectionString"];
        _topicName = configuration["AzureServiceBus:OrderCreatedTopicName"];

        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogError("Azure Service Bus connection string is not configured.");
            throw new InvalidOperationException("Azure Service Bus connection string is not configured.");
        }
        if (string.IsNullOrEmpty(_topicName))
        {
            _logger.LogError("Azure Service Bus Topic Name is not configured.");
            throw new InvalidOperationException("Azure Service Bus Topic Name is not configured.");
        }

        _serviceBusClient = new ServiceBusClient(connectionString);
        _serviceBusSender = _serviceBusClient.CreateSender(_topicName);

        // Define a Polly retry policy for publishing
        _retryPolicy = Policy
            .Handle<ServiceBusException>(ex => ex.IsTransient) // Retry only on transient Service Bus errors
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)), // Exponential backoff: 2s, 4s, 8s
                onRetry: (exception, timespan, attempt, context) =>
                {
                    _logger.LogWarning(exception, "Error publishing event to Service Bus. Retrying in {Timespan}. Attempt {Attempt}", timespan, attempt);
                });
    }

    public async Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default) where T : IntegrationEvent
    {
        if (integrationEvent == null) throw new ArgumentNullException(nameof(integrationEvent));

        var eventName = typeof(T).Name;
        _logger.LogInformation("Publishing event {EventName} with ID {EventId} to Service Bus topic {TopicName}",
            eventName, integrationEvent.Id, _topicName);

        var messageBody = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType()); // Serialize with actual type
        var serviceBusMessage = new ServiceBusMessage(messageBody)
        {
            MessageId = integrationEvent.Id.ToString(),
            ContentType = "application/json",
            Subject = eventName // Useful for filtering on the subscriber side if needed
        };

        // Execute publish action with Polly retry policy
        await _retryPolicy.ExecuteAsync(async token =>
        {
            await _serviceBusSender.SendMessageAsync(serviceBusMessage, token);
            _logger.LogInformation("Event {EventName} with ID {EventId} sent successfully to topic {TopicName}.",
                eventName, integrationEvent.Id, _topicName);
        }, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _serviceBusSender.DisposeAsync();
        await _serviceBusClient.DisposeAsync();
        _logger.LogInformation("ServiceBusEventPublisher disposed.");
    }
}