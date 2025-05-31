using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Worker.Interfaces;
using NotificationService.Worker.Services;

namespace NotificationService.Tests.Services;

public class OrderBackgroundServiceTests
{
    private readonly Mock<ILogger<OrderBackgroundService>> _loggerMock = new();
    private readonly Mock<IIntegrationEventHandlerFactory> _handlerFactoryMock = new();

    private IConfiguration BuildConfig(
        string connectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=TESTKEY",
        string topic = "topic",
        string subscription = "sub")
    {
        var dict = new Dictionary<string, string?>
        {
            ["AzureServiceBus:ConnectionString"] = connectionString,
            ["AzureServiceBus:TopicName"] = topic,
            ["AzureServiceBus:SubscriptionName"] = subscription
        };
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    [Fact]
    public void Constructor_ThrowsIfLoggerNull()
    {
        var config = BuildConfig();
        Assert.Throws<ArgumentNullException>(() =>
            new OrderBackgroundService(config, null!, _handlerFactoryMock.Object));
    }

    [Fact]
    public void Constructor_ThrowsIfHandlerFactoryNull()
    {
        var config = BuildConfig();
        Assert.Throws<ArgumentNullException>(() =>
            new OrderBackgroundService(config, _loggerMock.Object, null!));
    }

    [Theory]
    [InlineData(null, "topic", "sub")]
    [InlineData("conn", null, "sub")]
    [InlineData("conn", "topic", null)]
    [InlineData("", "topic", "sub")]
    [InlineData("conn", "", "sub")]
    [InlineData("conn", "topic", "")]
    public void Constructor_ThrowsIfConfigMissing(string conn, string topic, string sub)
    {
        var config = BuildConfig(conn, topic, sub);
        Assert.Throws<InvalidOperationException>(() =>
            new OrderBackgroundService(config, _loggerMock.Object, _handlerFactoryMock.Object));
    }

    [Fact]
    public async Task StopAsync_DisposesProcessorAndClient_AndLogs()
    {
        // Arrange
        var config = BuildConfig();
        var logger = new Mock<ILogger<OrderBackgroundService>>();
        var handlerFactory = new Mock<IIntegrationEventHandlerFactory>();

        var service = new OrderBackgroundService(config, logger.Object, handlerFactory.Object);

        // Use reflection to get private fields for processor and client
        var processorField = typeof(OrderBackgroundService).GetField("_processor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var clientField = typeof(OrderBackgroundService).GetField("_serviceBusClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var processorMock = new Mock<ServiceBusProcessor>();
        var clientMock = new Mock<ServiceBusClient>();

        processorField!.SetValue(service, processorMock.Object);
        clientField!.SetValue(service, clientMock.Object);

        // Act
        await service.StopAsync(default);

        // Assert
        logger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("OrderEventsHandler is stopping")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        logger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("OrderEventsHandler shutdown complete")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    [Fact]
    public async Task ErrorHandlerAsync_LogsError()
    {
        // Arrange
        var config = BuildConfig();
        var logger = new Mock<ILogger<OrderBackgroundService>>();
        var handlerFactory = new Mock<IIntegrationEventHandlerFactory>();
        var service = new OrderBackgroundService(config, logger.Object, handlerFactory.Object);

        var errorArgs = new ProcessErrorEventArgs(
            new Exception("Test error"),
            ServiceBusErrorSource.Receive,
            "entityPath",
            "namespace",
            CancellationToken.None);

        // Use reflection to invoke private ErrorHandlerAsync
        var method = typeof(OrderBackgroundService).GetMethod("ErrorHandlerAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var task = (Task)method.Invoke(service, new object[] { errorArgs })!;
        await task;

        logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Service Bus error")),
            errorArgs.Exception,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    [Fact]
    public async Task HandleEventCoreAsync_HandlesSuccessfully_Logs()
    {
        // Arrange
        var config = BuildConfig();
        var logger = new Mock<ILogger<OrderBackgroundService>>();
        var handlerFactory = new Mock<IIntegrationEventHandlerFactory>();
        handlerFactory.Setup(f => f.TryHandleAsync("TestEvent", "{}", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = new OrderBackgroundService(config, logger.Object, handlerFactory.Object);

        // Act
        await service.HandleEventCoreAsync("TestEvent", "{}", 123, CancellationToken.None);

        // Assert
        handlerFactory.Verify(f => f.TryHandleAsync("TestEvent", "{}", It.IsAny<CancellationToken>()), Times.Once);
        logger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("completed successfully")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }
}