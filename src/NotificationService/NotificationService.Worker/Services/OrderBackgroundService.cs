using System.Text.Json;
using Azure.Messaging.ServiceBus;
using NotificationService.Worker.Interfaces;

namespace NotificationService.Worker.Services;

public class OrderBackgroundService : BackgroundService
{
    private readonly ILogger<OrderBackgroundService> _logger;
    private readonly ServiceBusProcessor _processor;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly IIntegrationEventHandlerFactory _handlerFactory;
    private readonly string _topicName;
    private readonly string _subscriptionName;

    public OrderBackgroundService(
        IConfiguration configuration,
        ILogger<OrderBackgroundService> logger,
        IIntegrationEventHandlerFactory handlerFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _handlerFactory = handlerFactory ?? throw new ArgumentNullException(nameof(handlerFactory));

        var connectionString = configuration["AzureServiceBus:ConnectionString"];
        _topicName = configuration["AzureServiceBus:TopicName"];
        _subscriptionName = configuration["AzureServiceBus:SubscriptionName"];

        if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(_topicName) || string.IsNullOrWhiteSpace(_subscriptionName))
        {
            _logger.LogError("Azure Service Bus configuration is missing or incomplete.");
            throw new InvalidOperationException("Azure Service Bus configuration is missing for NotificationService.");
        }

        _serviceBusClient = new ServiceBusClient(connectionString);
        _processor = _serviceBusClient.CreateProcessor(_topicName, _subscriptionName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1,
            ReceiveMode = ServiceBusReceiveMode.PeekLock
        });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderEventsHandler started. Listening to topic '{TopicName}' and subscription '{SubscriptionName}'", _topicName, _subscriptionName);

        _processor.ProcessMessageAsync += MessageHandlerAsync;
        _processor.ProcessErrorAsync += ErrorHandlerAsync;

        await _processor.StartProcessingAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        await _processor.StopProcessingAsync(CancellationToken.None);
        _logger.LogInformation("OrderEventsHandler stopped.");
    }

    private async Task MessageHandlerAsync(ProcessMessageEventArgs args)
    {
        var body = args.Message.Body.ToString();
        var eventType = args.Message.Subject ?? "UnknownEvent";
        var sequenceNumber = args.Message.SequenceNumber;

        _logger.LogInformation("Received message: SequenceNumber={SequenceNumber}, Subject={Subject}, Body={Body}", sequenceNumber, eventType, body);

        try
        {
            var handled = await _handlerFactory.TryHandleAsync(eventType, body, args.CancellationToken);

            if (!handled && eventType != "UnknownEvent")
            {
                _logger.LogWarning("No handler found or deserialization failed for event type '{EventType}'.", eventType);
            }

            await args.CompleteMessageAsync(args.Message, args.CancellationToken);
            _logger.LogInformation("Message {SequenceNumber} (Subject: {Subject}) completed successfully.", sequenceNumber, eventType);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "Deserialization failed for message {SequenceNumber} (Subject: {Subject}). Dead-lettering message.", sequenceNumber, eventType);
            await args.DeadLetterMessageAsync(args.Message, "DeserializationError", jsonEx.Message, args.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message {SequenceNumber} (Subject: {Subject}). Abandoning for retry.", sequenceNumber, eventType);
            await args.AbandonMessageAsync(args.Message, null, args.CancellationToken);
        }
    }

    private Task ErrorHandlerAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Service Bus error. Source: {ErrorSource}, Entity: {EntityPath}, Namespace: {Namespace}",
            args.ErrorSource, args.EntityPath, args.FullyQualifiedNamespace);

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("OrderEventsHandler is stopping. Cleaning up resources...");

        if (_processor != null)
        {
            await _processor.DisposeAsync();
        }

        if (_serviceBusClient != null)
        {
            await _serviceBusClient.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
        _logger.LogInformation("OrderEventsHandler shutdown complete.");
    }
}