using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Commands;
using OrderService.Application.Events;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Handlers;

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, bool>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<CancelOrderCommandHandler> _logger;
    private readonly IOrderStatusTransitionService _statusTransitionService;

    public CancelOrderCommandHandler(
        IOrderRepository orderRepository,
        IMediator mediator,
        ILogger<CancelOrderCommandHandler> logger,
        IOrderStatusTransitionService statusTransitionService)
    {
        _orderRepository = orderRepository;
        _mediator = mediator;
        _logger = logger;
        _statusTransitionService = statusTransitionService;
    }

    public async Task<bool> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling CancelOrderCommand for OrderId: {OrderId}", request.OrderId);
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("Order with Id {OrderId} not found.", request.OrderId);
            return false;
        }

        try
        {
            var cancelledDate = DateTimeOffset.UtcNow;
            _statusTransitionService.ChangeStatus(order, OrderStatus.Cancelled, cancelledDate, request.Reason);

            await _orderRepository.UpdateAsync(order, cancellationToken);
            _logger.LogInformation("Order {OrderId} status updated to Cancelled. Reason: {Reason}", order.Id, request.Reason ?? "N/A");

            await _mediator.Publish(new OrderCancelledDomainEvent(order, cancelledDate, request.Reason), cancellationToken);
            return true;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid status transition");
            return false;
        }
    }
}