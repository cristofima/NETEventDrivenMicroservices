using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using OrderService.Application.Commands;
using OrderService.Application.Events;
using OrderService.Application.Handlers;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;

namespace OrderService.UnitTests.Application.Handlers;

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<IMediator> _mockMediator;
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockMediator = new Mock<IMediator>();
        var mockLogger = new Mock<ILogger<CreateOrderCommandHandler>>();
        _handler = new CreateOrderCommandHandler(
            _mockOrderRepository.Object,
            _mockMediator.Object,
            mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateOrderAndPublishDomainEvent()
    {
        // Arrange
        var command = new CreateOrderCommand("customer1",
        [
            new OrderItemDto("product1", 1, 10.0m)
        ]);

        Order? capturedOrder = null;
        _mockOrderRepository
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, ct) => capturedOrder = order)
            .Returns(Task.CompletedTask);

        // Act
        var resultOrderId = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultOrderId.Should().NotBeEmpty();
        _mockOrderRepository.Verify(r => r.AddAsync(It.Is<Order>(o =>
            o.CustomerId == command.CustomerId &&
            o.OrderItems.Count == command.ProductItems.Count &&
            o.OrderItems.First().ProductId == command.ProductItems.First().ProductId
        ), It.IsAny<CancellationToken>()), Times.Once);

        // Verify that the domain event was published via MediatR
        _mockMediator.Verify(m => m.Publish(It.Is<OrderCreatedDomainEvent>(domainEvent =>
            domainEvent.Order.Id == resultOrderId &&
            domainEvent.Order == capturedOrder // Ensure the event contains the captured order
        ), It.IsAny<CancellationToken>()), Times.Once);

        // Check captured order details
        capturedOrder.Should().NotBeNull();
        capturedOrder.Id.Should().Be(resultOrderId);
        capturedOrder.CustomerId.Should().Be("customer1");
        capturedOrder.OrderItems.Should().HaveCount(1);
        capturedOrder.OrderItems.First().ProductId.Should().Be("product1");
        capturedOrder.OrderItems.First().Quantity.Should().Be(1);
        capturedOrder.OrderItems.First().UnitPrice.Should().Be(10.0m);
        capturedOrder.Status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public async Task Handle_CommandWithNoItems_ShouldThrowArgumentException_InDomainEntity()
    {
        // Arrange: The command itself allows empty items, but the Order entity constructor should throw.
        var command = new CreateOrderCommand("customer1", []);

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // The Order entity constructor will throw ArgumentException if items list is empty.
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Order must have at least one item.*"); // Check specific message

        _mockOrderRepository.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockMediator.Verify(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}