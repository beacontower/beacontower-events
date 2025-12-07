using BeaconTower.Events.Nats;
using FluentAssertions;
using Xunit;

namespace BeaconTower.Events.Tests.Nats;

public class CloudEventSubjectMapperTests
{
    [Theory]
    [InlineData("cloud.beacontower.device.created", "beacontower.device.created")]
    [InlineData("cloud.beacontower.user.updated", "beacontower.user.updated")]
    [InlineData("cloud.beacontower.sensor.data.telemetry", "beacontower.sensor.data.telemetry")]
    public void ToNatsSubject_Should_Strip_Cloud_Prefix(string cloudEventType, string expectedSubject)
    {
        // Act
        var result = CloudEventSubjectMapper.ToNatsSubject(cloudEventType);

        // Assert
        result.Should().Be(expectedSubject);
    }

    [Fact]
    public void ToNatsSubject_Should_Return_Original_If_No_Cloud_Prefix()
    {
        // Arrange
        var type = "beacontower.device.created";

        // Act
        var result = CloudEventSubjectMapper.ToNatsSubject(type);

        // Assert
        result.Should().Be(type);
    }

    [Theory]
    [InlineData("beacontower.device.created", "cloud.beacontower.device.created")]
    [InlineData("beacontower.user.updated", "cloud.beacontower.user.updated")]
    public void ToCloudEventType_Should_Add_Cloud_Prefix(string natsSubject, string expectedType)
    {
        // Act
        var result = CloudEventSubjectMapper.ToCloudEventType(natsSubject);

        // Assert
        result.Should().Be(expectedType);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToNatsSubject_Should_Throw_For_Invalid_Input(string? input)
    {
        // Act
        var act = () => CloudEventSubjectMapper.ToNatsSubject(input!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToCloudEventType_Should_Throw_For_Invalid_Input(string? input)
    {
        // Act
        var act = () => CloudEventSubjectMapper.ToCloudEventType(input!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
