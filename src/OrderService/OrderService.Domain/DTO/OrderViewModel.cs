namespace OrderService.Domain.DTO;

public class OrderViewModel
{
    public Guid Id { get; set; }
    public string CustomerId { get; set; }
    public DateTimeOffset OrderDate { get; set; }
    public string Status { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderItemViewModel> OrderItems { get; set; } = [];
}

public class OrderItemViewModel
{
    public Guid Id { get; set; }
    public string ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}