using MediatR;

namespace OrderService.Application.Commands;

public class CompleteOrderCommand : IRequest<bool>
{
    public Guid OrderId { get; }

    public CompleteOrderCommand(Guid orderId)
    {
        OrderId = orderId;
    }
}