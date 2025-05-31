using NotificationService.Worker.Interfaces;
using SharedKernel.Events;

namespace NotificationService.Worker.EventHandlers;

public class OrderShippedHandler : IIntegrationEventHandler<OrderShippedIntegrationEvent>
{
    private readonly ILogger<OrderShippedHandler> _logger;

    public OrderShippedHandler(ILogger<OrderShippedHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(OrderShippedIntegrationEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling OrderShippedIntegrationEvent: OrderId={OrderId}, ShippedDate={ShippedDate}, Tracking={Tracking}",
            @event.OrderId, @event.ShippedDate, @event.TrackingNumber ?? "N/A");

        return Task.CompletedTask;
    }

    async Task IIntegrationEventHandler.HandleAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        await HandleAsync((OrderShippedIntegrationEvent)integrationEvent, cancellationToken);
    }
}