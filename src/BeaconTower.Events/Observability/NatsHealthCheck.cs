using BeaconTower.Observability;
using Microsoft.Extensions.Options;
using NATS.Client.Core;

namespace BeaconTower.Events.Observability;

/// <summary>
/// Health check for NATS connectivity.
/// Reports Healthy when connected, Unhealthy when disconnected.
/// </summary>
/// <remarks>
/// Extends <see cref="HealthCheckBase"/> for standard RTT measurement,
/// timeout handling, and exception reporting patterns.
/// </remarks>
public sealed class NatsHealthCheck : HealthCheckBase, IAsyncDisposable
{
    private readonly NatsConnection _connection;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="NatsHealthCheck"/> class.
    /// </summary>
    public NatsHealthCheck(IOptions<NatsHealthCheckOptions> options)
        : base(options?.Value?.Timeout)
    {
        ArgumentNullException.ThrowIfNull(options);
        var opts = options.Value;

        var natsOpts = new NatsOpts
        {
            Url = opts.ConnectionString
        };
        _connection = new NatsConnection(natsOpts);
    }

    /// <inheritdoc />
    protected override async Task PerformHealthCheckAsync(CancellationToken cancellationToken)
    {
        await _connection.PingAsync(cancellationToken).ConfigureAwait(false);
    }

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
    /// If not specified, uses <see cref="HealthCheckBase.DefaultTimeout"/> (30 seconds).
    /// </summary>
    public TimeSpan? Timeout { get; set; }
}
