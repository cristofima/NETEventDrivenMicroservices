using MediatR;
using OrderService.Domain.Entities;

namespace OrderService.Application.Events;

public class OrderCancelledDomainEvent : INotification
{
    public Order Order { get; }
    public DateTimeOffset CancelledDate { get; }
    public string? Reason { get; }

    public OrderCancelledDomainEvent(Order order, DateTimeOffset cancelledDate, string? reason)
    {
        Order = order;
        CancelledDate = cancelledDate;
        Reason = reason;
    }
}