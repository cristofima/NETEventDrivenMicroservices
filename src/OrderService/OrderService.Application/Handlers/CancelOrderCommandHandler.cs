using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Commands;
using OrderService.Application.Events;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Handlers;

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, bool>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<CancelOrderCommandHandler> _logger;

    public CancelOrderCommandHandler(
        IOrderRepository orderRepository,
        IMediator mediator,
        ILogger<CancelOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<bool> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling CancelOrderCommand for OrderId: {OrderId}", request.OrderId);
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for cancellation.", request.OrderId);
            return false;
        }

        // Add domain logic: e.g., order can only be cancelled if not Shipped or Completed.
        if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Completed)
        {
            _logger.LogWarning("Order {OrderId} is already {Status} and cannot be cancelled.", order.Id, order.Status);
            return false; // Or throw new InvalidOperationException($"Order {order.Id} cannot be cancelled from status {order.Status}.");
        }
        if (order.Status == OrderStatus.Cancelled)
        {
            _logger.LogInformation("Order {OrderId} is already cancelled.", order.Id);
            return true; // Already in desired state
        }

        var cancelledDate = DateTimeOffset.UtcNow;
        order.SetStatus(OrderStatus.Cancelled);
        // You might want to store the cancellation reason on the Order entity.
        // order.CancellationReason = request.Reason;

        await _orderRepository.UpdateAsync(order, cancellationToken);
        _logger.LogInformation("Order {OrderId} status updated to Cancelled. Reason: {Reason}", order.Id, request.Reason ?? "N/A");

        await _mediator.Publish(new OrderCancelledDomainEvent(order, cancelledDate, request.Reason), cancellationToken);
        return true;
    }
}