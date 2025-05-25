using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Commands;
using OrderService.Application.Interfaces;
using SharedKernel.Events;

namespace OrderService.Application.Handlers;

public class OrderProcessedDomainEventHandler : INotificationHandler<OrderProcessedDomainEvent>
{
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<OrderProcessedDomainEventHandler> _logger;

    public OrderProcessedDomainEventHandler(IEventPublisher eventPublisher, ILogger<OrderProcessedDomainEventHandler> logger)
    {
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(OrderProcessedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling OrderProcessedDomainEvent for OrderId: {OrderId}. Publishing integration event...", notification.Order.Id);

        var integrationEvent = new OrderProcessedIntegrationEvent(
            notification.Order.Id,
            notification.ProcessedDate
        );

        try
        {
            await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);
            _logger.LogInformation("OrderProcessedIntegrationEvent published successfully for OrderId: {OrderId}", notification.Order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing OrderProcessedIntegrationEvent for OrderId: {OrderId}", notification.Order.Id);
            throw;
        }
    }
}