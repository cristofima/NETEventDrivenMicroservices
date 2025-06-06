using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Commands;
using OrderService.Application.Events;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Handlers;

public class CompleteOrderCommandHandler : IRequestHandler<CompleteOrderCommand, bool>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<CompleteOrderCommandHandler> _logger;
    private readonly IOrderStatusTransitionService _statusTransitionService;

    public CompleteOrderCommandHandler(
        IOrderRepository orderRepository,
        IMediator mediator,
        ILogger<CompleteOrderCommandHandler> logger,
        IOrderStatusTransitionService statusTransitionService)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _statusTransitionService = statusTransitionService ?? throw new ArgumentNullException(nameof(statusTransitionService));
    }

    public async Task<bool> Handle(CompleteOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling CompleteOrderCommand for OrderId: {OrderId}", request.OrderId);
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("Order with Id {OrderId} not found.", request.OrderId);
            return false;
        }

        try
        {
            var completedDate = DateTimeOffset.UtcNow;
            _statusTransitionService.ChangeStatus(order, OrderStatus.Completed, completedDate);

            await _orderRepository.UpdateAsync(order, cancellationToken);
            _logger.LogInformation("Order {OrderId} status updated to Completed.", order.Id);

            await _mediator.Publish(new OrderCompletedDomainEvent(order, completedDate), cancellationToken);
            return true;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid status transition");
            return false;
        }
    }
}