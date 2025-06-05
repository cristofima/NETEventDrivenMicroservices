using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Commands;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Handlers;

public class ProcessOrderCommandHandler : IRequestHandler<ProcessOrderCommand, bool>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<ProcessOrderCommandHandler> _logger;
    private readonly IOrderStatusTransitionService _statusTransitionService;

    public ProcessOrderCommandHandler(
        IOrderRepository orderRepository,
        IMediator mediator,
        ILogger<ProcessOrderCommandHandler> logger,
        IOrderStatusTransitionService statusTransitionService)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _statusTransitionService = statusTransitionService ?? throw new ArgumentNullException(nameof(statusTransitionService)); ;
    }

    public async Task<bool> Handle(ProcessOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling ProcessOrderCommand for OrderId: {OrderId}", request.OrderId);

        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("Order with Id {OrderId} not found.", request.OrderId);
            return false; // Or throw a NotFoundException
        }

        try
        {
            var processedAt = DateTimeOffset.UtcNow;
            _statusTransitionService.ChangeStatus(order, OrderStatus.Processing, processedAt);

            await _orderRepository.UpdateAsync(order, cancellationToken);
            _logger.LogInformation("Order {OrderId} status updated to Processing.", order.Id);

            await _mediator.Publish(new OrderProcessedDomainEvent(order, processedAt), cancellationToken);
            return true;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid status transition");
            return false;
        }
    }
}