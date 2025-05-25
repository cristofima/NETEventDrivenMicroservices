using MediatR;

namespace OrderService.Application.Commands;

public class CancelOrderCommand : IRequest<bool>
{
    public Guid OrderId { get; }
    public string? Reason { get; }

    public CancelOrderCommand(Guid orderId, string? reason = null)
    {
        OrderId = orderId;
        Reason = reason;
    }
}