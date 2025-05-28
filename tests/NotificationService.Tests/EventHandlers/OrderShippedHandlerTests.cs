using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Worker.EventHandlers;
using SharedKernel.Events;

namespace NotificationService.Tests.EventHandlers;

public class OrderShippedHandlerTests
{
    [Fact]
    public async Task HandleAsync_LogsInformation()
    {
        var loggerMock = new Mock<ILogger<OrderShippedHandler>>();
        var handler = new OrderShippedHandler(loggerMock.Object);
        var evt = new OrderShippedIntegrationEvent
        (
            Guid.NewGuid(),
            DateTime.UtcNow,
            "TRACK123"
        );

        await handler.HandleAsync(evt, CancellationToken.None);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("OrderShipped")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}