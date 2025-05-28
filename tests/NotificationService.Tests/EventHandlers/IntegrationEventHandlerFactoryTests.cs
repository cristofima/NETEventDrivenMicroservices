using Microsoft.Extensions.DependencyInjection;
using Moq;
using NotificationService.Worker.EventHandlers;
using NotificationService.Worker.Interfaces;
using SharedKernel.Events;
using System.Text.Json;

namespace NotificationService.Tests.EventHandlers;

public class IntegrationEventHandlerFactoryTests
{
    private readonly Mock<IIntegrationEventHandler<OrderCreatedIntegrationEvent>> _createdHandlerMock = new();
    private readonly ServiceProvider _provider;

    public IntegrationEventHandlerFactoryTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_createdHandlerMock.Object);
        services.AddSingleton(typeof(IIntegrationEventHandler<OrderCreatedIntegrationEvent>),
            _createdHandlerMock.Object);
        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task TryHandleAsync_ReturnsFalse_IfNoHandler()
    {
        var factory = new IntegrationEventHandlerFactory(_provider);
        var result = await factory.TryHandleAsync("UnknownEvent", "{}", CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task TryHandleAsync_CallsHandler_IfHandlerExists()
    {
        var factory = new IntegrationEventHandlerFactory(_provider);
        var evt = new OrderCreatedIntegrationEvent(Guid.NewGuid(), string.Empty, [], 123);
        var json = JsonSerializer.Serialize(evt);

        _createdHandlerMock.Setup(h =>
                h.HandleAsync(It.IsAny<OrderCreatedIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var result = await factory.TryHandleAsync(nameof(OrderCreatedIntegrationEvent), json, CancellationToken.None);

        Assert.True(result);
        _createdHandlerMock.Verify(
            h => h.HandleAsync(It.IsAny<OrderCreatedIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}