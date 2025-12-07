using BeaconTower.Events.Observability;
using BeaconTower.Observability;
using DotNet.Testcontainers.Builders;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Testcontainers.Nats;
using Xunit;

namespace BeaconTower.Events.Tests.Observability;

/// <summary>
/// Integration tests for NatsHealthCheck using Testcontainers.
/// </summary>
public sealed class NatsHealthCheckTests : IAsyncLifetime
{
    private readonly NatsContainer _natsContainer;
    private string _connectionString = string.Empty;

    public NatsHealthCheckTests()
    {
        _natsContainer = new NatsBuilder()
            .WithImage("nats:2.10")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _natsContainer.StartAsync();
        _connectionString = _natsContainer.GetConnectionString();
    }

    public async Task DisposeAsync()
    {
        await _natsContainer.DisposeAsync();
    }

    [Fact]
    public async Task NatsHealthCheck_Reports_Healthy_When_NATS_Connected()
    {
        // Arrange
        var options = Options.Create(new NatsHealthCheckOptions
        {
            ConnectionString = _connectionString
        });
        await using var healthCheck = new NatsHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey(HealthCheckBase.RttKey);
        result.Data[HealthCheckBase.RttKey].Should().BeOfType<long>();
    }

    [Fact]
    public async Task NatsHealthCheck_Reports_Unhealthy_When_NATS_Disconnected()
    {
        // Arrange - use invalid connection string
        var options = Options.Create(new NatsHealthCheckOptions
        {
            ConnectionString = "nats://invalid-host:4222",
            Timeout = TimeSpan.FromSeconds(2)
        });
        await using var healthCheck = new NatsHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task NatsHealthCheck_Reports_Unhealthy_On_Timeout()
    {
        // Arrange - use valid container but very short timeout
        // Note: This test may be flaky on fast systems, but demonstrates timeout behavior
        var options = Options.Create(new NatsHealthCheckOptions
        {
            ConnectionString = "nats://192.0.2.1:4222", // Non-routable IP (TEST-NET-1)
            Timeout = TimeSpan.FromMilliseconds(100)
        });
        await using var healthCheck = new NatsHealthCheck(options);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
    }
}
