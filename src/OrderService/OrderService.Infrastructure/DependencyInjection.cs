using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Interfaces;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Messaging;
using OrderService.Infrastructure.Persistence;
using OrderService.Infrastructure.Persistence.Repositories;

namespace OrderService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure EF Core DbContext
        var connectionString = configuration.GetConnectionString("OrderServiceDb");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string 'OrderServiceDb' not found in configuration.");
        }
        services.AddDbContext<OrderDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                // Configure for resiliency
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            }));

        // Register Repositories
        services.AddScoped<IOrderRepository, OrderRepository>();

        // Register Event Publisher
        services.AddSingleton<IEventPublisher, ServiceBusEventPublisher>();

        return services;
    }
}