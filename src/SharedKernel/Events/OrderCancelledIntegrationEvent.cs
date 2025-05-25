namespace SharedKernel.Events;

public class OrderCancelledIntegrationEvent : IntegrationEvent
{
    public Guid OrderId { get; private set; }
    public DateTimeOffset CancelledDate { get; private set; }
    public string? Reason { get; private set; } // Optional reason for cancellation

    // For deserialization
    private OrderCancelledIntegrationEvent()
    { }

    public OrderCancelledIntegrationEvent(Guid orderId, DateTimeOffset cancelledDate, string? reason = null) : base()
    {
        OrderId = orderId;
        CancelledDate = cancelledDate;
        Reason = reason;
    }
}