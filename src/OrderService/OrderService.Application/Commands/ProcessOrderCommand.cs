using MediatR;

namespace OrderService.Application.Commands;

public class ProcessOrderCommand : IRequest<bool>
{
    public Guid OrderId { get; }

    public ProcessOrderCommand(Guid orderId)
    {
        OrderId = orderId;
    }
}