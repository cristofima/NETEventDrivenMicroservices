using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.Services;

namespace OrderService.UnitTests.Domain;

public class OrderStatusTransitionServiceTests
{
    private readonly OrderStatusTransitionService _transitionService = new();

    private static Order CreateOrderWithStatus(OrderStatus status)
    {
        var order = new Order("customer-123", [new OrderItem("product-1", 1, 10.0m)]);

        // Use reflection to simulate setting internal status
        typeof(Order).GetProperty(nameof(Order.Status))!
            .SetValue(order, status);

        return order;
    }

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Processing)]
    [InlineData(OrderStatus.Processing, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Completed)]
    public void ChangeStatus_ValidTransition_Succeeds(OrderStatus from, OrderStatus to)
    {
        var order = CreateOrderWithStatus(from);
        var now = DateTimeOffset.UtcNow;

        _transitionService.ChangeStatus(order, to, now);

        Assert.Equal(to, order.Status);
    }

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Processing, OrderStatus.Completed)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Processing)]
    public void ChangeStatus_InvalidTransition_Throws(OrderStatus from, OrderStatus to)
    {
        var order = CreateOrderWithStatus(from);
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<InvalidOperationException>(() =>
            _transitionService.ChangeStatus(order, to, now));
    }

    [Fact]
    public void CancelOrder_ValidTransition_Succeeds()
    {
        var order = CreateOrderWithStatus(OrderStatus.Pending);
        var now = DateTimeOffset.UtcNow;
        var reason = "Customer changed mind";

        _transitionService.ChangeStatus(order, OrderStatus.Cancelled, now, reason);

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(reason, typeof(Order).GetProperty("CancellationReason", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)!
            .GetValue(order));
    }

    [Fact]
    public void CancelOrder_WhenCompleted_Throws()
    {
        var order = CreateOrderWithStatus(OrderStatus.Completed);
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<InvalidOperationException>(() =>
            _transitionService.ChangeStatus(order, OrderStatus.Cancelled, now, "Too late"));
    }
}