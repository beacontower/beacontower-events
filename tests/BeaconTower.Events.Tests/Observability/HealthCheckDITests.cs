using BeaconTower.Events.DependencyInjection;
using BeaconTower.Events.Observability;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BeaconTower.Events.Tests.Observability;

/// <summary>
/// Tests for health check DI registration extensions.
/// </summary>
public sealed class HealthCheckDITests
{
    [Fact]
    public void AddNatsHealthCheck_Extension_Registers_Health_Check_With_AspNetCore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddHealthChecks()
            .AddNatsHealthCheck("nats://localhost:4222");

        // Assert
        var provider = services.BuildServiceProvider();
        var healthCheckService = provider.GetService<HealthCheckService>();

        healthCheckService.Should().NotBeNull();
    }

    [Fact]
    public void AddNatsHealthCheck_Registers_With_Custom_Name()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddHealthChecks()
            .AddNatsHealthCheck("nats://localhost:4222", name: "custom-nats");

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetService<Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>();

        options.Should().NotBeNull();
        options!.Value.Registrations.Should().Contain(r => r.Name == "custom-nats");
    }

    [Fact]
    public void AddNatsHealthCheck_With_Options_Action_Works()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddHealthChecks()
            .AddNatsHealthCheck(options =>
            {
                options.ConnectionString = "nats://custom-host:4222";
                options.Timeout = TimeSpan.FromSeconds(10);
            });

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetService<Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>();

        options.Should().NotBeNull();
        options!.Value.Registrations.Should().Contain(r => r.Name == "nats");
    }

    [Fact]
    public void AddNatsHealthCheck_Supports_Custom_Tags()
    {
        // Arrange
        var services = new ServiceCollection();
        var tags = new[] { "infrastructure", "messaging" };

        // Act
        services.AddHealthChecks()
            .AddNatsHealthCheck("nats://localhost:4222", tags: tags);

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetService<Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>();

        options.Should().NotBeNull();
        var registration = options!.Value.Registrations.First(r => r.Name == "nats");
        registration.Tags.Should().BeEquivalentTo(tags);
    }

    [Fact]
    public void AddNatsHealthCheck_Supports_Custom_Failure_Status()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddHealthChecks()
            .AddNatsHealthCheck(
                "nats://localhost:4222",
                failureStatus: HealthStatus.Degraded);

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetService<Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>();

        options.Should().NotBeNull();
        var registration = options!.Value.Registrations.First(r => r.Name == "nats");
        registration.FailureStatus.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public void AddCloudEventsMetrics_Registers_Singleton_Metrics()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCloudEventsMetrics();

        // Assert
        var provider = services.BuildServiceProvider();
        var metrics = provider.GetService<CloudEventsMetrics>();

        metrics.Should().NotBeNull();

        // Verify singleton behavior
        var metrics2 = provider.GetService<CloudEventsMetrics>();
        metrics2.Should().BeSameAs(metrics);
    }
}
