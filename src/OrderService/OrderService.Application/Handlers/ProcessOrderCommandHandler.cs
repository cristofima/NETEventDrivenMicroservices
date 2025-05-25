using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Commands;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Handlers;

public class ProcessOrderCommandHandler : IRequestHandler<ProcessOrderCommand, bool>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<ProcessOrderCommandHandler> _logger;

    public ProcessOrderCommandHandler(
        IOrderRepository orderRepository,
        IMediator mediator,
        ILogger<ProcessOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        // Add domain logic here if needed to check if the order can be processed
        // For example, check current status: if (order.Status != OrderStatus.Pending) throw new InvalidOperationException("Order cannot be processed.");

        var processedDate = DateTimeOffset.UtcNow;
        order.SetStatus(OrderStatus.Processing);
        // In a real scenario, you might have specific properties to update when an order is processed.
        // order.ProcessedDate = processedDate; (if Order entity had such a property)

        // EF Core tracks changes, so calling SaveChangesAsync on the DbContext
        // (which OrderRepository's AddAsync/UpdateAsync would do) will persist the status change.
        // We assume IOrderRepository.UpdateAsync or similar persists changes.
        // For this example, let's ensure the repository handles the update.
        // If IOrderRepository doesn't have an explicit UpdateAsync, it implies SaveChanges is handled at a higher level (e.g., Unit of Work)
        // or that AddAsync also handles updates if the entity is tracked.
        // For simplicity, let's assume GetByIdAsync tracks the entity, and SaveChangesAsync is called by the repository or UoW.
        // If using a generic repository that doesn't call SaveChanges, you'd need to call it here or ensure the UoW does.
        // Let's refine IOrderRepository to include an UpdateAsync method.
        await _orderRepository.UpdateAsync(order, cancellationToken);

        _logger.LogInformation("Order {OrderId} status updated to Processing.", order.Id);

        await _mediator.Publish(new OrderProcessedDomainEvent(order, processedDate), cancellationToken);

        return true;
    }
}