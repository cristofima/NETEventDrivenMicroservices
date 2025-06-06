using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Worker.EventHandlers;
using NotificationService.Worker.Interfaces;
using SharedKernel.Events;
using System.Text.Json;

namespace NotificationService.Tests.EventHandlers;

public class IntegrationEventHandlerFactoryTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly ILogger<IntegrationEventHandlerFactory> _logger;

    public IntegrationEventHandlerFactoryTests()
    {
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var serviceScopeMock = new Mock<IServiceScope>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _logger = new LoggerFactory().CreateLogger<IntegrationEventHandlerFactory>();

        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(serviceScopeMock.Object);
        serviceScopeMock.Setup(x => x.ServiceProvider).Returns(_serviceProviderMock.Object);
    }

    public static IEnumerable<object[]> IntegrationEventTestData =>
        new List<object[]>
        {
            new object[]
            {
                nameof(OrderCreatedIntegrationEvent),
                new OrderCreatedIntegrationEvent(Guid.NewGuid(), "123", [], 10m),
                typeof(OrderCreatedIntegrationEvent)
            },
            new object[]
            {
                nameof(OrderShippedIntegrationEvent),
                new OrderShippedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, "TRACK123"),
                typeof(OrderShippedIntegrationEvent)
            },
            new object[]
            {
                nameof(OrderProcessedIntegrationEvent),
                new OrderProcessedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow),
                typeof(OrderProcessedIntegrationEvent)
            },
            new object[]
            {
                nameof(OrderCompletedIntegrationEvent),
                new OrderCompletedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow),
                typeof(OrderCompletedIntegrationEvent)
            },
            new object[]
            {
                nameof(OrderCancelledIntegrationEvent),
                new OrderCancelledIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow),
                typeof(OrderCancelledIntegrationEvent)
            }
        };

    [Theory]
    [MemberData(nameof(IntegrationEventTestData))]
    public async Task TryHandleAsync_Should_Handle_IntegrationEvent(string eventTypeName, IntegrationEvent eventInstance, Type eventType)
    {
        // Arrange
        var handlerInterfaceType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);

        var handlerMock = (Mock)Activator.CreateInstance(typeof(Mock<>).MakeGenericType(handlerInterfaceType));
        handlerMock.As<IIntegrationEventHandler>()
            .Setup(h => h.HandleAsync(It.IsAny<IntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(sp => sp.GetService(handlerInterfaceType))
            .Returns(handlerMock.Object);

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

        var loggerMock = new Mock<ILogger<IntegrationEventHandlerFactory>>();

        var factory = new IntegrationEventHandlerFactory(scopeFactoryMock.Object, loggerMock.Object);

        var serializedEvent = JsonSerializer.Serialize(eventInstance);

        // Act
        var result = await factory.TryHandleAsync(eventTypeName, serializedEvent, CancellationToken.None);

        // Assert
        Assert.True(result);
        handlerMock.As<IIntegrationEventHandler>()
            .Verify(h => h.HandleAsync(It.IsAny<IntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TryHandleAsync_Should_ReturnFalse_For_UnknownEventType()
    {
        // Arrange
        var factory = new IntegrationEventHandlerFactory(_scopeFactoryMock.Object, _logger);

        var body = JsonSerializer.Serialize(new { Dummy = "value" });

        // Act
        var result = await factory.TryHandleAsync("UnknownEventType", body, CancellationToken.None);

        // Assert
        Assert.False(result);
    }
}