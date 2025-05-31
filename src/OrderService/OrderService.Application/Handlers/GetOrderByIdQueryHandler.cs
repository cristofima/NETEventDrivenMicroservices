using MediatR;
using OrderService.Application.Queries;
using OrderService.Domain.DTO;

namespace OrderService.Application.Handlers;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderViewModel>
{
    private readonly Domain.Interfaces.IOrderRepository _orderRepository;

    public GetOrderByIdQueryHandler(Domain.Interfaces.IOrderRepository orderRepository)
    { _orderRepository = orderRepository; }

    public async Task<OrderViewModel> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order == null) return null;
        return new OrderViewModel
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            OrderDate = order.OrderDate,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            OrderItems = [.. order.OrderItems.Select(oi => new OrderItemViewModel
            {
                Id = oi.Id,
                ProductId = oi.ProductId,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice
            })]
        };
    }
}