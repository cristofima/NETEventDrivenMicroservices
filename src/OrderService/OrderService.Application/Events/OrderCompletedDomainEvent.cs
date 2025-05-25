using MediatR;
using OrderService.Domain.Entities;

namespace OrderService.Application.Events;

public class OrderCompletedDomainEvent : INotification
{
    public Order Order { get; }
    public DateTimeOffset CompletedDate { get; }

    public OrderCompletedDomainEvent(Order order, DateTimeOffset completedDate)
    {
        Order = order;
        CompletedDate = completedDate;
    }
}