using Microsoft.Extensions.DependencyInjection;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.Interfaces;
using OrderService.Domain.Services;
using OrderService.IntegrationTests.Infrastructure.Base;

namespace OrderService.IntegrationTests.Infrastructure.Repositories;

public class OrderRepositoryTests : DbContextTestBase
{
    private readonly IOrderRepository _repository;

    public OrderRepositoryTests()
    {
        _repository = ServiceProvider.GetRequiredService<IOrderRepository>();
    }

    private static Order CreateTestOrder()
    {
        return new Order
        (
            Guid.NewGuid().ToString(),
            [new OrderItem(Guid.NewGuid().ToString(), 2, 10)]
        );
    }

    [Fact]
    public async Task AddAsync_ShouldPersistOrder()
    {
        var order = CreateTestOrder();

        await _repository.AddAsync(order);

        var fetched = await _repository.GetByIdAsync(order.Id);

        Assert.NotNull(fetched);
        Assert.Equal(order.Id, fetched.Id);
        Assert.Equal(order.CustomerId, fetched.CustomerId);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Single(fetched.OrderItems);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyOrder()
    {
        var order = CreateTestOrder();
        await _repository.AddAsync(order);

        var fetched = await _repository.GetByIdAsync(order.Id);
        var transitionService = new OrderStatusTransitionService();
        transitionService.ChangeStatus(fetched, OrderStatus.Processing, DateTimeOffset.UtcNow);
        await _repository.UpdateAsync(fetched);

        var updated = await _repository.GetByIdAsync(order.Id);
        Assert.Equal(OrderStatus.Processing, updated.Status);
    }
}