using SharedKernel.Events;

namespace OrderService.Application.Interfaces;

/// <summary>
/// Interface for publishing integration events to an external message bus.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default) where T : IntegrationEvent;
}