using SharedKernel.Events;

namespace NotificationService.Worker.Interfaces;

/// <summary>
/// Non-generic interface for uniform event handling
/// </summary>
public interface IIntegrationEventHandler
{
    Task HandleAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken);
}

/// <summary>
/// Type-safe handler for a specific event type
/// </summary>
public interface IIntegrationEventHandler<in TEvent> : IIntegrationEventHandler
    where TEvent : IntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
}