using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Worker.EventHandlers;
using SharedKernel.Events;

namespace NotificationService.Tests.EventHandlers;

public class OrderCancelledHandlerTests
{
    [Fact]
    public async Task HandleAsync_LogsInformation()
    {
        var loggerMock = new Mock<ILogger<OrderCancelledHandler>>();
        var handler = new OrderCancelledHandler(loggerMock.Object);
        var evt = new OrderCancelledIntegrationEvent
        (
            Guid.NewGuid(),
            DateTime.UtcNow,
            "Customer request"
        );

        await handler.HandleAsync(evt, CancellationToken.None);

        loggerMock.Verify(
            static x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>(static (v, t) => v.ToString().Contains("OrderCancelled")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}