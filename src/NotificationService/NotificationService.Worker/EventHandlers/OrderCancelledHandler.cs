using NotificationService.Worker.Interfaces;
using SharedKernel.Events;

namespace NotificationService.Worker.EventHandlers;

public class OrderCancelledHandler : IIntegrationEventHandler<OrderCancelledIntegrationEvent>
{
    private readonly ILogger<OrderCancelledHandler> _logger;

    public OrderCancelledHandler(ILogger<OrderCancelledHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(OrderCancelledIntegrationEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("OrderCancelled: OrderId={OrderId}, CancelledDate={CancelledDate}, Reason={Reason}",
            @event.OrderId, @event.CancelledDate, @event.Reason ?? "N/A");
        return Task.CompletedTask;
    }

    async Task IIntegrationEventHandler.HandleAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        await HandleAsync((OrderCancelledIntegrationEvent)integrationEvent, cancellationToken);
    }
}