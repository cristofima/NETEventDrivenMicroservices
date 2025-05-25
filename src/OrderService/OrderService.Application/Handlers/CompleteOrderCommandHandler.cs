using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Commands;
using OrderService.Application.Events;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Handlers;

public class CompleteOrderCommandHandler : IRequestHandler<CompleteOrderCommand, bool>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<CompleteOrderCommandHandler> _logger;

    public CompleteOrderCommandHandler(
        IOrderRepository orderRepository,
        IMediator mediator,
        ILogger<CompleteOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<bool> Handle(CompleteOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling CompleteOrderCommand for OrderId: {OrderId}", request.OrderId);
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for completion.", request.OrderId);
            return false;
        }

        // Add domain logic: e.g., order must be in 'Shipped' state to be completed.
        if (order.Status != OrderStatus.Shipped)
        {
            _logger.LogWarning("Order {OrderId} is not in Shipped state. Current state: {Status}. Cannot complete.", order.Id, order.Status);
            return false; // Or throw new InvalidOperationException($"Order {order.Id} cannot be completed from status {order.Status}.");
        }

        var completedDate = DateTimeOffset.UtcNow;
        order.SetStatus(OrderStatus.Completed);

        await _orderRepository.UpdateAsync(order, cancellationToken);
        _logger.LogInformation("Order {OrderId} status updated to Completed.", order.Id);

        await _mediator.Publish(new OrderCompletedDomainEvent(order, completedDate), cancellationToken);
        return true;
    }
}