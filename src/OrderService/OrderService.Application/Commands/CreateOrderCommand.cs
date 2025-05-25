using MediatR;

namespace OrderService.Application.Commands;

public class CreateOrderCommand : IRequest<Guid>
{
    public string CustomerId { get; set; }
    public List<OrderItemDto> ProductItems { get; set; } = new();

    public CreateOrderCommand(string customerId, List<OrderItemDto> productItems)
    {
        CustomerId = customerId;
        ProductItems = productItems;
    }
}

public class OrderItemDto
{
    public string ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public OrderItemDto(string productId, int quantity, decimal unitPrice)
    {
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}