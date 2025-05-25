using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Commands;
using OrderService.Application.Events;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Handlers;

public class ShipOrderCommandHandler : IRequestHandler<ShipOrderCommand, bool>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<ShipOrderCommandHandler> _logger;

    public ShipOrderCommandHandler(
        IOrderRepository orderRepository,
        IMediator mediator,
        ILogger<ShipOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<bool> Handle(ShipOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling ShipOrderCommand for OrderId: {OrderId}", request.OrderId);
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for shipping.", request.OrderId);
            return false;
        }

        // Add domain logic: e.g., order must be in 'Processing' state to be shipped.
        if (order.Status != OrderStatus.Processing)
        {
            _logger.LogWarning("Order {OrderId} is not in Processing state. Current state: {Status}. Cannot ship.", order.Id, order.Status);
            // Consider throwing an InvalidOperationException or returning a specific error response.
            return false; // Or throw new InvalidOperationException($"Order {order.Id} cannot be shipped from status {order.Status}.");
        }

        var shippedDate = DateTimeOffset.UtcNow;
        order.SetStatus(OrderStatus.Shipped);
        // You might want to store trackingNumber on the Order entity if it's relevant.
        // order.TrackingNumber = request.TrackingNumber;

        await _orderRepository.UpdateAsync(order, cancellationToken);
        _logger.LogInformation("Order {OrderId} status updated to Shipped. Tracking: {TrackingNumber}", order.Id, request.TrackingNumber ?? "N/A");

        await _mediator.Publish(new OrderShippedDomainEvent(order, shippedDate, request.TrackingNumber), cancellationToken);
        return true;
    }
}