namespace SharedKernel.Events;

public class OrderCreatedIntegrationEvent : IntegrationEvent
{
    public Guid OrderId { get; private set; }
    public string CustomerId { get; private set; }
    public List<OrderItemData> ProductItems { get; private set; }
    public decimal TotalAmount { get; private set; }

    // Private constructor for deserialization
    private OrderCreatedIntegrationEvent()
    { }

    public OrderCreatedIntegrationEvent(Guid orderId, string customerId, List<OrderItemData> productItems, decimal totalAmount)
        : base()
    {
        OrderId = orderId;
        CustomerId = customerId;
        ProductItems = productItems;
        TotalAmount = totalAmount;
    }
}

public class OrderItemData
{
    public string ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    // Parameterless constructor for deserialization
    public OrderItemData()
    { }

    public OrderItemData(string productId, int quantity, decimal unitPrice)
    {
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}