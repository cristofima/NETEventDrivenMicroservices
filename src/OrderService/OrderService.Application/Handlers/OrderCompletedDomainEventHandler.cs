using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Events;
using OrderService.Application.Interfaces;
using SharedKernel.Events;

namespace OrderService.Application.Handlers;

public class OrderCompletedDomainEventHandler : INotificationHandler<OrderCompletedDomainEvent>
{
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<OrderCompletedDomainEventHandler> _logger;

    public OrderCompletedDomainEventHandler(IEventPublisher eventPublisher, ILogger<OrderCompletedDomainEventHandler> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task Handle(OrderCompletedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling OrderCompletedDomainEvent for OrderId: {OrderId}. Publishing integration event...", notification.Order.Id);
        var integrationEvent = new OrderCompletedIntegrationEvent(
            notification.Order.Id,
            notification.CompletedDate
        );

        try
        {
            await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);
            _logger.LogInformation("OrderCompletedIntegrationEvent published for OrderId: {OrderId}", notification.Order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing OrderCompletedIntegrationEvent for OrderId: {OrderId}", notification.Order.Id);
            throw;
        }
    }
}