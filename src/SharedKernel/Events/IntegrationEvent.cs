namespace SharedKernel.Events;

/// <summary>
/// Base class for integration events that are published across service boundaries.
/// </summary>
public abstract class IntegrationEvent
{
    public Guid Id { get; }
    public DateTimeOffset OccurredOn { get; }

    protected IntegrationEvent()
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTimeOffset.UtcNow;
    }
}