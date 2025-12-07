using BeaconTower.Events.Abstractions;
using BeaconTower.Events.InMemory;
using CloudNative.CloudEvents;
using FluentAssertions;
using Xunit;

namespace BeaconTower.Events.Tests.InMemory;

public class InMemoryCloudEventSubscriberTests
{
    [Fact]
    public async Task DeliverAsync_Should_Invoke_Handler_With_Matching_CloudEvent()
    {
        // Arrange
        var subscriber = new InMemoryCloudEventSubscriber();
        CloudEvent? receivedEvent = null;
        TestData? receivedData = null;

        await subscriber.SubscribeAsync<TestData>(
            "cloud.beacontower.device.created",
            (cloudEvent, data, ct) =>
            {
                receivedEvent = cloudEvent;
                receivedData = data;
                return Task.CompletedTask;
            });

        var cloudEvent = CreateTestCloudEvent(
            "cloud.beacontower.device.created",
            new TestData { Name = "Device1", Value = 42 });

        // Act
        await subscriber.DeliverAsync(cloudEvent);

        // Assert
        receivedEvent.Should().BeSameAs(cloudEvent);
        receivedData.Should().NotBeNull();
        receivedData!.Name.Should().Be("Device1");
        receivedData.Value.Should().Be(42);
    }

    [Fact]
    public async Task DeliverAsync_Should_Not_Invoke_Handler_For_NonMatching_CloudEvent()
    {
        // Arrange
        var subscriber = new InMemoryCloudEventSubscriber();
        var handlerCalled = false;

        await subscriber.SubscribeAsync<TestData>(
            "cloud.beacontower.device.created",
            (_, _, _) =>
            {
                handlerCalled = true;
                return Task.CompletedTask;
            });

        var cloudEvent = CreateTestCloudEvent(
            "cloud.beacontower.device.deleted",
            new TestData { Name = "Device1" });

        // Act
        await subscriber.DeliverAsync(cloudEvent);

        // Assert
        handlerCalled.Should().BeFalse();
    }

    [Fact]
    public async Task SubscribeAsync_Should_Support_SingleToken_Wildcard()
    {
        // Arrange
        var subscriber = new InMemoryCloudEventSubscriber();
        var receivedTypes = new List<string>();

        await subscriber.SubscribeAsync<TestData>(
            "cloud.beacontower.*.created",
            (cloudEvent, _, _) =>
            {
                receivedTypes.Add(cloudEvent.Type!);
                return Task.CompletedTask;
            });

        // Act
        await subscriber.DeliverAsync(CreateTestCloudEvent("cloud.beacontower.device.created", new TestData()));
        await subscriber.DeliverAsync(CreateTestCloudEvent("cloud.beacontower.user.created", new TestData()));
        await subscriber.DeliverAsync(CreateTestCloudEvent("cloud.beacontower.device.deleted", new TestData()));
        await subscriber.DeliverAsync(CreateTestCloudEvent("com.other.device.created", new TestData()));

        // Assert - * matches single token
        receivedTypes.Should().HaveCount(2);
        receivedTypes.Should().Contain("cloud.beacontower.device.created");
        receivedTypes.Should().Contain("cloud.beacontower.user.created");
    }

    [Fact]
    public async Task SubscribeAsync_Should_Support_MultiToken_Wildcard()
    {
        // Arrange
        var subscriber = new InMemoryCloudEventSubscriber();
        var receivedTypes = new List<string>();

        await subscriber.SubscribeAsync<TestData>(
            "cloud.beacontower.>",
            (cloudEvent, _, _) =>
            {
                receivedTypes.Add(cloudEvent.Type!);
                return Task.CompletedTask;
            });

        // Act
        await subscriber.DeliverAsync(CreateTestCloudEvent("cloud.beacontower.device.created", new TestData()));
        await subscriber.DeliverAsync(CreateTestCloudEvent("cloud.beacontower.user.updated", new TestData()));
        await subscriber.DeliverAsync(CreateTestCloudEvent("cloud.beacontower.sensor.data.telemetry", new TestData()));
        await subscriber.DeliverAsync(CreateTestCloudEvent("com.other.device.created", new TestData()));

        // Assert - > matches one or more tokens to end
        receivedTypes.Should().HaveCount(3);
        receivedTypes.Should().Contain("cloud.beacontower.device.created");
        receivedTypes.Should().Contain("cloud.beacontower.user.updated");
        receivedTypes.Should().Contain("cloud.beacontower.sensor.data.telemetry");
    }

    [Fact]
    public async Task SubscribeAsync_Should_Support_ICloudEventHandler()
    {
        // Arrange
        var subscriber = new InMemoryCloudEventSubscriber();
        var handler = new TestHandler();

        await subscriber.SubscribeAsync("cloud.beacontower.device.created", handler);

        var cloudEvent = CreateTestCloudEvent(
            "cloud.beacontower.device.created",
            new TestData { Name = "TestDevice" });

        // Act
        await subscriber.DeliverAsync(cloudEvent);

        // Assert
        handler.ReceivedEvents.Should().ContainSingle();
        handler.ReceivedData.Should().ContainSingle();
        handler.ReceivedData[0]!.Name.Should().Be("TestDevice");
    }

    [Fact]
    public async Task Multiple_Handlers_Should_All_Receive_Matching_Events()
    {
        // Arrange
        var subscriber = new InMemoryCloudEventSubscriber();
        var handler1CallCount = 0;
        var handler2CallCount = 0;

        await subscriber.SubscribeAsync<TestData>(
            "cloud.beacontower.device.created",
            (_, _, _) => { handler1CallCount++; return Task.CompletedTask; });

        await subscriber.SubscribeAsync<TestData>(
            "cloud.beacontower.device.created",
            (_, _, _) => { handler2CallCount++; return Task.CompletedTask; });

        var cloudEvent = CreateTestCloudEvent("cloud.beacontower.device.created", new TestData());

        // Act
        await subscriber.DeliverAsync(cloudEvent);

        // Assert
        handler1CallCount.Should().Be(1);
        handler2CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Subscriber_Should_Implement_ICloudEventSubscriber()
    {
        // Arrange - intentionally use interface to test implementation
#pragma warning disable CA1859 // Use concrete types for performance - testing interface implementation
        ICloudEventSubscriber subscriber = new InMemoryCloudEventSubscriber();
#pragma warning restore CA1859
        var handlerCalled = false;

        // Act
        await subscriber.SubscribeAsync<TestData>(
            "cloud.beacontower.device.created",
            (_, _, _) =>
            {
                handlerCalled = true;
                return Task.CompletedTask;
            });

        // Assert - Can use interface successfully
        var inMemorySubscriber = (InMemoryCloudEventSubscriber)subscriber;
        await inMemorySubscriber.DeliverAsync(
            CreateTestCloudEvent("cloud.beacontower.device.created", new TestData()));
        handlerCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ClearSubscriptions_Should_Remove_All_Handlers()
    {
        // Arrange
        var subscriber = new InMemoryCloudEventSubscriber();
        var handlerCalled = false;

        await subscriber.SubscribeAsync<TestData>(
            "cloud.beacontower.device.created",
            (_, _, _) =>
            {
                handlerCalled = true;
                return Task.CompletedTask;
            });

        // Act
        subscriber.ClearSubscriptions();
        await subscriber.DeliverAsync(
            CreateTestCloudEvent("cloud.beacontower.device.created", new TestData()));

        // Assert
        handlerCalled.Should().BeFalse();
    }

    [Fact]
    public void SubscribeAsync_Should_Throw_For_Null_Pattern()
    {
        // Arrange
        var subscriber = new InMemoryCloudEventSubscriber();

        // Act & Assert
        FluentActions.Invoking(() => subscriber.SubscribeAsync<TestData>(
                null!,
                (_, _, _) => Task.CompletedTask))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void SubscribeAsync_Should_Throw_For_Null_Handler()
    {
        // Arrange
        var subscriber = new InMemoryCloudEventSubscriber();

        // Act & Assert
        FluentActions.Invoking(() => subscriber.SubscribeAsync<TestData>(
                "cloud.beacontower.>",
                (Func<CloudEvent, TestData?, CancellationToken, Task>)null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DeliverAsync_Should_Throw_For_Null_CloudEvent()
    {
        // Arrange
        var subscriber = new InMemoryCloudEventSubscriber();

        // Act & Assert
        await FluentActions.Invoking(() => subscriber.DeliverAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task InMemory_Implementation_Enables_Unit_Tests_Without_NATS()
    {
        // Arrange - Create publisher and subscriber without any NATS dependencies
        var publisher = new InMemoryCloudEventPublisher();
        var subscriber = new InMemoryCloudEventSubscriber();
        var receivedEvents = new List<CloudEvent>();

        await subscriber.SubscribeAsync<TestData>(
            "cloud.beacontower.>",
            (cloudEvent, _, _) =>
            {
                receivedEvents.Add(cloudEvent);
                return Task.CompletedTask;
            });

        var factory = new CloudEventFactory(new CloudEventFactoryOptions { ServiceName = "unit-test" });

        // Act - Publish and deliver using in-memory implementations
        var event1 = factory.CreateEntityEvent("device", "created", "device-1", new TestData { Name = "Device1" });
        var event2 = factory.CreateEntityEvent("user", "updated", "user-1", new TestData { Name = "User1" });

        await publisher.PublishAsync(event1);
        await publisher.PublishAsync(event2);

        // Simulate message bus delivery
        foreach (var evt in publisher.PublishedEvents)
        {
            await subscriber.DeliverAsync(evt);
        }

        // Assert - Unit test works without NATS
        receivedEvents.Should().HaveCount(2);
        publisher.PublishedEvents.Should().HaveCount(2);
    }

    private static CloudEvent CreateTestCloudEvent(string type, TestData data)
    {
        return new CloudEvent(
            CloudEventsSpecVersion.V1_0,
            BeaconTowerCloudEventExtensionAttributes.AllAttributes)
        {
            Id = Guid.NewGuid().ToString(),
            Source = new Uri("//beacontower.cloud/test", UriKind.RelativeOrAbsolute),
            Type = type,
            Time = DateTimeOffset.UtcNow,
            DataContentType = "application/json",
            Data = data
        };
    }

    private sealed class TestData
    {
        public string Name { get; init; } = string.Empty;
        public int Value { get; init; }
    }

    private sealed class TestHandler : ICloudEventHandler<TestData>
    {
        public List<CloudEvent> ReceivedEvents { get; } = [];
        public List<TestData?> ReceivedData { get; } = [];

        public Task HandleAsync(CloudEvent cloudEvent, TestData? data, CancellationToken ct = default)
        {
            ReceivedEvents.Add(cloudEvent);
            ReceivedData.Add(data);
            return Task.CompletedTask;
        }
    }
}
