using NotificationService.Worker.Interfaces;
using SharedKernel.Events;

namespace NotificationService.Worker.EventHandlers;

public class OrderProcessedHandler : IIntegrationEventHandler<OrderProcessedIntegrationEvent>
{
    private readonly ILogger<OrderProcessedHandler> _logger;

    public OrderProcessedHandler(ILogger<OrderProcessedHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(OrderProcessedIntegrationEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("OrderProcessed: OrderId={OrderId}, ProcessedDate={ProcessedDate}",
            @event.OrderId, @event.ProcessedDate);
        return Task.CompletedTask;
    }

    async Task IIntegrationEventHandler.HandleAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        await HandleAsync((OrderProcessedIntegrationEvent)integrationEvent, cancellationToken);
    }
}