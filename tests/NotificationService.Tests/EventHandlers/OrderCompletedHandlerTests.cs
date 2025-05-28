using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Worker.EventHandlers;
using SharedKernel.Events;

namespace NotificationService.Tests.EventHandlers;

public class OrderCompletedHandlerTests
{
    [Fact]
    public async Task HandleAsync_LogsInformation()
    {
        var loggerMock = new Mock<ILogger<OrderCompletedHandler>>();
        var handler = new OrderCompletedHandler(loggerMock.Object);
        var evt = new OrderCompletedIntegrationEvent
        (
            Guid.NewGuid(),
            DateTime.UtcNow
        );

        await handler.HandleAsync(evt, CancellationToken.None);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("OrderCompleted")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}