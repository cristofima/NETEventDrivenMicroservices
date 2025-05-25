namespace SharedKernel.Events;

public class OrderCompletedIntegrationEvent : IntegrationEvent
{
    public Guid OrderId { get; private set; }
    public DateTimeOffset CompletedDate { get; private set; }

    // For deserialization
    private OrderCompletedIntegrationEvent()
    { }

    public OrderCompletedIntegrationEvent(Guid orderId, DateTimeOffset completedDate) : base()
    {
        OrderId = orderId;
        CompletedDate = completedDate;
    }
}