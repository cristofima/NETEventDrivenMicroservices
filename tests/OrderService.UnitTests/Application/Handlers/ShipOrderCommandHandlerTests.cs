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

public class ShipOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<ILogger<ShipOrderCommandHandler>> _logger = new();
    private readonly ShipOrderCommandHandler _handler;

    public ShipOrderCommandHandlerTests()
    {
        _handler = new ShipOrderCommandHandler(_orderRepository.Object, _mediator.Object, _logger.Object, new OrderStatusTransitionService());
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsFalse()
    {
        var command = new ShipOrderCommand(Guid.NewGuid(), "TRACK123");
        _orderRepository.Setup(r => r.GetByIdAsync(command.OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var result = await _handler.Handle(command, default);

        result.Should().BeFalse();
        _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _mediator.Verify(m => m.Publish(It.IsAny<OrderShippedDomainEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OrderNotProcessing_ReturnsFalse()
    {
        var order = new Order("cust", [new OrderItem("prod", 1, 1m)]);
        typeof(Order).GetProperty(nameof(Order.Status))!.SetValue(order, OrderStatus.Pending);
        var command = new ShipOrderCommand(order.Id, "TRACK123");
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _handler.Handle(command, default);

        result.Should().BeFalse();
        _orderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _mediator.Verify(m => m.Publish(It.IsAny<OrderShippedDomainEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OrderProcessing_UpdatesAndPublishes()
    {
        var order = new Order("cust", [new OrderItem("prod", 1, 1m)]);
        typeof(Order).GetProperty(nameof(Order.Status))!.SetValue(order, OrderStatus.Processing);
        var command = new ShipOrderCommand(order.Id, "TRACK123");
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _handler.Handle(command, default);

        result.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Shipped);
        order.TrackingNumber.Should().Be("TRACK123");
        order.ShippedAt.Should().NotBeNull();
        _orderRepository.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _mediator.Verify(m => m.Publish(It.Is<OrderShippedDomainEvent>(e => e.Order == order && e.TrackingNumber == "TRACK123"), It.IsAny<CancellationToken>()), Times.Once);
    }
}