using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderService.Domain.Entities;

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