using NotificationService.Worker.EventHandlers;
using NotificationService.Worker.Interfaces;
using NotificationService.Worker.Services;
using SharedKernel.Events;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // Configure Application Insights for Worker Service
        // ConnectionString is typically set in appsettings.json or environment variables.
        services.AddApplicationInsightsTelemetryWorkerService(options =>
        {
            options.ConnectionString = hostContext.Configuration["ApplicationInsights:ConnectionString"];
        });
        
        services.AddSingleton<IIntegrationEventHandlerFactory, IntegrationEventHandlerFactory>();

        services.AddScoped<IIntegrationEventHandler<OrderCreatedIntegrationEvent>, OrderCreatedHandler>();
        services.AddScoped<IIntegrationEventHandler<OrderProcessedIntegrationEvent>, OrderProcessedHandler>();
        services.AddScoped<IIntegrationEventHandler<OrderShippedIntegrationEvent>, OrderShippedHandler>();
        services.AddScoped<IIntegrationEventHandler<OrderCompletedIntegrationEvent>, OrderCompletedHandler>();
        services.AddScoped<IIntegrationEventHandler<OrderCancelledIntegrationEvent>, OrderCancelledHandler>();
        
        // Register the background service that handles events
        services.AddHostedService<OrderBackgroundService>();

        // Configure logging further if needed
        services.AddLogging(configure =>
        {
            configure.AddConsole();
            configure.AddDebug();
            // Application Insights logging is added by AddApplicationInsightsTelemetryWorkerService
        });
    })
    .Build();

await host.RunAsync();