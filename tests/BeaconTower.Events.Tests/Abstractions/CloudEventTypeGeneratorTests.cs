using BeaconTower.Events.Abstractions;
using FluentAssertions;
using Xunit;

namespace BeaconTower.Events.Tests.Abstractions;

public class CloudEventTypeGeneratorTests
{
    [Fact]
    public void Generate_Should_Produce_BeaconTower_Format()
    {
        // Arrange & Act
        var result = CloudEventTypeGenerator.Generate("device", "created");

        // Assert
        result.Should().Be("com.beacontower.device.created");
    }

    [Theory]
    [InlineData("Device", "Created", "com.beacontower.device.created")]
    [InlineData("USER", "LOGGED_IN", "com.beacontower.user.logged_in")]
    [InlineData("Alarm", "Triggered", "com.beacontower.alarm.triggered")]
    public void Generate_Should_Normalize_To_Lowercase(string entityType, string action, string expected)
    {
        // Act
        var result = CloudEventTypeGenerator.Generate(entityType, action);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Created_Should_Generate_Created_Type()
    {
        // Act
        var result = CloudEventTypeGenerator.Created("device");

        // Assert
        result.Should().Be("com.beacontower.device.created");
    }

    [Fact]
    public void Updated_Should_Generate_Updated_Type()
    {
        // Act
        var result = CloudEventTypeGenerator.Updated("device");

        // Assert
        result.Should().Be("com.beacontower.device.updated");
    }

    [Fact]
    public void Deleted_Should_Generate_Deleted_Type()
    {
        // Act
        var result = CloudEventTypeGenerator.Deleted("device");

        // Assert
        result.Should().Be("com.beacontower.device.deleted");
    }

    [Fact]
    public void TryParse_Should_Extract_EntityType_And_Action()
    {
        // Arrange
        var cloudEventType = "com.beacontower.device.created";

        // Act
        var success = CloudEventTypeGenerator.TryParse(cloudEventType, out var entityType, out var action);

        // Assert
        success.Should().BeTrue();
        entityType.Should().Be("device");
        action.Should().Be("created");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid.format")]
    [InlineData("com.other.device.created")]
    public void TryParse_Should_Return_False_For_Invalid_Format(string? cloudEventType)
    {
        // Act
        var success = CloudEventTypeGenerator.TryParse(cloudEventType!, out var entityType, out var action);

        // Assert
        success.Should().BeFalse();
        entityType.Should().BeEmpty();
        action.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null, "created")]
    [InlineData("", "created")]
    [InlineData("   ", "created")]
    [InlineData("device", null)]
    [InlineData("device", "")]
    [InlineData("device", "   ")]
    public void Generate_Should_Throw_For_Invalid_Arguments(string? entityType, string? action)
    {
        // Act
        var act = () => CloudEventTypeGenerator.Generate(entityType!, action!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
