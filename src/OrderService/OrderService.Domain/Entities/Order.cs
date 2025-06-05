using System.ComponentModel.DataAnnotations;
using OrderService.Domain.Enums;

namespace OrderService.Domain.Entities;

public class Order
{
    [Key]
    public Guid Id { get; private set; }

    public string CustomerId { get; private set; }
    public DateTimeOffset OrderDate { get; private set; }
    public List<OrderItem> OrderItems { get; private set; }
    public decimal TotalAmount => OrderItems.Sum(item => item.Quantity * item.UnitPrice);
    public OrderStatus Status { get; private set; }
    public string TrackingNumber { get; set; } // Optional tracking number for shipping

    // Timestamps for status changes
    public DateTimeOffset? ProcessingStartedAt { get; private set; }
    public DateTimeOffset? ShippedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string CancellationReason { get; set; }

    // For EF Core
    private Order()
    { }

    public Order(string customerId, List<OrderItem> items)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        OrderDate = DateTimeOffset.UtcNow;
        OrderItems = items ?? throw new ArgumentNullException(nameof(items));
        Status = OrderStatus.Pending;

        if (OrderItems != null && !OrderItems.Any())
        {
            throw new ArgumentException("Order must have at least one item.");
        }
    }

    public void ApplyStatusTransition(OrderStatus newStatus, DateTimeOffset eventDate, string reason = null)
    {
        Status = newStatus;
        switch (newStatus)
        {
            case OrderStatus.Processing:
                ProcessingStartedAt = eventDate;
                break;
            case OrderStatus.Shipped:
                ShippedAt = eventDate;
                break;
            case OrderStatus.Completed:
                CompletedAt = eventDate;
                break;
            case OrderStatus.Cancelled:
                CancelledAt = eventDate;
                CancellationReason = reason;
                break;
            case OrderStatus.Pending:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newStatus), newStatus, null);
        }
    }
}
