using MediatR;
using OrderService.Domain.Entities;

namespace OrderService.Application.Commands;

public class OrderProcessedDomainEvent : INotification
{
    public Order Order { get; }
    public DateTimeOffset ProcessedDate { get; }

    public OrderProcessedDomainEvent(Order order, DateTimeOffset processedDate)
    {
        Order = order;
        ProcessedDate = processedDate;
    }
}