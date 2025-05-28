using System.Text.Json;
using NotificationService.Worker.Interfaces;
using SharedKernel.Events;

namespace NotificationService.Worker.EventHandlers;

public class IntegrationEventHandlerFactory : IIntegrationEventHandlerFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Func<string, CancellationToken, Task<bool>>> _handlers;

    public IntegrationEventHandlerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _handlers = new Dictionary<string, Func<string, CancellationToken, Task<bool>>>(StringComparer.OrdinalIgnoreCase)
        {
            [nameof(OrderCreatedIntegrationEvent)] = Handle<OrderCreatedIntegrationEvent>,
            [nameof(OrderProcessedIntegrationEvent)] = Handle<OrderProcessedIntegrationEvent>,
            [nameof(OrderShippedIntegrationEvent)] = Handle<OrderShippedIntegrationEvent>,
            [nameof(OrderCompletedIntegrationEvent)] = Handle<OrderCompletedIntegrationEvent>,
            [nameof(OrderCancelledIntegrationEvent)] = Handle<OrderCancelledIntegrationEvent>
        };
    }

    public Task<bool> TryHandleAsync(string eventType, string body, CancellationToken cancellationToken)
    {
        return _handlers.TryGetValue(eventType, out var handler)
            ? handler(body, cancellationToken)
            : Task.FromResult(false);
    }

    private async Task<bool> Handle<T>(string body, CancellationToken cancellationToken)
    {
        var evt = JsonSerializer.Deserialize<T>(body);
        if (evt == null) return false;

        var handler = _serviceProvider.GetService(typeof(IIntegrationEventHandler<T>)) as IIntegrationEventHandler<T>;
        if (handler == null) return false;

        await handler.HandleAsync(evt, cancellationToken);
        return true;
    }
}