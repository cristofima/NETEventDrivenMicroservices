namespace SharedKernel.Events;

public class OrderProcessedIntegrationEvent : IntegrationEvent
{
    public Guid OrderId { get; private set; }
    public DateTimeOffset ProcessedDate { get; private set; }

    // For deserialization
    private OrderProcessedIntegrationEvent()
    { }

    public OrderProcessedIntegrationEvent(Guid orderId, DateTimeOffset processedDate) : base()
    {
        OrderId = orderId;
        ProcessedDate = processedDate;
    }
}