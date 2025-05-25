using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Events;
using OrderService.Application.Interfaces;
using SharedKernel.Events;

namespace OrderService.Application.Handlers;

/// <summary>
/// Handles the OrderCreatedDomainEvent (an internal domain event).
/// Its responsibility is to publish an OrderCreatedIntegrationEvent to the external message bus.
/// </summary>
public class OrderCreatedDomainEventHandler : INotificationHandler<OrderCreatedDomainEvent>
{
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<OrderCreatedDomainEventHandler> _logger;

    public OrderCreatedDomainEventHandler(IEventPublisher eventPublisher, ILogger<OrderCreatedDomainEventHandler> logger)
    {
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(OrderCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling OrderCreatedDomainEvent for OrderId: {OrderId}. Publishing integration event...", notification.Order.Id);

        var integrationEvent = new OrderCreatedIntegrationEvent(
            notification.Order.Id,
            notification.Order.CustomerId,
            notification.Order.OrderItems.Select(oi => new OrderItemData(oi.ProductId, oi.Quantity, oi.UnitPrice)).ToList(),
            notification.Order.TotalAmount
        );

        try
        {
            await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);
            _logger.LogInformation("OrderCreatedIntegrationEvent published successfully for OrderId: {OrderId}", notification.Order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing OrderCreatedIntegrationEvent for OrderId: {OrderId}", notification.Order.Id);
            // Consider retry mechanisms or dead-lettering strategies here in a real application.
            throw; // Re-throw to indicate failure if critical
        }
    }
}