using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics.CodeAnalysis;

namespace OrderService.Api;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddWebApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddOpenApi();
        services.AddApplicationInsightsTelemetry(configuration);

        services.AddHealthChecks()
            .AddSqlServer(
                connectionString: configuration.GetConnectionString("OrderServiceDb")!,
                name: "OrderService-DB-Check",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["db", "sql", "sqlserver"])
            .AddAzureServiceBusTopic(
                connectionString: configuration.GetValue<string>("AzureServiceBus:ConnectionString")!,
                topicName: configuration.GetValue<string>("AzureServiceBus:OrderCreatedTopicName")!,
                name: "OrderService-ServiceBus-Check",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["servicebus", "messaging"]);

        return services;
    }
}