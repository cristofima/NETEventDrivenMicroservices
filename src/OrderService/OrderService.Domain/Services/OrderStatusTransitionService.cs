using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;

namespace OrderService.Domain.Services;

public class OrderStatusTransitionService : IOrderStatusTransitionService
{
    public void ChangeStatus(Order order, OrderStatus newStatus, DateTimeOffset eventDate, string reason = null)
    {
        ArgumentNullException.ThrowIfNull(order);

        switch (newStatus)
        {
            case OrderStatus.Processing:
                if (order.Status != OrderStatus.Pending)
                    ThrowInvalidTransition(order.Status, newStatus);
                break;

            case OrderStatus.Shipped:
                if (order.Status != OrderStatus.Processing)
                    ThrowInvalidTransition(order.Status, newStatus, $"Order is not in Processing state. Current state: {order.Status}. Cannot ship.");
                break;

            case OrderStatus.Completed:
                if (order.Status != OrderStatus.Shipped)
                    ThrowInvalidTransition(order.Status, newStatus, $"Order is not in Shipped state. Current state: {order.Status}. Cannot complete.");
                break;

            case OrderStatus.Cancelled:
                switch (order.Status)
                {
                    case OrderStatus.Cancelled:
                        ThrowInvalidTransition(order.Status, newStatus, "Order is already cancelled.");
                        break;

                    case OrderStatus.Completed or OrderStatus.Shipped:
                        ThrowInvalidTransition(order.Status, newStatus, "Cannot cancel a completed or shipped order.");
                        break;
                }

                break;

            default:
                ThrowInvalidTransition(order.Status, newStatus);
                break;
        }

        order.ApplyStatusTransition(newStatus, eventDate, reason);
    }

    private static void ThrowInvalidTransition(OrderStatus current, OrderStatus newStatus, string customMessage = null)
    {
        throw new InvalidOperationException(customMessage ?? $"Invalid transition from {current} to {newStatus}.");
    }
}