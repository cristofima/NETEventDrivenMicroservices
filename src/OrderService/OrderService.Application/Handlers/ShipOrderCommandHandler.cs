using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Commands;
using OrderService.Application.Events;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Handlers;

public class ShipOrderCommandHandler : IRequestHandler<ShipOrderCommand, bool>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<ShipOrderCommandHandler> _logger;
    private readonly IOrderStatusTransitionService _statusTransitionService;

    public ShipOrderCommandHandler(
        IOrderRepository orderRepository,
        IMediator mediator,
        ILogger<ShipOrderCommandHandler> logger,
        IOrderStatusTransitionService statusTransitionService)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _statusTransitionService = statusTransitionService ?? throw new ArgumentNullException(nameof(statusTransitionService));
    }

    public async Task<bool> Handle(ShipOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling ShipOrderCommand for OrderId: {OrderId}", request.OrderId);
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("Order with Id {OrderId} not found.", request.OrderId);
            return false;
        }

        try
        {
            var shippedDate = DateTimeOffset.UtcNow;
            order.TrackingNumber = request.TrackingNumber;
            _statusTransitionService.ChangeStatus(order, OrderStatus.Shipped, shippedDate);

            await _orderRepository.UpdateAsync(order, cancellationToken);
            _logger.LogInformation("Order {OrderId} status updated to Shipped. Tracking: {TrackingNumber}", order.Id, request.TrackingNumber ?? "N/A");

            await _mediator.Publish(new OrderShippedDomainEvent(order, shippedDate, request.TrackingNumber), cancellationToken);
            return true;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid status transition");
            return false;
        }
    }
}