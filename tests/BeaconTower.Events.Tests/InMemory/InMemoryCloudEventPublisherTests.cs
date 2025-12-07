using BeaconTower.Events.Abstractions;
using BeaconTower.Events.InMemory;
using CloudNative.CloudEvents;
using FluentAssertions;
using Xunit;

namespace BeaconTower.Events.Tests.InMemory;

public class InMemoryCloudEventPublisherTests
{
    [Fact]
    public async Task PublishAsync_Should_Store_CloudEvent_In_Memory()
    {
        // Arrange
        var publisher = new InMemoryCloudEventPublisher();
        var cloudEvent = CreateTestCloudEvent("test-1");

        // Act
        await publisher.PublishAsync(cloudEvent);

        // Assert
        publisher.PublishedEvents.Should().ContainSingle();
        publisher.PublishedEvents[0].Should().BeSameAs(cloudEvent);
    }

    [Fact]
    public async Task PublishedEvents_Should_Return_All_Published_CloudEvents()
    {
        // Arrange
        var publisher = new InMemoryCloudEventPublisher();
        var event1 = CreateTestCloudEvent("test-1");
        var event2 = CreateTestCloudEvent("test-2");
        var event3 = CreateTestCloudEvent("test-3");

        // Act
        await publisher.PublishAsync(event1);
        await publisher.PublishAsync(event2);
        await publisher.PublishAsync(event3);

        // Assert
        publisher.PublishedEvents.Should().HaveCount(3);
        publisher.PublishedEvents[0].Id.Should().Be("test-1");
        publisher.PublishedEvents[1].Id.Should().Be("test-2");
        publisher.PublishedEvents[2].Id.Should().Be("test-3");
    }

    [Fact]
    public async Task PublishedEvents_Should_Maintain_Publication_Order()
    {
        // Arrange
        var publisher = new InMemoryCloudEventPublisher();
        var events = Enumerable.Range(1, 10)
            .Select(i => CreateTestCloudEvent($"event-{i}"))
            .ToList();

        // Act
        foreach (var evt in events)
        {
            await publisher.PublishAsync(evt);
        }

        // Assert
        publisher.PublishedEvents.Should().HaveCount(10);
        for (int i = 0; i < 10; i++)
        {
            publisher.PublishedEvents[i].Id.Should().Be($"event-{i + 1}");
        }
    }

    [Fact]
    public async Task ClearEvents_Should_Remove_All_Stored_Events()
    {
        // Arrange
        var publisher = new InMemoryCloudEventPublisher();
        await publisher.PublishAsync(CreateTestCloudEvent("test-1"));
        await publisher.PublishAsync(CreateTestCloudEvent("test-2"));
        publisher.PublishedEvents.Should().HaveCount(2);

        // Act
        publisher.ClearEvents();

        // Assert
        publisher.PublishedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task PublishBatchAsync_Should_Store_All_CloudEvents()
    {
        // Arrange
        var publisher = new InMemoryCloudEventPublisher();
        var events = new[]
        {
            CreateTestCloudEvent("batch-1"),
            CreateTestCloudEvent("batch-2"),
            CreateTestCloudEvent("batch-3")
        };

        // Act
        await publisher.PublishBatchAsync(events);

        // Assert
        publisher.PublishedEvents.Should().HaveCount(3);
        publisher.PublishedEvents.Select(e => e.Id)
            .Should().ContainInOrder("batch-1", "batch-2", "batch-3");
    }

    [Fact]
    public async Task Publisher_Should_Implement_ICloudEventPublisher()
    {
        // Arrange - intentionally use interface to test implementation
#pragma warning disable CA1859 // Use concrete types for performance - testing interface implementation
        ICloudEventPublisher publisher = new InMemoryCloudEventPublisher();
#pragma warning restore CA1859
        var cloudEvent = CreateTestCloudEvent("test-1");

        // Act
        await publisher.PublishAsync(cloudEvent);

        // Assert - Can use interface successfully
        var inMemoryPublisher = (InMemoryCloudEventPublisher)publisher;
        inMemoryPublisher.PublishedEvents.Should().ContainSingle();
    }

    [Fact]
    public async Task PublishAsync_Should_Throw_For_Null_CloudEvent()
    {
        // Arrange
        var publisher = new InMemoryCloudEventPublisher();

        // Act & Assert
        await FluentActions.Invoking(() => publisher.PublishAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task PublishBatchAsync_Should_Throw_For_Null_Collection()
    {
        // Arrange
        var publisher = new InMemoryCloudEventPublisher();

        // Act & Assert
        await FluentActions.Invoking(() => publisher.PublishBatchAsync(null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    private static CloudEvent CreateTestCloudEvent(string id)
    {
        return new CloudEvent(
            CloudEventsSpecVersion.V1_0,
            BeaconTowerCloudEventExtensionAttributes.AllAttributes)
        {
            Id = id,
            Source = new Uri("//beacontower.cloud/test", UriKind.RelativeOrAbsolute),
            Type = "cloud.beacontower.test.created",
            Time = DateTimeOffset.UtcNow,
            DataContentType = "application/json",
            Data = new { Name = "Test" }
        };
    }
}
