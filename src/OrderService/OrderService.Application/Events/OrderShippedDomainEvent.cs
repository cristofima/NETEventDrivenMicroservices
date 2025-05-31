using MediatR;
using OrderService.Domain.Entities;

namespace OrderService.Application.Events;

public class OrderShippedDomainEvent : INotification
{
    public Order Order { get; }
    public DateTimeOffset ShippedDate { get; }
    public string TrackingNumber { get; }

    public OrderShippedDomainEvent(Order order, DateTimeOffset shippedDate, string trackingNumber = null)
    {
        Order = order;
        ShippedDate = shippedDate;
        TrackingNumber = trackingNumber;
    }
}