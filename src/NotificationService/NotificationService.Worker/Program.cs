using NotificationService.Worker.EventHandlers;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // Configure Application Insights for Worker Service
        // ConnectionString is typically set in appsettings.json or environment variables.
        services.AddApplicationInsightsTelemetryWorkerService(options =>
        {
            options.ConnectionString = hostContext.Configuration["ApplicationInsights:ConnectionString"];
        });

        // Register the background service that handles events
        services.AddHostedService<OrderEventsHandler>();

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