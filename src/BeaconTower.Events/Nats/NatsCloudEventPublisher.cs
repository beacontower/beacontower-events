using System.Diagnostics;
using BeaconTower.Events.Abstractions;
using BeaconTower.Events.Observability;
using BeaconTower.Observability;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using Polly;
using Polly.Retry;

namespace BeaconTower.Events.Nats;

/// <summary>
/// NATS JetStream-based CloudEvent publisher using Structured Content Mode.
/// </summary>
/// <remarks>
/// Structured Content Mode (CloudEvents NATS Protocol Binding):
/// - Full CloudEvent serialized as JSON in the message payload
/// - Uses CloudNative.CloudEvents.SystemTextJson formatter
/// </remarks>
public sealed class NatsCloudEventPublisher : ICloudEventPublisher, IAsyncDisposable
{
    private readonly NatsConnection _connection;
    private readonly NatsJSContext _jetStream;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly NatsCloudEventPublisherOptions _options;
    private readonly ILogger<NatsCloudEventPublisher> _logger;
    private readonly CloudEventsMetrics? _metrics;
    private readonly JsonEventFormatter _formatter;
    private bool _streamEnsured;

    /// <summary>
    /// Initializes a new instance of the <see cref="NatsCloudEventPublisher"/> class.
    /// </summary>
    public NatsCloudEventPublisher(
        IOptions<NatsCloudEventPublisherOptions> options,
        ILogger<NatsCloudEventPublisher> logger,
        CloudEventsMetrics? metrics = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;
        _metrics = metrics;
        _formatter = new JsonEventFormatter();

        // Create NATS connection
        var natsOpts = new NatsOpts
        {
            Url = _options.ConnectionString
        };
        _connection = new NatsConnection(natsOpts);
        _jetStream = new NatsJSContext(_connection);

        // Build resilience pipeline with retry
        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = _options.MaxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                Delay = _options.InitialRetryDelay,
                UseJitter = true,
                OnRetry = args =>
                {
                    Log.PublishRetry(_logger, args.Outcome.Exception, args.AttemptNumber, args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <inheritdoc />
    public async Task PublishAsync(CloudEvent cloudEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(cloudEvent);

        await EnsureStreamExistsAsync(ct).ConfigureAwait(false);

        var subject = CloudEventSubjectMapper.ToNatsSubject(cloudEvent.Type ?? string.Empty);
        var correlationId = cloudEvent.GetCorrelationId();

        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["CloudEventType"] = cloudEvent.Type,
            ["CloudEventId"] = cloudEvent.Id
        });

        var eventType = cloudEvent.Type ?? "unknown";
        var eventId = cloudEvent.Id ?? "unknown";

        // Create Activity for distributed tracing
        using var activity = ActivityExtensions.StartActivity("CloudEvents.Publish", ActivityKind.Producer);
        activity?.AddTag("cloudevents.type", eventType);
        activity?.AddTag("cloudevents.subject", subject);
        activity?.AddTag("cloudevents.id", eventId);

        Log.Publishing(_logger, eventType, subject);

        try
        {
            await _resiliencePipeline.ExecuteAsync(async token =>
            {
                // Structured Content Mode: serialize full CloudEvent as JSON
                var payload = _formatter.EncodeStructuredModeMessage(cloudEvent, out _);

                var ack = await _jetStream.PublishAsync(
                    subject,
                    payload,
                    cancellationToken: token).ConfigureAwait(false);

                ack.EnsureSuccess();

                // Record metric for successful publish
                _metrics?.RecordEventPublished(eventType);

                Log.Published(_logger, eventId, subject, ack.Seq);
            }, ct).ConfigureAwait(false);

            activity?.SetStatusOk();
        }
        catch (Exception ex)
        {
            activity?.SetStatusError(ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task PublishBatchAsync(IEnumerable<CloudEvent> cloudEvents, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(cloudEvents);

        foreach (var cloudEvent in cloudEvents)
        {
            await PublishAsync(cloudEvent, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Ensures the JetStream stream exists, creating it if necessary.
    /// </summary>
    private async Task EnsureStreamExistsAsync(CancellationToken ct)
    {
        if (_streamEnsured)
        {
            return;
        }

        try
        {
            await _jetStream.GetStreamAsync(_options.StreamName, cancellationToken: ct).ConfigureAwait(false);
            _streamEnsured = true;
        }
        catch (NatsJSApiException ex) when (ex.Error.Code == 404)
        {
            Log.CreatingStream(_logger, _options.StreamName, string.Join(", ", _options.StreamSubjects));

            var config = new StreamConfig(
                _options.StreamName,
                _options.StreamSubjects.ToList())
            {
                Retention = StreamConfigRetention.Limits,
                Storage = StreamConfigStorage.File
            };

            await _jetStream.CreateStreamAsync(config, ct).ConfigureAwait(false);
            _streamEnsured = true;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync().ConfigureAwait(false);
    }
}
