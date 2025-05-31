using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Worker.EventHandlers;
using NotificationService.Worker.Interfaces;
using SharedKernel.Events;

namespace NotificationService.Tests.EventHandlers;

public class IntegrationEventHandlerFactoryTests
{
    private readonly Mock<IIntegrationEventHandler<OrderCreatedIntegrationEvent>> _createdHandlerMock = new();
    private readonly Mock<ILogger<IntegrationEventHandlerFactory>> _mockLogger = new();
    private readonly IServiceScopeFactory _scopeFactory;

    public IntegrationEventHandlerFactoryTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_createdHandlerMock.Object);
        services.AddSingleton(typeof(IIntegrationEventHandler<OrderCreatedIntegrationEvent>),
            _createdHandlerMock.Object);
        var provider = services.BuildServiceProvider();
        _scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    public async Task TryHandleAsync_ReturnsFalse_IfNoHandler()
    {
        var factory = new IntegrationEventHandlerFactory(_scopeFactory, _mockLogger.Object);
        var result = await factory.TryHandleAsync("UnknownEvent", "{}", CancellationToken.None);
        Assert.False(result);
    }
}