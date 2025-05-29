namespace SharedKernel.Events;

public class OrderShippedIntegrationEvent : IntegrationEvent
{
    public Guid OrderId { get; private set; }
    public DateTimeOffset ShippedDate { get; private set; }
    public string TrackingNumber { get; private set; } // Optional tracking number

    // For deserialization
    private OrderShippedIntegrationEvent()
    { }

    public OrderShippedIntegrationEvent(Guid orderId, DateTimeOffset shippedDate, string trackingNumber = null) : base()
    {
        OrderId = orderId;
        ShippedDate = shippedDate;
        TrackingNumber = trackingNumber;
    }
}