using BeaconTower.Events.Abstractions;
using FluentAssertions;
using Xunit;

namespace BeaconTower.Events.Tests.Abstractions;

public class CloudEventFactoryTests
{
    private readonly CloudEventFactory _factory;

    public CloudEventFactoryTests()
    {
        _factory = new CloudEventFactory(new CloudEventFactoryOptions
        {
            ServiceName = "device-api"
        });
    }

    [Fact]
    public void CreateEntityEvent_Should_Create_CloudEvent_With_BeaconTower_Conventions()
    {
        // Arrange
        var data = new TestEntity { Id = Guid.NewGuid(), Name = "Test Device" };

        // Act
        var cloudEvent = _factory.CreateEntityEvent(
            entityType: "device",
            action: "created",
            subject: data.Id.ToString(),
            data: data);

        // Assert
        cloudEvent.Should().NotBeNull();
        cloudEvent.Type.Should().Be("cloud.beacontower.device.created");
        cloudEvent.Source.Should().NotBeNull();
        cloudEvent.Source!.ToString().Should().Be("//beacontower.cloud/device-api");
        cloudEvent.Subject.Should().Be(data.Id.ToString());
        cloudEvent.DataContentType.Should().Be("application/json");
        cloudEvent.Time.Should().NotBeNull();
        cloudEvent.Data.Should().Be(data);
    }

    [Fact]
    public void CreateEntityEvent_Should_Generate_UuidV7_Id()
    {
        // Arrange
        var data = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };

        // Act
        var cloudEvent = _factory.CreateEntityEvent("device", "created", data.Id.ToString(), data);

        // Assert
        cloudEvent.Id.Should().NotBeNullOrWhiteSpace();
        var id = Guid.Parse(cloudEvent.Id!);
        id.Should().NotBeEmpty();

        // UUID v7 has version 7 in bits 48-51 (byte index 6, upper 4 bits)
        var bytes = id.ToByteArray();
        var version = (bytes[7] >> 4) & 0x0F;
        version.Should().Be(7);
    }

    [Fact]
    public void CreateEntityEvent_Should_Include_CorrelationId_When_Provided()
    {
        // Arrange
        var data = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };
        var correlationId = "test-correlation-123";

        // Act
        var cloudEvent = _factory.CreateEntityEvent(
            entityType: "device",
            action: "created",
            subject: data.Id.ToString(),
            data: data,
            correlationId: correlationId);

        // Assert
        cloudEvent.GetCorrelationId().Should().Be(correlationId);
    }

    [Fact]
    public void CreateEntityEvent_Should_Include_UserId_When_Provided()
    {
        // Arrange
        var data = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };
        var userId = Guid.NewGuid();

        // Act
        var cloudEvent = _factory.CreateEntityEvent(
            entityType: "device",
            action: "created",
            subject: data.Id.ToString(),
            data: data,
            userId: userId);

        // Assert
        cloudEvent.GetUserId().Should().Be(userId);
    }

    [Fact]
    public void CreateEntityEvent_Should_Include_UserName_When_Provided()
    {
        // Arrange
        var data = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };
        var userName = "john@example.com";

        // Act
        var cloudEvent = _factory.CreateEntityEvent(
            entityType: "device",
            action: "created",
            subject: data.Id.ToString(),
            data: data,
            userName: userName);

        // Assert
        cloudEvent.GetUserName().Should().Be(userName);
    }

    [Fact]
    public void CreateEvent_Should_Create_CloudEvent_With_Custom_Type()
    {
        // Arrange
        var data = new { Message = "User logged in" };
        var customType = "cloud.beacontower.user.logged_in";

        // Act
        var cloudEvent = _factory.CreateEvent(
            type: customType,
            subject: "user-123",
            data: data);

        // Assert
        cloudEvent.Type.Should().Be(customType);
        cloudEvent.Subject.Should().Be("user-123");
    }

    [Theory]
    [InlineData(null, "created", "subject")]
    [InlineData("", "created", "subject")]
    [InlineData("device", null, "subject")]
    [InlineData("device", "", "subject")]
    [InlineData("device", "created", null)]
    [InlineData("device", "created", "")]
    public void CreateEntityEvent_Should_Throw_For_Invalid_Arguments(
        string? entityType, string? action, string? subject)
    {
        // Arrange
        var data = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };

        // Act
        var act = () => _factory.CreateEntityEvent(entityType!, action!, subject!, data);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_Should_Throw_For_Missing_ServiceName()
    {
        // Act
        var act = () => new CloudEventFactory(new CloudEventFactoryOptions { ServiceName = "" });

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_Should_Throw_For_Null_Options()
    {
        // Act
        var act = () => new CloudEventFactory(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    private sealed record TestEntity
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }
}
