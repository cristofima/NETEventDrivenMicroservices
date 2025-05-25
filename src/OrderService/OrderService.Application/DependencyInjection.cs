using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace OrderService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register MediatR - it will scan the assembly for handlers
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // If you have other application-specific services that are not MediatR handlers,
        // register them here. For example, mappers, validators, etc.
        // services.AddTransient<IOrderValidator, OrderValidator>();

        return services;
    }
}