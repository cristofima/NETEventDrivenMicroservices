using NotificationService.Worker.Interfaces;
using SharedKernel.Events;

namespace NotificationService.Worker.EventHandlers;

public class OrderCompletedHandler : IIntegrationEventHandler<OrderCompletedIntegrationEvent>
{
    private readonly ILogger<OrderCompletedHandler> _logger;

    public OrderCompletedHandler(ILogger<OrderCompletedHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(OrderCompletedIntegrationEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation("OrderCompleted: OrderId={OrderId}, CompletedDate={CompletedDate}",
            @event.OrderId, @event.CompletedDate);
        return Task.CompletedTask;
    }
}