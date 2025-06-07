using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OrderService.Api;
using OrderService.Application;
using OrderService.Infrastructure;
using OrderService.Infrastructure.Persistence;
using Scalar.AspNetCore;
using System.Diagnostics.CodeAnalysis;

var builder = WebApplication.CreateBuilder(args);

// Register services from other layers using extension methods
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddWebApiServices(builder.Configuration);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
if (!string.IsNullOrEmpty(builder.Configuration["ApplicationInsights:ConnectionString"]))
{
    builder.Logging.AddApplicationInsights();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
        .WithTitle("Order Service API");
    });

    try
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        dbContext.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while creating the database.");
        // Optionally, rethrow or handle as critical failure
    }
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Map Health Checks endpoint
app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    Predicate = _ => true, // Include all health checks
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse // Nicer JSON output
});

app.MapHealthChecks("/healthz-liveness", new HealthCheckOptions
{
    Predicate = r => r.Name.Contains("self") // Basic liveness check
});

app.Run();

[ExcludeFromCodeCoverage]
public partial class Program
{ }