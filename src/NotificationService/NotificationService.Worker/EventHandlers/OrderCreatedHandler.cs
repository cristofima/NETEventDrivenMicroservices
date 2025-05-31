using NotificationService.Worker.Interfaces;
using SharedKernel.Events;

namespace NotificationService.Worker.EventHandlers;

public class OrderCreatedHandler : IIntegrationEventHandler<OrderCreatedIntegrationEvent>
{
    private readonly ILogger<OrderCreatedHandler> _logger;

    public OrderCreatedHandler(ILogger<OrderCreatedHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(OrderCreatedIntegrationEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("OrderCreated: OrderId={OrderId}, Customer={CustomerId}, Total={TotalAmount}",
            @event.OrderId, @event.CustomerId, @event.TotalAmount);
        return Task.CompletedTask;
    }

    async Task IIntegrationEventHandler.HandleAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        await HandleAsync((OrderCreatedIntegrationEvent)integrationEvent, cancellationToken);
    }
}