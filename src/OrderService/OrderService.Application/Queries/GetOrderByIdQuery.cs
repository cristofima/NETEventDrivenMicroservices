using MediatR;
using OrderService.Domain.DTO;

namespace OrderService.Application.Queries;

public class GetOrderByIdQuery : IRequest<OrderViewModel?>
{
    public Guid OrderId { get; }

    public GetOrderByIdQuery(Guid orderId)
    { OrderId = orderId; }
}