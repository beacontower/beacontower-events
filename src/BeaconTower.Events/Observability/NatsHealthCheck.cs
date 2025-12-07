using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using NATS.Client.Core;

namespace BeaconTower.Events.Observability;

/// <summary>
/// Health check for NATS connectivity.
/// Reports Healthy when connected, Unhealthy when disconnected.
/// </summary>
public sealed class NatsHealthCheck : IHealthCheck, IAsyncDisposable
{
    private readonly NatsHealthCheckOptions _options;
    private readonly NatsConnection _connection;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="NatsHealthCheck"/> class.
    /// </summary>
    public NatsHealthCheck(IOptions<NatsHealthCheckOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;

        var natsOpts = new NatsOpts
        {
            Url = _options.ConnectionString
        };
        _connection = new NatsConnection(natsOpts);
    }

    /// <inheritdoc />
#pragma warning disable CA1031 // Catch Exception - Health checks must catch all exceptions to report Unhealthy status
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to ping the NATS server
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.Timeout);

            var rtt = await _connection.PingAsync(cts.Token).ConfigureAwait(false);

            return HealthCheckResult.Healthy($"NATS connection healthy. RTT: {rtt.TotalMilliseconds:F1}ms");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("NATS health check timed out");
        }
        catch (NatsException ex)
        {
            return HealthCheckResult.Unhealthy($"NATS connection unhealthy: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"NATS health check failed: {ex.Message}", ex);
        }
    }
#pragma warning restore CA1031

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await _connection.DisposeAsync().ConfigureAwait(false);
    }
}

/// <summary>
/// Configuration options for the NATS health check.
/// </summary>
public class NatsHealthCheckOptions
{
    /// <summary>
    /// The NATS server connection string.
    /// </summary>
    public string ConnectionString { get; set; } = "nats://localhost:4222";

    /// <summary>
    /// The timeout for the health check ping.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
}
