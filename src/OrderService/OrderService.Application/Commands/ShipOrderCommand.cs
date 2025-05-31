using MediatR;

namespace OrderService.Application.Commands;

public class ShipOrderCommand : IRequest<bool>
{
    public Guid OrderId { get; }
    public string TrackingNumber { get; }

    public ShipOrderCommand(Guid orderId, string trackingNumber = null)
    {
        OrderId = orderId;
        TrackingNumber = trackingNumber;
    }
}