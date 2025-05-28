using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Worker.EventHandlers;
using SharedKernel.Events;

namespace NotificationService.Tests.EventHandlers;

public class OrderProcessedHandlerTests
{
    [Fact]
    public async Task HandleAsync_LogsInformation()
    {
        var loggerMock = new Mock<ILogger<OrderProcessedHandler>>();
        var handler = new OrderProcessedHandler(loggerMock.Object);
        var evt = new OrderProcessedIntegrationEvent
        (
            Guid.NewGuid(),
            DateTime.UtcNow
        );

        await handler.HandleAsync(evt, CancellationToken.None);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("OrderProcessed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}