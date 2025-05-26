using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    public string? TrackingNumber { get; set; } // Optional tracking number for shipping
    public string? CancellationReason { get; set; }

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

        if (!OrderItems.Any())
        {
            throw new ArgumentException("Order must have at least one item.");
        }
    }

    public void SetStatus(OrderStatus newStatus)
    {
        // Add any domain logic for status transitions if needed
        Status = newStatus;
    }
}

public class OrderItem
{
    [Key]
    public Guid Id { get; private set; }

    public string ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    [ForeignKey("Order")]
    public Guid OrderId { get; private set; }

    public Order Order { get; private set; } = null!;

    // For EF Core
    private OrderItem()
    { }

    public OrderItem(string productId, int quantity, decimal unitPrice)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;

        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.");
    }
}

public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Completed,
    Cancelled
}