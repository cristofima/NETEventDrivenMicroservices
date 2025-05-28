namespace NotificationService.Worker.Interfaces;

public interface IIntegrationEventHandlerFactory
{
    Task<bool> TryHandleAsync(string eventType, string body, CancellationToken cancellationToken);
}