using BeaconTower.Events.Abstractions;
using CloudNative.CloudEvents;
using FluentAssertions;
using Xunit;

namespace BeaconTower.Events.Tests.Abstractions;

public class CloudEventExtensionsTests
{
    [Fact]
    public void SetCorrelationId_Should_Set_Extension_Attribute()
    {
        // Arrange
        var cloudEvent = CreateTestCloudEvent();
        var correlationId = "test-correlation-123";

        // Act
        cloudEvent.SetCorrelationId(correlationId);

        // Assert
        cloudEvent[BeaconTowerCloudEventExtensionAttributes.CorrelationId].Should().Be(correlationId);
    }

    [Fact]
    public void GetCorrelationId_Should_Return_Extension_Attribute()
    {
        // Arrange
        var cloudEvent = CreateTestCloudEvent();
        var correlationId = "test-correlation-456";
        cloudEvent[BeaconTowerCloudEventExtensionAttributes.CorrelationId] = correlationId;

        // Act
        var result = cloudEvent.GetCorrelationId();

        // Assert
        result.Should().Be(correlationId);
    }

    [Fact]
    public void GetCorrelationId_Should_Return_Null_When_Not_Set()
    {
        // Arrange
        var cloudEvent = CreateTestCloudEvent();

        // Act
        var result = cloudEvent.GetCorrelationId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void SetUserId_Should_Set_Extension_Attribute()
    {
        // Arrange
        var cloudEvent = CreateTestCloudEvent();
        var userId = Guid.NewGuid();

        // Act
        cloudEvent.SetUserId(userId);

        // Assert - stored as binary (16 bytes) for efficiency
        cloudEvent[BeaconTowerCloudEventExtensionAttributes.UserId].Should().BeEquivalentTo(userId.ToByteArray());
    }

    [Fact]
    public void GetUserId_Should_Return_Guid_From_Extension_Attribute()
    {
        // Arrange
        var cloudEvent = CreateTestCloudEvent();
        var userId = Guid.NewGuid();
        cloudEvent[BeaconTowerCloudEventExtensionAttributes.UserId] = userId.ToByteArray();

        // Act
        var result = cloudEvent.GetUserId();

        // Assert
        result.Should().Be(userId);
    }

    [Fact]
    public void GetUserId_Should_Return_Null_When_Not_Set()
    {
        // Arrange
        var cloudEvent = CreateTestCloudEvent();

        // Act
        var result = cloudEvent.GetUserId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetUserId_Should_Return_Null_For_Invalid_Guid()
    {
        // Arrange
        var cloudEvent = CreateTestCloudEvent();
        // Set invalid binary data (wrong length)
        cloudEvent[BeaconTowerCloudEventExtensionAttributes.UserId] = new byte[] { 1, 2, 3 };

        // Act
        var result = cloudEvent.GetUserId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void SetUserName_Should_Set_Extension_Attribute()
    {
        // Arrange
        var cloudEvent = CreateTestCloudEvent();
        var userName = "john@example.com";

        // Act
        cloudEvent.SetUserName(userName);

        // Assert
        cloudEvent[BeaconTowerCloudEventExtensionAttributes.UserName].Should().Be(userName);
    }

    [Fact]
    public void GetUserName_Should_Return_Extension_Attribute()
    {
        // Arrange
        var cloudEvent = CreateTestCloudEvent();
        var userName = "jane@example.com";
        cloudEvent[BeaconTowerCloudEventExtensionAttributes.UserName] = userName;

        // Act
        var result = cloudEvent.GetUserName();

        // Assert
        result.Should().Be(userName);
    }

    [Fact]
    public void GetUserName_Should_Return_Null_When_Not_Set()
    {
        // Arrange
        var cloudEvent = CreateTestCloudEvent();

        // Act
        var result = cloudEvent.GetUserName();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Extension_Methods_Should_Support_Fluent_Chaining()
    {
        // Arrange
        var cloudEvent = CreateTestCloudEvent();
        var correlationId = "corr-123";
        var userId = Guid.NewGuid();
        var userName = "test@example.com";

        // Act
        cloudEvent
            .SetCorrelationId(correlationId)
            .SetUserId(userId)
            .SetUserName(userName);

        // Assert
        cloudEvent.GetCorrelationId().Should().Be(correlationId);
        cloudEvent.GetUserId().Should().Be(userId);
        cloudEvent.GetUserName().Should().Be(userName);
    }

    [Fact]
    public void SetCorrelationId_Should_Not_Set_When_Null()
    {
        // Arrange
        var cloudEvent = CreateTestCloudEvent();

        // Act
        cloudEvent.SetCorrelationId(null);

        // Assert
        cloudEvent[BeaconTowerCloudEventExtensionAttributes.CorrelationId].Should().BeNull();
    }

    [Fact]
    public void SetUserId_Should_Not_Set_When_Null()
    {
        // Arrange
        var cloudEvent = CreateTestCloudEvent();

        // Act
        cloudEvent.SetUserId(null);

        // Assert
        cloudEvent[BeaconTowerCloudEventExtensionAttributes.UserId].Should().BeNull();
    }

    [Fact]
    public void Extension_Methods_Should_Throw_For_Null_CloudEvent()
    {
        // Arrange
        CloudEvent cloudEvent = null!;

        // Act & Assert
        FluentActions.Invoking(() => cloudEvent.GetCorrelationId()).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => cloudEvent.SetCorrelationId("test")).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => cloudEvent.GetUserId()).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => cloudEvent.SetUserId(Guid.NewGuid())).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => cloudEvent.GetUserName()).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => cloudEvent.SetUserName("test")).Should().Throw<ArgumentNullException>();
    }

    private static CloudEvent CreateTestCloudEvent()
    {
        // Create CloudEvent with registered BeaconTower extension attributes
        return new CloudEvent(
            CloudEventsSpecVersion.V1_0,
            BeaconTowerCloudEventExtensionAttributes.AllAttributes)
        {
            Id = Guid.NewGuid().ToString(),
            Source = new Uri("//beacontower.cloud/test", UriKind.RelativeOrAbsolute),
            Type = "cloud.beacontower.test.created",
            Time = DateTimeOffset.UtcNow,
            DataContentType = "application/json"
        };
    }
}
