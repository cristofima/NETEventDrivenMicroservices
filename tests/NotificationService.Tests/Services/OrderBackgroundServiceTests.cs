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

    private IConfiguration BuildConfig(string connectionString = "Endpoint=sb://test/", string topic = "topic", string subscription = "sub")
    {
        var dict = new System.Collections.Generic.Dictionary<string, string?>
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
}