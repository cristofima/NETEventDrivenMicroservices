using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Persistence;
using OrderService.Infrastructure.Persistence.Repositories;

namespace OrderService.IntegrationTests.Infrastructure.Base;

public abstract class DbContextTestBase : IDisposable
{
    protected readonly ServiceProvider ServiceProvider;
    protected readonly OrderDbContext DbContext;

    protected DbContextTestBase()
    {
        var services = new ServiceCollection();
        services.AddDbContext<OrderDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString())); // unique per test run
        services.AddScoped<IOrderRepository, OrderRepository>();

        ServiceProvider = services.BuildServiceProvider();
        DbContext = ServiceProvider.GetRequiredService<OrderDbContext>();

        DbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        DbContext.Database.EnsureDeleted();
        DbContext.Dispose();
        ServiceProvider.Dispose();
    }
}