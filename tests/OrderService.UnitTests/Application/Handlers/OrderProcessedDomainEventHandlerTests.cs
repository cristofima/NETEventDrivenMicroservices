using Microsoft.Extensions.Logging;
using Moq;
using OrderService.Application.Commands;
using OrderService.Application.Handlers;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entities;
using SharedKernel.Events;

namespace OrderService.UnitTests.Application.Handlers;

public class OrderProcessedDomainEventHandlerTests
{
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly Mock<ILogger<OrderProcessedDomainEventHandler>> _logger = new();
    private readonly OrderProcessedDomainEventHandler _handler;

    public OrderProcessedDomainEventHandlerTests()
    {
        _handler = new OrderProcessedDomainEventHandler(_eventPublisher.Object, _logger.Object);
    }

    [Fact]
    public async Task Handle_PublishesIntegrationEvent()
    {
        var order = new Order("cust", [new OrderItem("prod", 1, 1m)]);
        var evt = new OrderProcessedDomainEvent(order, DateTimeOffset.UtcNow);

        await _handler.Handle(evt, default);

        _eventPublisher.Verify(p => p.PublishAsync(
            It.Is<OrderProcessedIntegrationEvent>(e =>
                e.OrderId == order.Id &&
                e.ProcessedDate == evt.ProcessedDate
            ),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PublishThrows_LogsErrorAndThrows()
    {
        var order = new Order("cust", [new OrderItem("prod", 1, 1m)]);
        var evt = new OrderProcessedDomainEvent(order, DateTimeOffset.UtcNow);
        _eventPublisher.Setup(p => p.PublishAsync(It.IsAny<OrderProcessedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("fail"));

        await Assert.ThrowsAsync<Exception>(() => _handler.Handle(evt, default));
        _eventPublisher.Verify(p => p.PublishAsync(It.IsAny<OrderProcessedIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error publishing OrderProcessedIntegrationEvent")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }
}