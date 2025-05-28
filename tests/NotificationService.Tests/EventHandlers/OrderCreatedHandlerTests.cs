using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Worker.EventHandlers;
using SharedKernel.Events;

namespace NotificationService.Tests.EventHandlers;

public class OrderCreatedHandlerTests
{
    [Fact]
    public async Task HandleAsync_LogsInformation()
    {
        var loggerMock = new Mock<ILogger<OrderCreatedHandler>>();
        var handler = new OrderCreatedHandler(loggerMock.Object);
        var evt = new OrderCreatedIntegrationEvent
        (
            Guid.NewGuid(),
            string.Empty,
            [],
            100
        );

        await handler.HandleAsync(evt, CancellationToken.None);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("OrderCreated")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}