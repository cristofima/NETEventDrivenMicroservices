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
using OrderService.Domain.Services;

namespace OrderService.UnitTests.Application.Handlers;

public class CancelOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<ILogger<CancelOrderCommandHandler>> _logger = new();
    private readonly CancelOrderCommandHandler _handler;

    public CancelOrderCommandHandlerTests()
    {
        _handler = new CancelOrderCommandHandler(_orderRepository.Object, _mediator.Object, _logger.Object, new OrderStatusTransitionService());
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsFalse()
    {
        var command = new CancelOrderCommand(Guid.NewGuid(), "reason");
        _orderRepository.Setup(r => r.GetByIdAsync(command.OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var result = await _handler.Handle(command, default);

        result.Should().BeFalse();
        _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _mediator.Verify(m => m.Publish(It.IsAny<OrderCancelledDomainEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyCancelled_ReturnsFalse()
    {
        var order = new Order("cust", [new OrderItem("prod", 1, 1m)]);
        typeof(Order).GetProperty(nameof(Order.Status))!.SetValue(order, OrderStatus.Cancelled);
        var command = new CancelOrderCommand(order.Id, "reason");
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _handler.Handle(command, default);

        result.Should().BeFalse();
        _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _mediator.Verify(m => m.Publish(It.IsAny<OrderCancelledDomainEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Completed)]
    public async Task Handle_ShippedOrCompleted_ReturnsFalse(OrderStatus status)
    {
        var order = new Order("cust", [new OrderItem("prod", 1, 1m)]);
        typeof(Order).GetProperty(nameof(Order.Status))!.SetValue(order, status);
        var command = new CancelOrderCommand(order.Id, "reason");
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _handler.Handle(command, default);

        result.Should().BeFalse();
        _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _mediator.Verify(m => m.Publish(It.IsAny<OrderCancelledDomainEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Processing)]
    public async Task Handle_PendingOrProcessing_UpdatesAndPublishes(OrderStatus status)
    {
        var order = new Order("cust", [new OrderItem("prod", 1, 1m)]);
        typeof(Order).GetProperty(nameof(Order.Status))!.SetValue(order, status);
        var command = new CancelOrderCommand(order.Id, "reason");
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _handler.Handle(command, default);

        result.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancellationReason.Should().Be("reason");
        order.CancelledAt.Should().NotBeNull();
        _orderRepository.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _mediator.Verify(m => m.Publish(It.Is<OrderCancelledDomainEvent>(e => e.Order == order && e.Reason == "reason"), It.IsAny<CancellationToken>()), Times.Once);
    }
}