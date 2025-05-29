using NotificationService.Worker.Interfaces;
using SharedKernel.Events;
using System.Text.Json;

namespace NotificationService.Worker.EventHandlers;

public class IntegrationEventHandlerFactory : IIntegrationEventHandlerFactory
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IntegrationEventHandlerFactory> _logger;

    public IntegrationEventHandlerFactory(IServiceScopeFactory scopeFactory, ILogger<IntegrationEventHandlerFactory> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<bool> TryHandleAsync(string eventTypeName, string messageBody, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var provider = scope.ServiceProvider;

        var handler = GetHandlerByEventType(eventTypeName, provider);
        if (handler is null)
        {
            _logger.LogWarning("No handler found for event type '{EventType}'.", eventTypeName);
            return false;
        }

        var integrationEvent = DeserializeEvent(eventTypeName, messageBody);
        if (integrationEvent == null) return false;

        await handler.HandleAsync(integrationEvent, cancellationToken);
        return true;
    }

    private IIntegrationEventHandler GetHandlerByEventType(string eventTypeName, IServiceProvider provider)
    {
        return eventTypeName switch
        {
            nameof(OrderCreatedIntegrationEvent) => provider.GetService<IIntegrationEventHandler<OrderCreatedIntegrationEvent>>(),
            nameof(OrderProcessedIntegrationEvent) => provider.GetService<IIntegrationEventHandler<OrderProcessedIntegrationEvent>>(),
            nameof(OrderShippedIntegrationEvent) => provider.GetService<IIntegrationEventHandler<OrderShippedIntegrationEvent>>(),
            nameof(OrderCompletedIntegrationEvent) => provider.GetService<IIntegrationEventHandler<OrderCompletedIntegrationEvent>>(),
            nameof(OrderCancelledIntegrationEvent) => provider.GetService<IIntegrationEventHandler<OrderCancelledIntegrationEvent>>(),
            _ => null
        };
    }

    private IntegrationEvent? DeserializeEvent(string eventTypeName, string messageBody)
    {
        try
        {
            return eventTypeName switch
            {
                nameof(OrderCreatedIntegrationEvent) => JsonSerializer.Deserialize<OrderCreatedIntegrationEvent>(messageBody),
                nameof(OrderProcessedIntegrationEvent) => JsonSerializer.Deserialize<OrderProcessedIntegrationEvent>(messageBody),
                nameof(OrderShippedIntegrationEvent) => JsonSerializer.Deserialize<OrderShippedIntegrationEvent>(messageBody),
                nameof(OrderCompletedIntegrationEvent) => JsonSerializer.Deserialize<OrderCompletedIntegrationEvent>(messageBody),
                nameof(OrderCancelledIntegrationEvent) => JsonSerializer.Deserialize<OrderCancelledIntegrationEvent>(messageBody),
                _ => null
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize message for event type '{EventType}'", eventTypeName);
            return null;
        }
    }
}