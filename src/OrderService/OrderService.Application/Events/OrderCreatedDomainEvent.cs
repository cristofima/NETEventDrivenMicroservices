using MediatR;
using OrderService.Domain.Entities;

namespace OrderService.Application.Events;

/// <summary>
/// Domain event raised when an order is successfully created and saved.
/// This event is handled within the OrderService itself, typically to trigger side effects
/// like publishing an integration event.
/// </summary>
public class OrderCreatedDomainEvent : INotification
{
    public Order Order { get; }

    public OrderCreatedDomainEvent(Order order)
    {
        Order = order;
    }
}