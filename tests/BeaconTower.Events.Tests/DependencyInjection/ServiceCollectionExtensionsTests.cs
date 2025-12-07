using BeaconTower.Events.Abstractions;
using BeaconTower.Events.DependencyInjection;
using BeaconTower.Events.InMemory;
using BeaconTower.Events.Nats;
using CloudNative.CloudEvents;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace BeaconTower.Events.Tests.DependencyInjection;

/// <summary>
/// Unit tests for ServiceCollectionExtensions.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddNatsCloudEventPublisher_Should_Register_Publisher_As_ICloudEventPublisher()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddNatsCloudEventPublisher(options =>
        {
            options.ConnectionString = "nats://localhost:4222";
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var publisher = provider.GetService<ICloudEventPublisher>();

        publisher.Should().NotBeNull();
        publisher.Should().BeOfType<NatsCloudEventPublisher>();
    }

    [Fact]
    public void AddNatsCloudEventSubscriber_Should_Register_Subscriber_As_ICloudEventSubscriber()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddNatsCloudEventSubscriber(options =>
        {
            options.ConnectionString = "nats://localhost:4222";
            options.ServiceName = "test-service";
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var subscriber = provider.GetService<ICloudEventSubscriber>();

        subscriber.Should().NotBeNull();
        subscriber.Should().BeOfType<NatsCloudEventSubscriber>();
    }

    [Fact]
    public void AddInMemoryCloudEvents_Should_Register_InMemory_Implementations()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInMemoryCloudEvents();

        // Assert
        var provider = services.BuildServiceProvider();

        var publisher = provider.GetService<ICloudEventPublisher>();
        publisher.Should().NotBeNull();
        publisher.Should().BeOfType<InMemoryCloudEventPublisher>();

        var subscriber = provider.GetService<ICloudEventSubscriber>();
        subscriber.Should().NotBeNull();
        subscriber.Should().BeOfType<InMemoryCloudEventSubscriber>();

        // Should also be able to get the concrete types directly
        var concretePublisher = provider.GetService<InMemoryCloudEventPublisher>();
        concretePublisher.Should().NotBeNull();
        concretePublisher.Should().BeSameAs(publisher);
    }

    [Fact]
    public void AddCloudEventHandler_Should_Register_Handler_With_Scoped_Lifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCloudEventHandler<TestEventData, TestEventHandler>();

        // Assert
        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(ICloudEventHandler<TestEventData>));

        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be<TestEventHandler>();
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddNatsCloudEventPublisher_Should_Bind_Configuration()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BeaconTower:Events:Nats:Publisher:ConnectionString"] = "nats://custom-host:4222",
                ["BeaconTower:Events:Nats:Publisher:StreamName"] = "custom-stream",
                ["BeaconTower:Events:Nats:Publisher:MaxRetryAttempts"] = "5"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddNatsCloudEventPublisher(
            configuration.GetSection(NatsCloudEventPublisherOptions.SectionName));

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<NatsCloudEventPublisherOptions>>().Value;

        options.ConnectionString.Should().Be("nats://custom-host:4222");
        options.StreamName.Should().Be("custom-stream");
        options.MaxRetryAttempts.Should().Be(5);
    }

    [Fact]
    public void AddNatsCloudEventSubscriber_Should_Bind_Configuration()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BeaconTower:Events:Nats:Subscriber:ConnectionString"] = "nats://custom-host:4222",
                ["BeaconTower:Events:Nats:Subscriber:ServiceName"] = "my-service",
                ["BeaconTower:Events:Nats:Subscriber:MaxConcurrency"] = "20",
                ["BeaconTower:Events:Nats:Subscriber:MaxDeliveryAttempts"] = "5"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddNatsCloudEventSubscriber(
            configuration.GetSection(NatsCloudEventSubscriberOptions.SectionName));

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<NatsCloudEventSubscriberOptions>>().Value;

        options.ConnectionString.Should().Be("nats://custom-host:4222");
        options.ServiceName.Should().Be("my-service");
        options.MaxConcurrency.Should().Be(20);
        options.MaxDeliveryAttempts.Should().Be(5);
    }

    [Fact]
    public void AddNatsCloudEventPublisher_Should_Throw_For_Missing_ConnectionString()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddNatsCloudEventPublisher(options =>
        {
            options.ConnectionString = null!;
        });

        var provider = services.BuildServiceProvider();

        // Act & Assert
        var action = () => provider.GetRequiredService<IOptions<NatsCloudEventPublisherOptions>>().Value;

        action.Should().Throw<OptionsValidationException>()
            .WithMessage("*ConnectionString*");
    }

    [Fact]
    public void AddNatsCloudEventSubscriber_Should_Throw_For_Missing_ServiceName()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddNatsCloudEventSubscriber(options =>
        {
            options.ConnectionString = "nats://localhost:4222";
            options.ServiceName = null!;
        });

        var provider = services.BuildServiceProvider();

        // Act & Assert
        var action = () => provider.GetRequiredService<IOptions<NatsCloudEventSubscriberOptions>>().Value;

        action.Should().Throw<OptionsValidationException>()
            .WithMessage("*ServiceName*");
    }

#pragma warning disable CA1812 // Internal class that is apparently never instantiated - used by DI
    private sealed class TestEventData
    {
        public string Name { get; init; } = string.Empty;
    }

    private sealed class TestEventHandler : ICloudEventHandler<TestEventData>
    {
        public Task HandleAsync(CloudEvent cloudEvent, TestEventData? data, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }
#pragma warning restore CA1812
}
