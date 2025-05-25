using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Events;
using OrderService.Application.Interfaces;
using SharedKernel.Events;

namespace OrderService.Application.Handlers;

public class OrderShippedDomainEventHandler : INotificationHandler<OrderShippedDomainEvent>
{
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<OrderShippedDomainEventHandler> _logger;

    public OrderShippedDomainEventHandler(IEventPublisher eventPublisher, ILogger<OrderShippedDomainEventHandler> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task Handle(OrderShippedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling OrderShippedDomainEvent for OrderId: {OrderId}. Publishing integration event...", notification.Order.Id);
        var integrationEvent = new OrderShippedIntegrationEvent(
            notification.Order.Id,
            notification.ShippedDate,
            notification.TrackingNumber
        );

        try
        {
            await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);
            _logger.LogInformation("OrderShippedIntegrationEvent published for OrderId: {OrderId}", notification.Order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing OrderShippedIntegrationEvent for OrderId: {OrderId}", notification.Order.Id);
            throw;
        }
    }
}