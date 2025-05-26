using Azure.Messaging.ServiceBus;
using SharedKernel.Events;
using System.Text.Json;

namespace NotificationService.Worker.EventHandlers;

public class OrderEventsHandler : BackgroundService
{
    private readonly ILogger<OrderEventsHandler> _logger;
    private readonly ServiceBusProcessor _processor;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly string _topicName;
    private readonly string _subscriptionName;

    public OrderEventsHandler(IConfiguration configuration, ILogger<OrderEventsHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var connectionString = configuration["AzureServiceBus:ConnectionString"];
        _topicName = configuration["AzureServiceBus:TopicName"];
        _subscriptionName = configuration["AzureServiceBus:SubscriptionName"];

        if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(_topicName) || string.IsNullOrEmpty(_subscriptionName))
        {
            _logger.LogError("Azure Service Bus configuration is missing or incomplete.");
            throw new InvalidOperationException("Azure Service Bus configuration is missing for NotificationService.");
        }

        _serviceBusClient = new ServiceBusClient(connectionString);
        _processor = _serviceBusClient.CreateProcessor(_topicName, _subscriptionName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1, // Process one message at a time for simplicity
            ReceiveMode = ServiceBusReceiveMode.PeekLock // Default, good for manual completion
        });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderEventsHandler starting. Listening to Service Bus topic '{TopicName}' with subscription '{SubscriptionName}'.", _topicName, _subscriptionName);

        _processor.ProcessMessageAsync += MessageHandler;
        _processor.ProcessErrorAsync += ErrorHandler;

        await _processor.StartProcessingAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogTrace("OrderEventsHandler is active at: {time}", DateTimeOffset.Now);
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        _logger.LogInformation("OrderEventsHandler stopping.");
        await _processor.StopProcessingAsync(CancellationToken.None);
    }

    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        string body = args.Message.Body.ToString();
        string eventType = args.Message.Subject ?? "UnknownEvent"; // Use Subject to determine event type
        string orderId = "UnknownOrderId";

        _logger.LogInformation("Received message: SequenceNumber:{SequenceNumber}, Subject:{Subject}, Body:{Body}",
            args.Message.SequenceNumber, eventType, body);

        try
        {
            bool processed = false;
            switch (eventType)
            {
                case nameof(OrderCreatedIntegrationEvent):
                    var orderCreatedEvent = JsonSerializer.Deserialize<OrderCreatedIntegrationEvent>(body);
                    if (orderCreatedEvent != null)
                    {
                        orderId = orderCreatedEvent.OrderId.ToString();
                        _logger.LogInformation(
                            "Processing OrderCreatedIntegrationEvent for OrderId: {OrderId}, Customer: {CustomerId}, Total: {TotalAmount}",
                            orderCreatedEvent.OrderId, orderCreatedEvent.CustomerId, orderCreatedEvent.TotalAmount);
                        // Simulate notification for order creation
                        processed = true;
                    }
                    break;

                case nameof(OrderProcessedIntegrationEvent):
                    var orderProcessedEvent = JsonSerializer.Deserialize<OrderProcessedIntegrationEvent>(body);
                    if (orderProcessedEvent != null)
                    {
                        orderId = orderProcessedEvent.OrderId.ToString();
                        _logger.LogInformation(
                            "Processing OrderProcessedIntegrationEvent for OrderId: {OrderId} at {ProcessedDate}",
                            orderProcessedEvent.OrderId, orderProcessedEvent.ProcessedDate);
                        // Simulate notification for order processing
                        processed = true;
                    }
                    break;

                case nameof(OrderShippedIntegrationEvent):
                    var orderShippedEvent = JsonSerializer.Deserialize<OrderShippedIntegrationEvent>(body);
                    if (orderShippedEvent != null)
                    {
                        orderId = orderShippedEvent.OrderId.ToString();
                        _logger.LogInformation(
                            "Processing OrderShippedIntegrationEvent for OrderId: {OrderId} at {ShippedDate}, Tracking: {TrackingNumber}",
                            orderShippedEvent.OrderId, orderShippedEvent.ShippedDate, orderShippedEvent.TrackingNumber ?? "N/A");
                        // Simulate notification for order shipment
                        processed = true;
                    }
                    break;

                case nameof(OrderCompletedIntegrationEvent):
                    var orderCompletedEvent = JsonSerializer.Deserialize<OrderCompletedIntegrationEvent>(body);
                    if (orderCompletedEvent != null)
                    {
                        orderId = orderCompletedEvent.OrderId.ToString();
                        _logger.LogInformation(
                            "Processing OrderCompletedIntegrationEvent for OrderId: {OrderId} at {CompletedDate}",
                            orderCompletedEvent.OrderId, orderCompletedEvent.CompletedDate);
                        // Simulate notification for order completion
                        processed = true;
                    }
                    break;

                case nameof(OrderCancelledIntegrationEvent):
                    var orderCancelledEvent = JsonSerializer.Deserialize<OrderCancelledIntegrationEvent>(body);
                    if (orderCancelledEvent != null)
                    {
                        orderId = orderCancelledEvent.OrderId.ToString();
                        _logger.LogInformation(
                            "Processing OrderCancelledIntegrationEvent for OrderId: {OrderId} at {CancelledDate}, Reason: {Reason}",
                            orderCancelledEvent.OrderId, orderCancelledEvent.CancelledDate, orderCancelledEvent.Reason ?? "N/A");
                        // Simulate notification for order cancellation
                        processed = true;
                    }
                    break;

                default:
                    _logger.LogWarning("Received message with unhandled subject/event type '{Subject}'. Body: {Body}", eventType, body);
                    // Decide if you want to dead-letter unhandled known subjects or just complete them if they are not errors.
                    // For now, we'll complete it to avoid it being reprocessed indefinitely if it's not an error.
                    processed = true; // Mark as processed to complete the message.
                    break;
            }

            if (processed)
            {
                // Simulate actual notification work for any processed event
                await Task.Delay(TimeSpan.FromSeconds(1), args.CancellationToken); // Simulate work
                _logger.LogInformation("Notification simulated for event type {EventType}, OrderId {OrderId}.", eventType, orderId);
            }
            else if (!string.IsNullOrEmpty(eventType) && eventType != "UnknownEvent") // Check if it was a known event type but failed deserialization
            {
                _logger.LogWarning("Failed to deserialize message body for event type {EventType}. Body: {Body}", eventType, body);
                // This case should ideally be caught by JsonException below if deserialization fails.
                // If it gets here, it means Deserialize<T> returned null without throwing.
            }

            await args.CompleteMessageAsync(args.Message, args.CancellationToken);
            _logger.LogInformation("Message {SequenceNumber} (Subject: {Subject}) completed.", args.Message.SequenceNumber, eventType);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "Error deserializing message {SequenceNumber} (Subject: {Subject}). Body: {Body}. Moving to dead-letter queue.",
                args.Message.SequenceNumber, eventType, body);
            await args.DeadLetterMessageAsync(args.Message, "DeserializationError", jsonEx.Message, args.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message {SequenceNumber} (Subject: {Subject}). Body: {Body}. Will attempt to abandon for retry.",
                args.Message.SequenceNumber, eventType, body);
            await args.AbandonMessageAsync(args.Message, null, args.CancellationToken);
        }
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Message handler encountered an exception. ErrorSource: {ErrorSource}, EntityPath: {EntityPath}, Namespace: {FullyQualifiedNamespace}",
            args.ErrorSource, args.EntityPath, args.FullyQualifiedNamespace);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("OrderEventsHandler stopping. Disposing resources.");
        if (_processor != null)
        {
            await _processor.DisposeAsync();
        }
        if (_serviceBusClient != null)
        {
            await _serviceBusClient.DisposeAsync();
        }
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("OrderEventsHandler stopped and resources disposed.");
    }
}