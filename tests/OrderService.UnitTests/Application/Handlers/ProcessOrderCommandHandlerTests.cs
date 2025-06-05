using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using OrderService.Application.Commands;
using OrderService.Application.Handlers;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;
using OrderService.Domain.Services;

namespace OrderService.UnitTests.Application.Handlers;

public class ProcessOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<IMediator> _mockMediator;
    private readonly ProcessOrderCommandHandler _handler;

    public ProcessOrderCommandHandlerTests()
    {
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockMediator = new Mock<IMediator>();
        var mockLogger = new Mock<ILogger<ProcessOrderCommandHandler>>();
        _handler = new ProcessOrderCommandHandler(
            _mockOrderRepository.Object,
            _mockMediator.Object,
            mockLogger.Object,
            new OrderStatusTransitionService());
    }

    [Fact]
    public async Task Handle_ExistingOrder_ShouldUpdateStatusToProcessingAndPublishEvent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var command = new ProcessOrderCommand(orderId);
        var existingOrder = new Order("cust123", [new OrderItem("prod1", 1, 10m)]);
        // Manually set Id for testing, though constructor usually does this.
        typeof(Order).GetProperty(nameof(Order.Id))?.SetValue(existingOrder, orderId);

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);
        _mockOrderRepository.Setup(r => r.UpdateAsync(existingOrder, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        existingOrder.Status.Should().Be(OrderStatus.Processing);
        _mockOrderRepository.Verify(r => r.UpdateAsync(existingOrder, It.IsAny<CancellationToken>()), Times.Once);
        _mockMediator.Verify(m => m.Publish(It.Is<OrderProcessedDomainEvent>(e =>
            e.Order.Id == orderId && e.Order.Status == OrderStatus.Processing
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistingOrder_ShouldReturnFalseAndNotPublishEvent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var command = new ProcessOrderCommand(orderId);

        _mockOrderRepository.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockOrderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockMediator.Verify(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}