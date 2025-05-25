using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Commands;
using OrderService.Application.Events;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Handlers;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMediator _mediator; // For publishing domain events
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IMediator mediator,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling CreateOrderCommand for CustomerId: {CustomerId}", request.CustomerId);

        var orderItems = request.ProductItems
            .Select(item => new OrderItem(item.ProductId, item.Quantity, item.UnitPrice))
            .ToList();

        var order = new Order(request.CustomerId, orderItems);

        await _orderRepository.AddAsync(order, cancellationToken);

        _logger.LogInformation("Order {OrderId} created successfully for CustomerId: {CustomerId}", order.Id, request.CustomerId);

        // Publish a domain event. This will be handled by OrderCreatedDomainEventHandler
        // which in turn will publish an integration event.
        await _mediator.Publish(new OrderCreatedDomainEvent(order), cancellationToken);

        return order.Id;
    }
}