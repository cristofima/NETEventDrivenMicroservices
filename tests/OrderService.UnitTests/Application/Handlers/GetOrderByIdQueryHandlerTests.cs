using FluentAssertions;
using Moq;
using OrderService.Application.Handlers;
using OrderService.Application.Queries;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;

namespace OrderService.UnitTests.Application.Handlers;

public class GetOrderByIdQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly GetOrderByIdQueryHandler _handler;

    public GetOrderByIdQueryHandlerTests()
    {
        _handler = new GetOrderByIdQueryHandler(_orderRepository.Object);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsNull()
    {
        var query = new GetOrderByIdQuery(Guid.NewGuid());
        _orderRepository.Setup(r => r.GetByIdAsync(query.OrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var result = await _handler.Handle(query, default);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_OrderFound_ReturnsOrderViewModel()
    {
        var order = new Order("cust", [new OrderItem("prod", 2, 5m)]);
        typeof(Order).GetProperty(nameof(Order.Id))!.SetValue(order, Guid.NewGuid());
        var query = new GetOrderByIdQuery(order.Id);
        _orderRepository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _handler.Handle(query, default);

        result.Should().NotBeNull();
        result!.Id.Should().Be(order.Id);
        result.CustomerId.Should().Be(order.CustomerId);
        result.Status.Should().Be(order.Status.ToString());
        result.TotalAmount.Should().Be(order.TotalAmount);
        result.OrderItems.Should().HaveCount(1);
        result.OrderItems[0].ProductId.Should().Be("prod");
        result.OrderItems[0].Quantity.Should().Be(2);
        result.OrderItems[0].UnitPrice.Should().Be(5m);
    }
}