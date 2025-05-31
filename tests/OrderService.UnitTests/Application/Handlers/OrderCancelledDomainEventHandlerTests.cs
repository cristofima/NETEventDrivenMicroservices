using Microsoft.Extensions.Logging;
using Moq;
using OrderService.Application.Events;
using OrderService.Application.Handlers;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using SharedKernel.Events;

namespace OrderService.UnitTests.Application.Handlers;

public class OrderCancelledDomainEventHandlerTests
{
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly Mock<ILogger<OrderCancelledDomainEventHandler>> _logger = new();
    private readonly OrderCancelledDomainEventHandler _handler;

    public OrderCancelledDomainEventHandlerTests()
    {
        _handler = new OrderCancelledDomainEventHandler(_eventPublisher.Object, _logger.Object);
    }

    [Fact]
    public async Task Handle_PublishesIntegrationEvent()
    {
        var order = new Order("cust", [new OrderItem("prod", 1, 1m)]);
        var evt = new OrderCancelledDomainEvent(order, DateTimeOffset.UtcNow, "reason");

        await _handler.Handle(evt, default);

        _eventPublisher.Verify(p => p.PublishAsync(
            It.Is<OrderCancelledIntegrationEvent>(e =>
                e.OrderId == order.Id &&
                e.CancelledDate == evt.CancelledDate &&
                e.Reason == "reason"
            ),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PublishThrows_LogsErrorAndThrows()
    {
        var order = new Order("cust", [new OrderItem("prod", 1, 1m)]);
        var evt = new OrderCancelledDomainEvent(order, DateTimeOffset.UtcNow, "reason");
        _eventPublisher.Setup(p => p.PublishAsync(It.IsAny<OrderCancelledIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("fail"));

        await Assert.ThrowsAsync<Exception>(() => _handler.Handle(evt, default));
        _eventPublisher.Verify(p => p.PublishAsync(It.IsAny<OrderCancelledIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error publishing OrderCancelledIntegrationEvent")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }
}